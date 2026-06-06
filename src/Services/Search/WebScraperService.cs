using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.Search
{
    public class WebScraperService
    {
        private readonly WebSearchService _http;

        public WebScraperService()
        {
            _http = new WebSearchService();
        }

        public string ExtractMainContent(string url)
        {
            try
            {
                string html = _http.HttpGet(url);

                string content = HtmlHelper.ExtractTagContent(html, "article");
                if (!string.IsNullOrEmpty(content) && content.Length > 100)
                    return HtmlHelper.TruncateText(content, 5000);

                content = HtmlHelper.ExtractTagContent(html, "main");
                if (!string.IsNullOrEmpty(content) && content.Length > 100)
                    return HtmlHelper.TruncateText(content, 5000);

                content = ExtractParagraphs(html);
                if (!string.IsNullOrEmpty(content) && content.Length > 100)
                    return HtmlHelper.TruncateText(content, 5000);

                content = HtmlHelper.StripHtml(html);
                return HtmlHelper.TruncateText(content, 3000);
            }
            catch (Exception ex)
            {
                return "無法擷取網頁內容: " + ex.Message;
            }
        }

        public string FetchMainContent(string url)
        {
            return ExtractMainContent(url);
        }

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

        public List<KeyValuePair<string, string>> ExtractLinks(string url)
        {
            try
            {
                string html = _http.HttpGet(url);
                List<KeyValuePair<string, string>> links = HtmlHelper.ExtractLinks(html);

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

        private string ExtractParagraphs(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            MatchCollection paragraphs = Regex.Matches(html,
                @"<p[^>]*>([\s\S]*?)</p>", RegexOptions.IgnoreCase);

            StringBuilder sb = new StringBuilder();
            foreach (Match p in paragraphs)
            {
                string text = HtmlHelper.StripHtml(p.Groups[1].Value).Trim();
                if (text.Length > 40)
                {
                    sb.AppendLine(text);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }
    }
}
