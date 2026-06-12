using System;
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
        private string _vqdHash;

        public DuckDuckGoAiService(string model)
        {
            _model = string.IsNullOrEmpty(model) ? MODEL_GPT4O_MINI : model;
            _vqd = "";
            _vqdHash = "";
        }

        public DuckDuckGoAiService() : this(MODEL_GPT4O_MINI) { }

        public string SendMessage(string prompt)
        {
            try
            {
                if (!ObtainVqdToken())
                    return null;

                string requestBody = BuildChatJson(prompt);

                // 強制 TLS 1.2
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(CHAT_URL);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "text/event-stream";
                request.Headers.Add("x-vqd-4", _vqd);
                if (!string.IsNullOrEmpty(_vqdHash))
                    request.Headers.Add("x-vqd-hash-1", _vqdHash);
                request.Headers.Add("accept-language", "en-US,en;q=0.9");
                request.Referer = "https://duckduckgo.com/";
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
                    string newHash = response.Headers["x-vqd-hash-1"];
                    if (!string.IsNullOrEmpty(newHash))
                        _vqdHash = newHash;

                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        return ParseSseResponse(reader);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool ObtainVqdToken()
        {
            try
            {
                // 強制 TLS 1.2
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(STATUS_URL);
                request.Method = "GET";
                request.Accept = "text/event-stream";
                request.Headers.Add("x-vqd-accept", "1");
                request.Headers.Add("accept-language", "en-US,en;q=0.9");
                request.Headers.Add("cache-control", "no-cache");
                request.Headers.Add("pragma", "no-cache");
                request.Referer = "https://duckduckgo.com/";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; rv:109.0) Gecko/20100101 Firefox/115.0";
                request.Timeout = 10000;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    _vqd = response.Headers["x-vqd-4"];
                    string hash = response.Headers["x-vqd-hash-1"];
                    if (!string.IsNullOrEmpty(hash))
                        _vqdHash = hash;
                    return !string.IsNullOrEmpty(_vqd);
                }
            }
            catch (Exception)
            {
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
