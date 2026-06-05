using System;
using System.IO;
using System.Net;
using System.Text;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.AI
{
    /// <summary>
    /// Google Gemini API 服務
    /// 免費額度：5000 次/月
    /// 使用 REST API (HttpWebRequest)，.NET 4.0 相容
    /// </summary>
    public class GeminiApiService
    {
        // Gemini API 端點 (使用 v1beta，支援免費 tier)
        private const string API_URL =
            "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

        // 預設模型 (免費 tier 可用)
        private const string DEFAULT_MODEL = "gemini-2.0-flash";

        private readonly string _apiKey;
        private readonly string _model;

        public bool IsAvailable { get { return !string.IsNullOrEmpty(_apiKey); } }

        public GeminiApiService(string apiKey, string model)
        {
            _apiKey = apiKey ?? "";
            _model = string.IsNullOrEmpty(model) ? DEFAULT_MODEL : model;
        }

        public GeminiApiService(string apiKey) : this(apiKey, DEFAULT_MODEL) { }

        /// <summary>
        /// 發送訊息給 Gemini 並取得回應
        /// </summary>
        /// <param name="prompt">使用者提示詞</param>
        /// <param name="systemInstruction">系統指令（可選）</param>
        /// <returns>AI 回應文字，失敗回傳 null</returns>
        public string SendMessage(string prompt, string systemInstruction)
        {
            if (!IsAvailable)
                return null;

            try
            {
                string url = string.Format(API_URL, _model, _apiKey);

                // 建構 JSON 請求本體
                string requestBody = BuildRequestJson(prompt, systemInstruction);

                // 發送 POST 請求
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = 30000; // 30 秒逾時
                request.ReadWriteTimeout = 30000;

                byte[] bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = bodyBytes.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bodyBytes, 0, bodyBytes.Length);
                }

                // 讀取回應
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                {
                    string responseJson = reader.ReadToEnd();
                    return ParseResponse(responseJson);
                }
            }
            catch (WebException ex)
            {
                // 嘗試讀取錯誤回應
                if (ex.Response != null)
                {
                    try
                    {
                        using (Stream errStream = ex.Response.GetResponseStream())
                        using (StreamReader errReader = new StreamReader(errStream))
                        {
                            string errBody = errReader.ReadToEnd();
                            System.Diagnostics.Debug.WriteLine(
                                "Gemini API 錯誤: " + errBody);
                        }
                    }
                    catch { }
                }
                System.Diagnostics.Debug.WriteLine(
                    "Gemini API 請求失敗: " + ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Gemini API 例外: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 發送訊息（不含系統指令）
        /// </summary>
        public string SendMessage(string prompt)
        {
            return SendMessage(prompt, null);
        }

        /// <summary>
        /// 建構 Gemini API 請求 JSON
        /// </summary>
        private string BuildRequestJson(string prompt, string systemInstruction)
        {
            // 跳脫 JSON 特殊字元
            string escapedPrompt = EscapeJson(prompt);

            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            // 系統指令 (可選)
            if (!string.IsNullOrEmpty(systemInstruction))
            {
                string escapedSystem = EscapeJson(systemInstruction);
                sb.Append("\"system_instruction\":{\"parts\":[{\"text\":\"");
                sb.Append(escapedSystem);
                sb.Append("\"}]},");
            }

            // 使用者訊息
            sb.Append("\"contents\":[{\"parts\":[{\"text\":\"");
            sb.Append(escapedPrompt);
            sb.Append("\"}]}],");

            // 生成設定
            sb.Append("\"generationConfig\":{");
            sb.Append("\"temperature\":0.7,");
            sb.Append("\"maxOutputTokens\":4096");
            sb.Append("}");

            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// 解析 Gemini API 回應 JSON
        /// </summary>
        private string ParseResponse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            // Gemini 回應格式:
            // {"candidates":[{"content":{"parts":[{"text":"..."}]}}]}
            // 需要找到 candidates[0].content.parts[0].text

            // 先找 candidates 陣列中的第一個物件
            var candidates = HtmlHelper.ExtractJsonArrayObjects(json, "candidates");
            if (candidates.Count == 0) return null;

            string candidate = candidates[0];

            // 從 candidate 中找 parts 陣列
            var parts = HtmlHelper.ExtractJsonArrayObjects(candidate, "parts");
            if (parts.Count == 0) return null;

            // 從第一個 part 中擷取 text
            string text = HtmlHelper.ExtractJsonValue(parts[0], "text");

            return string.IsNullOrEmpty(text) ? null : text;
        }

        /// <summary>
        /// JSON 字串跳脫
        /// </summary>
        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }

        /// <summary>
        /// 測試 API Key 是否有效
        /// </summary>
        public bool TestConnection()
        {
            string result = SendMessage("Hello, respond with just 'OK'.");
            return result != null;
        }
    }
}
