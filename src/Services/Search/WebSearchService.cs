using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using AIAgentTool.Models;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.Search
{
    /// <summary>
    /// 網路搜尋服務 - DuckDuckGo (修正版，加入必要 headers)
    /// </summary>
    public class WebSearchService
    {
        private const string DDG_INSTANT_URL = "https://api.duckduckgo.com/?q={0}&format=json&no_redirect=1&no_html=1";
        private const string DDG_HTML_URL = "https://html.duckduckgo.com/html/?q={0}";

        private const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36";

        /// <summary>
        /// 同時搜尋多個來源，合併結果
        /// </summary>
        public List<SearchResult> SearchAll(string query)
        {
            List<SearchResult> allResults = new List<SearchResult>();
            List<SearchResult> instantResults = null;
            List<SearchResult> htmlResults = null;
            Exception instantEx = null;
            Exception htmlEx = null;

            Thread t1 = new Thread(delegate()
            {
                try { instantResults = SearchDuckDuckGoInstant(query); }
                catch (Exception ex) { instantEx = ex; }
            });

            Thread t2 = new Thread(delegate()
            {
                try { htmlResults = SearchDuckDuckGoHtml(query); }
                catch (Exception ex) { htmlEx = ex; }
            });

            t1.Start();
            t2.Start();
            t1.Join(15000);
            t2.Join(15000);

            if (instantResults != null) allResults.AddRange(instantResults);
            if (htmlResults != null) allResults.AddRange(htmlResults);

            // 去重：相同 URL 只保留分數最高的
            Dictionary<string, SearchResult> unique = new Dictionary<string, SearchResult>();
            foreach (SearchResult r in allResults)
            {
                string key = (r.Url ?? r.Title ?? "").ToLower();
                if (string.IsNullOrEmpty(key)) continue;
                if (!unique.ContainsKey(key) || unique[key].RelevanceScore < r.RelevanceScore)
                    unique[key] = r;
            }

            List<SearchResult> final = new List<SearchResult>(unique.Values);
            final.Sort(delegate(SearchResult a, SearchResult b)
            {
                return b.RelevanceScore.CompareTo(a.RelevanceScore);
            });

            if (final.Count > 10) final.RemoveRange(10, final.Count - 10);
            return final;
        }

        /// <summary>
        /// DuckDuckGo Instant Answer API
        /// </summary>
        public List<SearchResult> SearchDuckDuckGoInstant(string query)
        {
            List<SearchResult> results = new List<SearchResult>();

            try
            {
                string url = string.Format(DDG_INSTANT_URL, Uri.EscapeDataString(query));
                string json = HttpGet(url);
                if (string.IsNullOrEmpty(json)) return results;

                // Abstract
                string abstractText = HtmlHelper.ExtractJsonValue(json, "AbstractText");
                string abstractSource = HtmlHelper.ExtractJsonValue(json, "AbstractSource");
                string abstractUrl = HtmlHelper.ExtractJsonValue(json, "AbstractURL");

                if (!string.IsNullOrEmpty(abstractText) && abstractText.Length > 10)
                {
                    results.Add(new SearchResult
                    {
                        Title = HtmlHelper.ExtractJsonValue(json, "Heading"),
                        Snippet = abstractText,
                        Url = abstractUrl,
                        Source = abstractSource,
                        RelevanceScore = 0.9
                    });
                }

                // Direct Answer
                string answer = HtmlHelper.ExtractJsonValue(json, "Answer");
                if (!string.IsNullOrEmpty(answer) && answer.Length > 2)
                {
                    results.Add(new SearchResult
                    {
                        Title = query + " - Direct Answer",
                        Snippet = answer,
                        Url = "",
                        Source = "DuckDuckGo Instant",
                        RelevanceScore = 1.0
                    });
                }

                // Definition
                string definition = HtmlHelper.ExtractJsonValue(json, "Definition");
                string definitionUrl = HtmlHelper.ExtractJsonValue(json, "DefinitionURL");
                if (!string.IsNullOrEmpty(definition) && definition.Length > 5)
                {
                    results.Add(new SearchResult
                    {
                        Title = query + " - Definition",
                        Snippet = definition,
                        Url = definitionUrl,
                        Source = "DuckDuckGo Definition",
                        RelevanceScore = 0.85
                    });
                }

                // Related Topics
                List<string> topics = HtmlHelper.ExtractJsonArrayObjects(json, "RelatedTopics");
                int count = 0;
                foreach (string topic in topics)
                {
                    if (count >= 5) break;
                    string text = HtmlHelper.ExtractJsonValue(topic, "Text");
                    string firstUrl = HtmlHelper.ExtractJsonValue(topic, "FirstURL");
                    if (!string.IsNullOrEmpty(text) && text.Length > 10)
                    {
                        results.Add(new SearchResult
                        {
                            Title = text.Length > 80 ? text.Substring(0, 80) : text,
                            Snippet = text,
                            Url = firstUrl,
                            Source = "DuckDuckGo Related",
                            RelevanceScore = 0.6
                        });
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DDG Instant error: " + ex.Message);
            }

            return results;
        }

        /// <summary>
        /// DuckDuckGo HTML 搜尋（解析搜尋結果頁面）
        /// </summary>
        public List<SearchResult> SearchDuckDuckGoHtml(string query)
        {
            List<SearchResult> results = new List<SearchResult>();

            try
            {
                string url = string.Format(DDG_HTML_URL, Uri.EscapeDataString(query));
                string html = HttpGet(url);
                if (string.IsNullOrEmpty(html)) return results;

                // 解析搜尋結果
                MatchCollection matches = Regex.Matches(html,
                    @"<a[^>]+class=""result__a""[^>]+href=""([^""]+)""[^>]*>([\s\S]*?)</a>",
                    RegexOptions.IgnoreCase);

                MatchCollection snippets = Regex.Matches(html,
                    @"<a[^>]+class=""result__snippet""[^>]*>([\s\S]*?)</a>",
                    RegexOptions.IgnoreCase);

                for (int i = 0; i < matches.Count && i < 10; i++)
                {
                    string resultUrl = matches[i].Groups[1].Value;
                    string title = HtmlHelper.StripHtml(matches[i].Groups[2].Value);
                    string snippet = i < snippets.Count
                        ? HtmlHelper.StripHtml(snippets[i].Groups[1].Value)
                        : "";

                    // 解碼 DuckDuckGo 重導向 URL
                    resultUrl = DecodeDdgUrl(resultUrl);

                    if (!string.IsNullOrEmpty(title) && title.Length > 2)
                    {
                        results.Add(new SearchResult
                        {
                            Title = title,
                            Snippet = snippet,
                            Url = resultUrl,
                            Source = "DuckDuckGo",
                            RelevanceScore = 0.7
                        });
                    }
                }

                // 如果正規解析沒結果，嘗試備用模式
                if (results.Count == 0)
                {
                    MatchCollection links = Regex.Matches(html,
                        @"<a[^>]+href=""(https?://[^""]+)""[^>]*>([^<]+)</a>",
                        RegexOptions.IgnoreCase);

                    int count = 0;
                    foreach (Match m in links)
                    {
                        if (count >= 10) break;
                        string linkUrl = m.Groups[1].Value;
                        string linkText = m.Groups[2].Value.Trim();

                        if (linkUrl.Contains("duckduckgo.com")) continue;
                        if (linkText.Length < 5) continue;

                        results.Add(new SearchResult
                        {
                            Title = linkText,
                            Snippet = "",
                            Url = linkUrl,
                            Source = "DuckDuckGo (fallback)",
                            RelevanceScore = 0.5
                        });
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DDG HTML error: " + ex.Message);
            }

            return results;
        }

        /// <summary>
        /// HTTP GET 請求（修正版，加入所有必要 headers）
        /// </summary>
        public string HttpGet(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = USER_AGENT;
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,application/json;q=0.8,*/*;q=0.7";
                request.Headers.Add("Accept-Language", "en-US,en;q=0.9,zh-TW;q=0.8,zh;q=0.7");
                request.Headers.Add("Accept-Encoding", "gzip, deflate");
                request.Headers.Add("Cache-Control", "no-cache");
                request.Headers.Add("Pragma", "no-cache");
                request.Referer = "https://duckduckgo.com/";
                request.Timeout = 15000;
                request.ReadWriteTimeout = 15000;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException wex)
            {
                Debug.WriteLine("HttpGet WebException: " + wex.Message);
                // 嘗試讀取錯誤回應
                if (wex.Response != null)
                {
                    try
                    {
                        using (Stream s = wex.Response.GetResponseStream())
                        using (StreamReader sr = new StreamReader(s))
                        {
                            Debug.WriteLine("Error body: " + sr.ReadToEnd());
                        }
                    }
                    catch { }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HttpGet error: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 解碼 DuckDuckGo 重導向 URL
        /// </summary>
        private string DecodeDdgUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;

            // 嘗試從 uddg 參數取得真實 URL
            Match m = Regex.Match(url, @"[?&]uddg=([^&]+)");
            if (m.Success)
            {
                try { return Uri.UnescapeDataString(m.Groups[1].Value); }
                catch { }
            }

            // 嘗試從 kbb 參數
            m = Regex.Match(url, @"[?&]kbb=([^&]+)");
            if (m.Success)
            {
                try { return Uri.UnescapeDataString(m.Groups[1].Value); }
                catch { }
            }

            // 如果是 //duckduckgo.com/l/?... 格式
            if (url.Contains("/l/?"))
            {
                m = Regex.Match(url, @"uddg=([^&]+)");
                if (m.Success)
                {
                    try { return Uri.UnescapeDataString(m.Groups[1].Value); }
                    catch { }
                }
            }

            return url;
        }
    }
}
