using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace AIAgentTool.Utils
{
    /// <summary>
    /// HTML 文字處理 + 簡易 JSON 解析工具
    /// 不依賴外部套件，純正則表達式實作
    /// </summary>
    public static class HtmlHelper
    {
        /// <summary>
        /// 移除所有 HTML 標籤，回傳純文字
        /// </summary>
        public static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            string result = Regex.Replace(html,
                @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            result = Regex.Replace(result,
                @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);

            result = Regex.Replace(result,
                @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result,
                @"</(p|div|h[1-6]|li|tr)>", "\n", RegexOptions.IgnoreCase);

            result = Regex.Replace(result, @"<[^>]+>", "");
            result = WebUtility.HtmlDecode(result);

            result = Regex.Replace(result, @"[ \t]+", " ");
            result = Regex.Replace(result, @"\n\s*\n\s*\n", "\n\n");

            return result.Trim();
        }

        /// <summary>
        /// 從 HTML 中擷取指定標籤的內容
        /// </summary>
        public static string ExtractTagContent(string html, string tagName)
        {
            if (string.IsNullOrEmpty(html)) return "";

            string pattern = string.Format(
                @"<{0}[^>]*>([\s\S]*?)</{0}>", tagName);
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);

            return match.Success ? StripHtml(match.Groups[1].Value) : "";
        }

        /// <summary>
        /// 從 HTML 中擷取所有連結 (href, text)
        /// </summary>
        public static List<KeyValuePair<string, string>> ExtractLinks(string html)
        {
            var links = new List<KeyValuePair<string, string>>();
            if (string.IsNullOrEmpty(html)) return links;

            var matches = Regex.Matches(html,
                @"<a\s+[^>]*href\s*=\s*[""']([^""']+)[""'][^>]*>([\s\S]*?)</a>",
                RegexOptions.IgnoreCase);

            foreach (Match m in matches)
            {
                string url = m.Groups[1].Value;
                string text = StripHtml(m.Groups[2].Value);
                if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(url))
                {
                    links.Add(new KeyValuePair<string, string>(url, text));
                }
            }
            return links;
        }

        /// <summary>
        /// 智慧截斷文字，保持完整句子
        /// </summary>
        public static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            string truncated = text.Substring(0, maxLength);
            int lastPeriod = truncated.LastIndexOfAny(
                new char[] { '.', '。', '!', '?', '！', '？' });

            if (lastPeriod > maxLength / 2)
                return truncated.Substring(0, lastPeriod + 1);

            int lastSpace = truncated.LastIndexOf(' ');
            if (lastSpace > maxLength / 2)
                return truncated.Substring(0, lastSpace) + "...";

            return truncated + "...";
        }

        // ============================================================
        // JSON 解析工具 (不依賴 Newtonsoft.Json)
        // ============================================================

        /// <summary>
        /// 從 JSON 中擷取字串值
        /// 支援: "key": "value"
        /// </summary>
        public static string ExtractJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return "";

            string pattern = string.Format(
                @"""{0}""\s*:\s*""((?:[^""\\]|\\.)*)""", Regex.Escape(key));
            var match = Regex.Match(json, pattern);

            if (match.Success)
            {
                string raw = match.Groups[1].Value;
                raw = UnescapeUnicode(raw);
                raw = raw.Replace("\\\"", "\"")
                         .Replace("\\n", "\n")
                         .Replace("\\r", "\r")
                         .Replace("\\t", "\t")
                         .Replace("\\\\", "\\");
                return raw;
            }

            // 嘗試非字串值 (number, bool, null)
            pattern = string.Format(
                @"""{0}""\s*:\s*([^,\}}\]\s]+)", Regex.Escape(key));
            match = Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        /// <summary>
        /// 從 JSON 中擷取整數值
        /// </summary>
        public static int ExtractJsonInt(string json, string key, int defaultValue)
        {
            string value = ExtractJsonValue(json, key);
            int result;
            if (int.TryParse(value, out result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// 從 JSON 陣列中擷取所有物件字串
        /// 支援: "key": [ {...}, {...} ]
        /// </summary>
        public static List<string> ExtractJsonArrayObjects(string json, string arrayKey)
        {
            var objects = new List<string>();
            if (string.IsNullOrEmpty(json)) return objects;

            string pattern = string.Format(
                @"""{0}""\s*:\s*\[([\s\S]*?)\]", Regex.Escape(arrayKey));
            var match = Regex.Match(json, pattern);
            if (!match.Success) return objects;

            string arrayContent = match.Groups[1].Value;
            int depth = 0;
            int start = -1;

            for (int i = 0; i < arrayContent.Length; i++)
            {
                char c = arrayContent[i];
                if (c == '{')
                {
                    if (depth == 0) start = i;
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        objects.Add(arrayContent.Substring(start, i - start + 1));
                        start = -1;
                    }
                }
            }
            return objects;
        }

        /// <summary>
        /// 從 JSON 陣列中擷取所有字串值
        /// 支援: "key": ["a", "b", "c"]
        /// </summary>
        public static List<string> ExtractJsonStringArray(string json, string arrayKey)
        {
            var items = new List<string>();
            if (string.IsNullOrEmpty(json)) return items;

            string pattern = string.Format(
                @"""{0}""\s*:\s*\[([\s\S]*?)\]", Regex.Escape(arrayKey));
            var match = Regex.Match(json, pattern);
            if (!match.Success) return items;

            var stringMatches = Regex.Matches(match.Groups[1].Value,
                @"""((?:[^""\\]|\\.)*)""");
            foreach (Match sm in stringMatches)
            {
                items.Add(sm.Groups[1].Value
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\"));
            }
            return items;
        }

        /// <summary>
        /// 擷取 code block (```...```) 中的程式碼
        /// 用於從 AI 回應中擷取程式碼
        /// </summary>
        public static string ExtractCodeBlock(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // 嘗試匹配 ```csharp ... ``` 或 ```cs ... ``` 或 ``` ... ```
            var match = Regex.Match(text,
                @"```(?:csharp|cs|c#)?\s*\n([\s\S]*?)```",
                RegexOptions.IgnoreCase);

            if (match.Success)
                return UnescapeUnicode(match.Groups[1].Value.Trim());

            // 如果沒有 code block，檢查是否整段都是程式碼
            if (text.Contains("using System") || text.Contains("namespace ") ||
                text.Contains("class ") || text.Contains("static void Main"))
            {
                return UnescapeUnicode(text.Trim());
            }

            return "";
        }

        /// <summary>
        /// 將 \uXXXX 格式的 Unicode 轉義還原為實際字元
        /// 例如 \u003c 還原為 <, \u003e 還原為 >, \u0026 還原為 &
        /// </summary>
        private static string UnescapeUnicode(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (!text.Contains("\\u")) return text;

            return Regex.Replace(text, @"\\u([0-9a-fA-F]{4})", delegate(Match m)
            {
                int code = Convert.ToInt32(m.Groups[1].Value, 16);
                return ((char)code).ToString();
            });
        }
    }
}
