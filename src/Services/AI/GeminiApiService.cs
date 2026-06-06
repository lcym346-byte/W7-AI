using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.AI
{
    public class GeminiApiService
    {
        private const string API_URL =
            "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";
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

        public string SendMessage(string prompt, string systemInstruction)
        {
            if (!IsAvailable)
                return null;

            try
            {
                string url = string.Format(API_URL, _model, _apiKey);
                string requestBody = BuildRequestJson(prompt, systemInstruction);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = 30000;
                request.ReadWriteTimeout = 30000;

                byte[] bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = bodyBytes.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bodyBytes, 0, bodyBytes.Length);
                }

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
                if (ex.Response != null)
                {
                    try
                    {
                        using (Stream errStream = ex.Response.GetResponseStream())
                        using (StreamReader errReader = new StreamReader(errStream))
                        {
                            string errBody = errReader.ReadToEnd();
                            Debug.WriteLine("Gemini API 錯誤: " + errBody);
                        }
                    }
                    catch { }
                }
                Debug.WriteLine("Gemini API 請求失敗: " + ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Gemini API 例外: " + ex.Message);
                return null;
            }
        }

        public string SendMessage(string prompt)
        {
            return SendMessage(prompt, null);
        }

        private string BuildRequestJson(string prompt, string systemInstruction)
        {
            string escapedPrompt = EscapeJson(prompt);

            StringBuilder sb = new StringBuilder();
            sb.Append("{");

            if (!string.IsNullOrEmpty(systemInstruction))
            {
                string escapedSystem = EscapeJson(systemInstruction);
                sb.Append("\"system_instruction\":{\"parts\":[{\"text\":\"");
                sb.Append(escapedSystem);
                sb.Append("\"}]},");
            }

            sb.Append("\"contents\":[{\"parts\":[{\"text\":\"");
            sb.Append(escapedPrompt);
            sb.Append("\"}]}],");
            sb.Append("\"generationConfig\":{");
            sb.Append("\"temperature\":0.7,");
            sb.Append("\"maxOutputTokens\":4096");
            sb.Append("}");
            sb.Append("}");

            return sb.ToString();
        }

        private string ParseResponse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var candidates = HtmlHelper.ExtractJsonArrayObjects(json, "candidates");
            if (candidates.Count == 0) return null;

            string candidate = candidates[0];
            var parts = HtmlHelper.ExtractJsonArrayObjects(candidate, "parts");
            if (parts.Count == 0) return null;

            string text = HtmlHelper.ExtractJsonValue(parts[0], "text");
            return string.IsNullOrEmpty(text) ? null : text;
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

        public bool TestConnection()
        {
            string result = SendMessage("Hello, respond with just 'OK'.");
            return result != null;
        }
    }
}
