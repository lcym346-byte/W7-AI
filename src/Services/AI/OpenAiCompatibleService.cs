using System;
using System.IO;
using System.Net;
using System.Text;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.AI
{
    public class OpenAiCompatibleService
    {
        private string _baseUrl;
        private string _apiKey;
        private string _model;
        private string _providerName;

        public string ProviderName { get { return _providerName; } }

        public OpenAiCompatibleService(string providerName, string baseUrl, string apiKey, string model)
        {
            _providerName = providerName;
            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;
            _model = model;
        }

        public string SendMessage(string prompt, string systemInstruction)
        {
            try
            {
                string url = _baseUrl + "/chat/completions";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 120000;
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
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (StreamReader r = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        string err = r.ReadToEnd();
                        System.Diagnostics.Debug.WriteLine(_providerName + " error: " + err);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(_providerName + " exception: " + ex.Message);
                return null;
            }
        }

        public bool TestConnection()
        {
            try
            {
                string url = _baseUrl + "/models";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 15000;
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64)";

                if (!string.IsNullOrEmpty(_apiKey))
                {
                    request.Headers.Add("Authorization", "Bearer " + _apiKey);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch
            {
                // Fallback: try a simple chat request
                string result = SendMessage("Say OK", "Reply with only OK");
                return result != null && result.Length > 0;
            }
        }

        private string BuildRequestJson(string prompt, string systemInstruction)
        {
            string escapedSystem = EscapeJson(systemInstruction ?? "You are a helpful assistant.");
            string escapedPrompt = EscapeJson(prompt);

            return "{\"model\":\"" + EscapeJson(_model) + "\","
                 + "\"messages\":["
                 + "{\"role\":\"system\",\"content\":\"" + escapedSystem + "\"},"
                 + "{\"role\":\"user\",\"content\":\"" + escapedPrompt + "\"}"
                 + "],"
                 + "\"max_tokens\":4096,"
                 + "\"temperature\":0.7}";
        }

        private string ParseResponse(string responseText)
        {
            // Extract content from: "content":"..." in choices[0].message
            string marker = "\"content\"";
            int idx = responseText.IndexOf("\"choices\"");
            if (idx < 0) return null;

            idx = responseText.IndexOf(marker, idx);
            if (idx < 0) return null;

            idx = responseText.IndexOf(":", idx + marker.Length);
            if (idx < 0) return null;

            idx++;
            while (idx < responseText.Length && (responseText[idx] == ' ' || responseText[idx] == '"'))
            {
                if (responseText[idx] == '"') { idx++; break; }
                idx++;
            }

            StringBuilder sb = new StringBuilder();
            bool escaped = false;
            for (int i = idx; i < responseText.Length; i++)
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
                        default: sb.Append(c); break;
                    }
                    escaped = false;
                }
                else if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\n", "\\n")
                       .Replace("\r", "\\r")
                       .Replace("\t", "\\t");
        }
    }
}
