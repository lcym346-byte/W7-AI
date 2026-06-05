using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AIAgentTool.Models;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.Search
{
    /// <summary>
    /// 維基百科查詢服務
    /// 支援中文 (zh) 和英文 (en) Wikipedia
    /// 免費，不需 API Key
    /// </summary>
    public class WikipediaService
    {
        // 搜尋 API
        private const string SEARCH_URL =
            "https://{0}.wikipedia.org/w/api.php?action=query&list=search" +
            "&srsearch={1}&format=json&utf8=1&srlimit=5";

        // 條目摘要 API
        private const string EXTRACT_URL =
            "https://{0}.wikipedia.org/w/api.php?action=query&prop=extracts" +
            "&exintro=true&explaintext=true&titles={1}&format=json&utf8=1";

        // 完整條目 API
        private const string FULL_EXTRACT_URL =
            "https://{0}.wikipedia.org/w/api.php?action=query&prop=extracts" +
            "&explaintext=true&titles={1}&format=json&utf8=1&exsectionformat=plain";

        private readonly WebSearchService _http;

        public WikipediaService()
{
    _http = new WebSearchService();
}

public WikipediaService(WebSearchService webSearch)
{
    _http = webSearch ?? new WebSearchService();
}


        /// <summary>
        /// 搜尋 Wikipedia 條目
        /// </summary>
        /// <param name="query">搜尋關鍵字</param>
        /// <param name="lang">語言代碼: "zh" 或 "en"</param>
        public List<SearchResult> Search(string query, string lang)
        {
            List<SearchResult> results = new List<SearchResult>();

            try
            {
                string url = string.Format(SEARCH_URL,
                    lang, Uri.EscapeDataString(query));
                string json = _http.HttpGet(url);

                List<string> searchObjects =
                    HtmlHelper.ExtractJsonArrayObjects(json, "search");

                foreach (string obj in searchObjects)
                {
                    string title = HtmlHelper.ExtractJsonValue(obj, "title");
                    string snippet = HtmlHelper.StripHtml(
                        HtmlHelper.ExtractJsonValue(obj, "snippet"));

                    string pageUrl = string.Format(
                        "https://{0}.wikipedia.org/wiki/{1}",
                        lang, Uri.EscapeDataString(title.Replace(' ', '_')));

                    results.Add(new SearchResult
                    {
                        Title = title,
                        Snippet = snippet,
                        Url = pageUrl,
                        Source = string.Format("Wikipedia/{0}", lang.ToUpper()),
                        RelevanceScore = 0.8
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    string.Format("Wikipedia ({0}) 搜尋失敗: {1}", lang, ex.Message));
            }

            return results;
        }

        /// <summary>
        /// 取得條目摘要 (簡介段落)
        /// </summary>
        public KnowledgeItem GetSummary(string title, string lang)
        {
            try
            {
                string url = string.Format(EXTRACT_URL,
                    lang, Uri.EscapeDataString(title));
                string json = _http.HttpGet(url);

                string extract = ExtractPageContent(json);

                if (!string.IsNullOrEmpty(extract) && extract.Length > 20)
                {
                    return new KnowledgeItem
                    {
                        Topic = title,
                        Content = extract,
                        Source = string.Format("Wikipedia ({0})", lang.ToUpper())
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Wikipedia 摘要取得失敗: " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// 取得完整條目
        /// </summary>
        public KnowledgeItem GetFullArticle(string title, string lang)
        {
            try
            {
                string url = string.Format(FULL_EXTRACT_URL,
                    lang, Uri.EscapeDataString(title));
                string json = _http.HttpGet(url);

                string extract = ExtractPageContent(json);

                if (!string.IsNullOrEmpty(extract) && extract.Length > 50)
                {
                    return new KnowledgeItem
                    {
                        Topic = title,
                        Content = extract,
                        Source = string.Format("Wikipedia ({0}) Full", lang.ToUpper())
                    };
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 多語言搜尋 (先中文後英文)
        /// </summary>
        public List<SearchResult> SearchMultiLang(string query)
        {
            List<SearchResult> results = new List<SearchResult>();

            // 中文搜尋
            List<SearchResult> zhResults = Search(query, "zh");
            results.AddRange(zhResults);

            // 英文搜尋
            List<SearchResult> enResults = Search(query, "en");
            results.AddRange(enResults);

            return results;
        }

        /// <summary>
        /// 嘗試取得摘要 (先中文後英文)
        /// </summary>
        public KnowledgeItem GetSummaryMultiLang(string title)
        {
            KnowledgeItem item = GetSummary(title, "zh");
            if (item != null) return item;

            item = GetSummary(title, "en");
            return item;
        }

        /// <summary>
        /// 從 Wikipedia API 回應中擷取頁面內容
        /// Wikipedia 的 JSON 結構: {"query":{"pages":{"12345":{"extract":"..."}}}}
        /// </summary>
        private string ExtractPageContent(string json)
        {
            if (string.IsNullOrEmpty(json)) return "";

            // 方式 1: 直接找 "extract" 值
            Match match = Regex.Match(json,
                @"""extract""\s*:\s*""((?:[^""\\]|\\[\s\S])*)""");

            if (match.Success)
            {
                return match.Groups[1].Value
                    .Replace("\\n", "\n")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\")
                    .Replace("\\t", "\t");
            }

            // 方式 2: 用 HtmlHelper
            string extract = HtmlHelper.ExtractJsonValue(json, "extract");
            return extract ?? "";
        }
    }
}
