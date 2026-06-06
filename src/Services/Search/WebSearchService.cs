using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AIAgentTool.Models;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.Search
{
    public class WebSearchService
    {
        private const string DDG_INSTANT_URL =
            "https://api.duckduckgo.com/?q={0}&format=json&no_redirect=1&no_html=1";
        private const string DDG_HTML_URL =
            "https://html.duckduckgo.com/html/?q={0}";

        private readonly string _userAgent;

        public WebSearchService()
        {
            _userAgent = "Mozilla/5.0 (Windows NT 6.1; rv:109.0) Gecko/20100101 Firefox/115.0";
        }

        public List<SearchResult> SearchAll(string query)
        {
            List<SearchResult> allResults = new List<SearchResult>();

            List<SearchResult> instantResults = null;
            List<SearchResult> htmlResults = null;
            Exception instantError = null;
            Exception htmlError = null;

            Thread t1 = new Thread(delegate()
            {
                try { instantResults = SearchDuckDuckGoInstant(query); }
                catch (Exception ex) { instantError = ex; }
            });

            Thread t2 = new Thread(delegate()
            {
                try { htmlResults = SearchDuckDuckGoHtml(query); }
                catch (Exception ex) { htmlError = ex; }
            });

            t1.Start();
            t2.Start();
            t1.Join(15000);
            t2.Join(15000);

            if (instantResults != null) allResults.AddRange(instantResults);
            if (htmlResults != null) allResults.AddRange(htmlResults);

            return allResults;
        }

        public List<SearchResult> SearchDuckDuckGoInstant(string query)
        {
            List<SearchResult> results = new List<SearchResult>();

            try
            {
                string url = string.Format(DDG_INSTANT_URL, Uri.EscapeDataString(query));
                string json = HttpGet(url);

                string abstractText = HtmlHelper.ExtractJsonValue(json, "AbstractText");
                string abstractSource = HtmlHelper.ExtractJsonValue(json, "AbstractSource");
                string abstractUrl = HtmlHelper.ExtractJsonValue(json, "AbstractURL");
                string heading = HtmlHelper.ExtractJsonValue(json, "Heading");

                if (!string.IsNullOrEmpty(abstractText) && abstractText.Length > 10)
                {
                    results.Add(new SearchResult
                    {
                        Title = heading,
                        Snippet = abstractText,
                        Url = abstractUrl,
                        Source = "DuckDuckGo/" + abstractSource,
                        RelevanceScore = 0.9
                    });
                }

                string answer = HtmlHelper.ExtractJsonValue(json, "Answer");
                if (!string.IsNullOrEmpty(answer))
                {
                    results.Add(new SearchResult
                    {
                        Title = "直接答案: " + heading,
                        Snippet = HtmlHelper.StripHtml(answer),
                        Url = "",
                        Source = "DuckDuckGo/Instant",
                        RelevanceScore = 1.0
                    });
                }

                string definition = HtmlHelper.ExtractJsonValue(json, "Definition");
                if (!string.IsNullOrEmpty(definition))
                {
                    results.Add(new SearchResult
                    {
                        Title = "定義: " + heading,
                        Snippet = definition,
                        Url = HtmlHelper.ExtractJsonValue(json, "DefinitionURL"),
                        Source = "DuckDuckGo/Definition",
                        RelevanceScore = 0.85
                    });
                }

                List<string> relatedObjects =
                    HtmlHelper.ExtractJsonArrayObjects(json, "RelatedTopics");
                foreach (string obj in relatedObjects)
                {
                    string text = HtmlHelper.ExtractJsonValue(obj, "Text");
                    string firstUrl = HtmlHelper.ExtractJsonValue(obj, "FirstURL");
                    if (!string.IsNullOrEmpty(text) && text.Length > 10)
                    {
                        results.Add(new SearchResult
                        {
                            Title = HtmlHelper.TruncateText(text, 60),
                            Snippet = text,
                            Url = firstUrl,
                            Source = "DuckDuckGo/Related",
                            RelevanceScore = 0.6
                        });
                    }
                    if (results.Count >= 10) break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DuckDuckGo Instant 搜尋失敗: " + ex.Message);
            }

            return results;
        }

        public List<SearchResult> SearchDuckDuckGoHtml(string query)
        {
            List<SearchResult> results = new List<SearchResult>();

            try
            {
                string url = string.Format(DDG_HTML_URL, Uri.EscapeDataString(query));
                string html = HttpGet(url);

                MatchCollection matches = Regex.Matches(html,
                    @"<a[^>]+class=""result__a""[^>]*href=""([^""]+)""[^>]*>([\s\S]*?)</a>" +
                    @"[\s\S]*?class=""result__snippet""[^>]*>([\s\S]*?)</",
                    RegexOptions.IgnoreCase);

                foreach (Match m in matches)
                {
                    string resultUrl = WebUtility.HtmlDecode(m.Groups[1].Value);
                    string title = HtmlHelper.StripHtml(m.Groups[2].Value);
                    string snippet = HtmlHelper.StripHtml(m.Groups[3].Value);

                    resultUrl = DecodeDdgUrl(resultUrl);

                    if (!string.IsNullOrEmpty(title) && title.Length > 2)
                    {
                        results.Add(new SearchResult
                        {
                            Title = title,
                            Snippet = snippet,
                            Url = resultUrl,
                            Source = "DuckDuckGo/Web",
                            RelevanceScore = 0.7
                        });
                    }
                    if (results.Count >= 10) break;
                }

                if (results.Count == 0)
                {
                    matches = Regex.Matches(html,
                        @"class=""result__title""[^>]*>[\s\S]*?<a[^>]*href=""([^""]+)""[^>]*>([\s\S]*?)</a>",
                        RegexOptions.IgnoreCase);

                    foreach (Match m in matches)
                    {
                        string resultUrl = DecodeDdgUrl(
                            WebUtility.HtmlDecode(m.Groups[1].Value));
                        string title = HtmlHelper.StripHtml(m.Groups[2].Value);

                        if (!string.IsNullOrEmpty(title) && title.Length > 2)
                        {
                            results.Add(new SearchResult
                            {
                                Title = title,
                                Url = resultUrl,
                                Snippet = "",
                                Source = "DuckDuckGo/Web",
                                RelevanceScore = 0.5
                            });
                        }
                        if (results.Count >= 8) break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DuckDuckGo HTML 搜尋失敗: " + ex.Message);
            }

            return results;
        }

                public string HttpGet(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = _userAgent;
                request.Accept = "text/html,application/json,*/*";
                request.Headers.Add("Accept-Language", "zh-TW,zh;q=0.9,en;q=0.8");
                request.Headers.Add("Cache-Control", "no-cache");
                request.Headers.Add("Pragma", "no-cache");
                request.Referer = "https://duckduckgo.com/";
                request.Timeout = 15000;
                request.ReadWriteTimeout = 15000;
                request.AutomaticDecompression =
                    DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return null;
            }
        }


        private string DecodeDdgUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;

            Match uddgMatch = Regex.Match(url, @"uddg=([^&]+)");
            if (uddgMatch.Success)
            {
                return Uri.UnescapeDataString(uddgMatch.Groups[1].Value);
            }

            if (url.StartsWith("//duckduckgo.com/l/?"))
            {
                Match kbbMatch = Regex.Match(url, @"kbb=([^&]+)");
                if (kbbMatch.Success)
                    return Uri.UnescapeDataString(kbbMatch.Groups[1].Value);
            }

            return url;
        }
    }
}
