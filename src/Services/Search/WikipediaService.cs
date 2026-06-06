using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using AIAgentTool.Models;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.Search
{
    public class WikipediaService
    {
        private const string SEARCH_URL =
            "https://{0}.wikipedia.org/w/api.php?action=query&list=search" +
            "&srsearch={1}&format=json&utf8=1&srlimit=5";

        private const string EXTRACT_URL =
            "https://{0}.wikipedia.org/w/api.php?action=query&prop=extracts" +
            "&exintro=true&explaintext=true&titles={1}&format=json&utf8=1";

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
                Debug.WriteLine(
                    string.Format("Wikipedia ({0}) 搜尋失敗: {1}", lang, ex.Message));
            }

            return results;
        }

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
                Debug.WriteLine("Wikipedia 摘要取得失敗: " + ex.Message);
            }
            return null;
        }

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

        public List<SearchResult> SearchMultiLang(string query)
        {
            List<SearchResult> results = new List<SearchResult>();
            List<SearchResult> zhResults = Search(query, "zh");
            results.AddRange(zhResults);
            List<SearchResult> enResults = Search(query, "en");
            results.AddRange(enResults);
            return results;
        }

        public KnowledgeItem GetSummaryMultiLang(string title)
        {
            KnowledgeItem item = GetSummary(title, "zh");
            if (item != null) return item;
            item = GetSummary(title, "en");
            return item;
        }

        public KnowledgeItem GetArticleSummary(string query)
        {
            return GetSummaryMultiLang(query);
        }

        private string ExtractPageContent(string json)
        {
            if (string.IsNullOrEmpty(json)) return "";

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

            string extract = HtmlHelper.ExtractJsonValue(json, "extract");
            return extract ?? "";
        }
    }
}
