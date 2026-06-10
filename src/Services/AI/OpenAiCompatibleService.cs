using System;
using System.IO;
using System.Net;
using System.Text;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.AI
{
    public class OpenAiCompatibleService
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _providerName;
        private int _timeoutMs;

        public string ProviderName { get { return _providerName; } }
        public bool IsAvailable { get { return true; } }

        public OpenAiCompatibleService(string providerName, string baseUrl, string apiKey, string model)
        {
            _providerName = providerName;
            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey ?? "";
            _model = model;
            _timeoutMs = 30000; // 預設 30 秒
        }

        /// <summary>
        /// 設定超時時間（毫秒）
        /// </summary>
        public void SetTimeout(int timeoutMs)
        {
            _timeoutMs = timeoutMs;
        }

        public string SendMessage(string prompt, string systemInstruction)
        {
            try
            {
                string url = _baseUrl + "/chat/completions";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = _timeoutMs;
                request.ReadWriteTimeout = _timeoutMs;
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64)";

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    request.Headers.Add("Authorization", "Bearer " + _apiKey);
                }

                string json = BuildRequestJson(prompt, systemInstruction);
                byte[] data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;

                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(data, 0, data.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    string responseText = reader.ReadToEnd();
                    return ParseResponse(responseText);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string SendMessage(string prompt)
        {
            return SendMessage(prompt, null);
        }

        public bool TestConnection()
        {
            // 測試時使用較短超時
            int originalTimeout = _timeoutMs;
            _timeoutMs = 8000; // 測試用 8 秒
            try
            {
                string result = SendMessage("Say OK", "Reply with only the word OK");
                return result != null && result.Length > 0;
            }
            finally
            {
                _timeoutMs = originalTimeout;
            }
        }

        private string BuildRequestJson(string prompt, string systemInstruction)
        {
            string sys = string.IsNullOrEmpty(systemInstruction)
                ? "You are a helpful assistant."
                : systemInstruction;

            return "{\"model\":\"" + EscapeJson(_model) + "\","
                 + "\"messages\":["
                 + "{\"role\":\"system\",\"content\":\"" + EscapeJson(sys) + "\"},"
                 + "{\"role\":\"user\",\"content\":\"" + EscapeJson(prompt) + "\"}"
                 + "],"
                 + "\"max_tokens\":4096,"
                 + "\"temperature\":0.7}";
        }

        private string ParseResponse(string responseText)
        {
            if (string.IsNullOrEmpty(responseText)) return null;

            int choicesIdx = responseText.IndexOf("\"choices\"");
            if (choicesIdx < 0) return null;

            int contentIdx = responseText.IndexOf("\"content\"", choicesIdx);
            if (contentIdx < 0) return null;

            int colonIdx = responseText.IndexOf(":", contentIdx + 9);
            if (colonIdx < 0) return null;

            int startQuote = responseText.IndexOf("\"", colonIdx + 1);
            if (startQuote < 0) return null;

            startQuote++;
            StringBuilder sb = new StringBuilder();
            bool escaped = false;

            for (int i = startQuote; i < responseText.Length; i++)
            {
                char c = responseText[i];
                if (escaped)
                {
                    switch (c)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        default: sb.Append('\\'); sb.Append(c); break;
                    }
                    escaped = false;
                }
                else if (c == '\\') { escaped = true; }
                else if (c == '"') { break; }
                else { sb.Append(c); }
            }

            string result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}
