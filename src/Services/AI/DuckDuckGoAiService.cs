using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.AI
{
    public class DuckDuckGoAiService
    {
        private const string STATUS_URL = "https://duckduckgo.com/duckchat/v1/status";
        private const string CHAT_URL = "https://duckduckgo.com/duckchat/v1/chat";

        public const string MODEL_GPT4O_MINI = "gpt-4o-mini";
        public const string MODEL_CLAUDE_HAIKU = "claude-3-haiku-20240307";
        public const string MODEL_LLAMA = "meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo";
        public const string MODEL_MIXTRAL = "mistralai/Mixtral-8x7B-Instruct-v0.1";

        private readonly string _model;
        private string _vqd;

        public DuckDuckGoAiService(string model)
        {
            _model = string.IsNullOrEmpty(model) ? MODEL_GPT4O_MINI : model;
            _vqd = "";
        }

        public DuckDuckGoAiService() : this(MODEL_GPT4O_MINI) { }

        public string SendMessage(string prompt)
        {
            try
            {
                if (!ObtainVqdToken())
                    return null;

                string requestBody = BuildChatJson(prompt);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(CHAT_URL);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "text/event-stream";
                request.Headers.Add("x-vqd-4", _vqd);
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; rv:109.0) Gecko/20100101 Firefox/115.0";
                request.Timeout = 30000;
                request.ReadWriteTimeout = 30000;

                byte[] bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                request.ContentLength = bodyBytes.Length;

                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(bodyBytes, 0, bodyBytes.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    string newVqd = response.Headers["x-vqd-4"];
                    if (!string.IsNullOrEmpty(newVqd))
                        _vqd = newVqd;

                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        return ParseSseResponse(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DuckDuckGo AI 請求失敗: " + ex.Message);
                return null;
            }
        }

        private bool ObtainVqdToken()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(STATUS_URL);
                request.Method = "GET";
                request.Headers.Add("x-vqd-accept", "1");
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; rv:109.0) Gecko/20100101 Firefox/115.0";
                request.Timeout = 10000;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    _vqd = response.Headers["x-vqd-4"];
                    return !string.IsNullOrEmpty(_vqd);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("取得 VQD Token 失敗: " + ex.Message);
                return false;
            }
        }

        private string BuildChatJson(string prompt)
        {
            string escapedPrompt = prompt
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");

            return string.Format(
                "{{\"model\":\"{0}\",\"messages\":[{{\"role\":\"user\",\"content\":\"{1}\"}}]}}",
                _model, escapedPrompt);
        }

        private string ParseSseResponse(StreamReader reader)
        {
            StringBuilder fullResponse = new StringBuilder();
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (!line.StartsWith("data: "))
                    continue;

                string data = line.Substring(6);

                if (data == "[DONE]")
                    break;

                string message = HtmlHelper.ExtractJsonValue(data, "message");
                if (!string.IsNullOrEmpty(message))
                {
                    fullResponse.Append(message);
                }
            }

            string result = fullResponse.ToString();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        public bool TestConnection()
        {
            return ObtainVqdToken();
        }
    }
}
