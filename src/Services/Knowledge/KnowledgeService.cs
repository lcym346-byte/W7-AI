using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AIAgentTool.Services.Media;

namespace AIAgentTool.Services.Knowledge
{
    /// <summary>
    /// 知識庫服務 - 透過 TurboVec 微服務做向量搜尋
    /// 用 LocalAI 的 embedding API 把文字轉向量
    /// </summary>
    public class KnowledgeService
    {
        private readonly string _turboVecUrl;
        private readonly LocalAiMediaService _media;
        private readonly string _knowledgePath;

        public string LastError { get; private set; }

        public KnowledgeService(string turboVecUrl, LocalAiMediaService media, string knowledgePath)
        {
            _turboVecUrl = (turboVecUrl ?? "http://localhost:5050").TrimEnd('/');
            _media = media;
            _knowledgePath = knowledgePath;

            if (!Directory.Exists(_knowledgePath))
                Directory.CreateDirectory(_knowledgePath);
        }

        /// <summary>
        /// 搜尋知識庫
        /// </summary>
        /// <param name="query">查詢文字</param>
        /// <param name="topK">回傳幾筆結果</param>
        /// <returns>搜尋結果的文字內容，失敗回傳 null</returns>
        public string Search(string query, int topK = 5)
        {
            try
            {
                // 第一步：用 LocalAI 把查詢轉成向量
                float[] embedding = _media.GetEmbedding(query);
                if (embedding == null || embedding.Length == 0)
                {
                    LastError = "Failed to get embedding: " + _media.LastError;
                    return null;
                }

                // 第二步：送到 TurboVec 搜尋
                StringBuilder vecJson = new StringBuilder("[");
                for (int i = 0; i < embedding.Length; i++)
                {
                    if (i > 0) vecJson.Append(",");
                    vecJson.Append(embedding[i].ToString("G"));
                }
                vecJson.Append("]");

                string body = string.Format(
                    "{{\"vector\":{0},\"k\":{1}}}",
                    vecJson.ToString(), topK);

                string response = PostJson(_turboVecUrl + "/search", body);
                if (response == null) return null;

                // 第三步：根據回傳的 ID 讀取對應文件內容
                return ParseSearchResults(response);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 把文件加入知識庫
        /// </summary>
        /// <param name="filePath">文件路徑（.txt）</param>
        /// <returns>是否成功</returns>
        public bool AddDocument(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LastError = "File not found: " + filePath;
                    return false;
                }

                string content = File.ReadAllText(filePath, Encoding.UTF8);
                string fileName = Path.GetFileName(filePath);

                // 把文件切成段落
                string[] paragraphs = content.Split(
                    new string[] { "\r\n\r\n", "\n\n" },
                    StringSplitOptions.RemoveEmptyEntries);

                int addedCount = 0;
                foreach (string para in paragraphs)
                {
                    string trimmed = para.Trim();
                    if (trimmed.Length < 20) continue; // 太短跳過

                    float[] embedding = _media.GetEmbedding(trimmed);
                    if (embedding == null) continue;

                    // 送到 TurboVec
                    StringBuilder vecJson = new StringBuilder("[");
                    for (int i = 0; i < embedding.Length; i++)
                    {
                        if (i > 0) vecJson.Append(",");
                        vecJson.Append(embedding[i].ToString("G"));
                    }
                    vecJson.Append("]");

                    string docId = Guid.NewGuid().ToString("N").Substring(0, 16);
                    string body = string.Format(
                        "{{\"vector\":{0},\"id\":\"{1}\",\"text\":\"{2}\",\"source\":\"{3}\"}}",
                        vecJson.ToString(), docId,
                        EscapeJson(Truncate(trimmed, 2000)),
                        EscapeJson(fileName));

                    PostJson(_turboVecUrl + "/add", body);
                    addedCount++;
                }

                return addedCount > 0;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 測試 TurboVec 連線
        /// </summary>
        public bool TestConnection()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_turboVecUrl + "/status");
                req.Method = "GET";
                req.Timeout = 5000;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    return resp.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                return false;
            }
        }

        private string ParseSearchResults(string json)
        {
            // 簡易解析 {"ids": [...], "scores": [...], "texts": [...]}
            StringBuilder sb = new StringBuilder();

            // 嘗試取得 texts 陣列
            int textsStart = json.IndexOf("\"texts\"");
            if (textsStart >= 0)
            {
                int arrStart = json.IndexOf("[", textsStart);
                int arrEnd = FindMatchingBracket(json, arrStart);
                if (arrStart >= 0 && arrEnd > arrStart)
                {
                    string arrContent = json.Substring(arrStart + 1, arrEnd - arrStart - 1);
                    // 分割各個文字結果
                    MatchCollection matches = Regex.Matches(arrContent, "\"((?:[^\"\\\\]|\\\\.)*)\"");
                    int idx = 1;
                    foreach (Match m in matches)
                    {
                        string text = m.Groups[1].Value
                            .Replace("\\n", "\n").Replace("\\\"", "\"");
                        sb.AppendLine(string.Format("[{0}] {1}", idx, text));
                        sb.AppendLine();
                        idx++;
                    }
                }
            }

            return sb.Length > 0 ? sb.ToString() : json;
        }

        private int FindMatchingBracket(string s, int openPos)
        {
            if (openPos < 0 || openPos >= s.Length) return -1;
            int depth = 0;
            for (int i = openPos; i < s.Length; i++)
            {
                if (s[i] == '[') depth++;
                if (s[i] == ']') { depth--; if (depth == 0) return i; }
            }
            return -1;
        }

        private string PostJson(string url, string body)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Timeout = 30000;

                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
                req.ContentLength = bodyBytes.Length;

                using (Stream s = req.GetRequestStream())
                    s.Write(bodyBytes, 0, bodyBytes.Length);

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max);
        }
    }
}
