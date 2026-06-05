using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.Search
{
    /// <summary>
    /// 網頁內容擷取服務
    /// 抓取網頁並提取主要文字內容
    /// </summary>
    public class WebScraperService
    {
        private readonly WebSearchService _http;

        public WebScraperService()
        {
            _http = new WebSearchService();
        }

        /// <summary>
        /// 擷取網頁主要內容（自動偵測最佳區塊）
        /// </summary>
        public string ExtractMainContent(string url)
        {
            try
            {
                string html = _http.HttpGet(url);

                // 優先嘗試語意標籤
                string content = HtmlHelper.ExtractTagContent(html, "article");
                if (!string.IsNullOrEmpty(content) && content.Length > 100)
                    return HtmlHelper.TruncateText(content, 5000);

                content = HtmlHelper.ExtractTagContent(html, "main");
                if (!string.IsNullOrEmpty(content) && content.Length > 100)
                    return HtmlHelper.TruncateText(content, 5000);

                // 次選：收集所有有意義的 <p> 標籤
                content = ExtractParagraphs(html);
                if (!string.IsNullOrEmpty(content) && content.Length > 100)
                    return HtmlHelper.TruncateText(content, 5000);

                // 最後手段：整頁去 HTML
                content = HtmlHelper.StripHtml(html);
                return HtmlHelper.TruncateText(content, 3000);
            }
            catch (Exception ex)
            {
                return "無法擷取網頁內容: " + ex.Message;
            }
        }

        /// <summary>
        /// FetchMainContent — ExtractMainContent 的別名，供 TaskAutomationService 呼叫
        /// </summary>
        public string FetchMainContent(string url)
        {
            return ExtractMainContent(url);
        }


        /// <summary>
        /// 擷取網頁標題
        /// </summary>
        public string ExtractTitle(string url)
        {
            try
            {
                string html = _http.HttpGet(url);
                return HtmlHelper.ExtractTagContent(html, "title");
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 擷取網頁中所有連結
        /// </summary>
        public List<KeyValuePair<string, string>> ExtractLinks(string url)
        {
            try
            {
                string html = _http.HttpGet(url);
                List<KeyValuePair<string, string>> links = HtmlHelper.ExtractLinks(html);

                // 將相對路徑轉為絕對路徑
                List<KeyValuePair<string, string>> absoluteLinks =
                    new List<KeyValuePair<string, string>>();
                Uri baseUri = new Uri(url);

                foreach (KeyValuePair<string, string> link in links)
                {
                    string linkUrl = link.Key;
                    try
                    {
                        if (!linkUrl.StartsWith("http"))
                        {
                            Uri absolute = new Uri(baseUri, linkUrl);
                            linkUrl = absolute.AbsoluteUri;
                        }
                        absoluteLinks.Add(
                            new KeyValuePair<string, string>(linkUrl, link.Value));
                    }
                    catch { }
                }
                return absoluteLinks;
            }
            catch
            {
                return new List<KeyValuePair<string, string>>();
            }
        }

        /// <summary>
        /// 從 HTML 中擷取所有有意義的段落
        /// </summary>
        private string ExtractParagraphs(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            MatchCollection paragraphs = Regex.Matches(html,
                @"<p[^>]*>([\s\S]*?)</p>", RegexOptions.IgnoreCase);

            StringBuilder sb = new StringBuilder();
            foreach (Match p in paragraphs)
            {
                string text = HtmlHelper.StripHtml(p.Groups[1].Value).Trim();
                if (text.Length > 40) // 只收集有意義的段落
                {
                    sb.AppendLine(text);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}
