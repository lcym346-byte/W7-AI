using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace AIAgentTool.Services.Media
{
    /// <summary>
    /// LocalAI 多媒體服務 - 圖片生成、語音合成、語音辨識
    /// 透過 OpenAI 相容 API 與本地 LocalAI 引擎通訊
    /// </summary>
    public class LocalAiMediaService
    {
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly int _timeoutMs;

        // 預設模型名稱（可在 LocalAI 中配置）
        private string _imageModel = "stablediffusion";
        private string _ttsModel = "tts-1";
        private string _sttModel = "whisper-1";

        public bool IsAvailable { get; private set; }
        public string LastError { get; private set; }

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="baseUrl">LocalAI 伺服器位址，如 http://localhost:8080</param>
        /// <param name="apiKey">API Key（LocalAI 預設不需要，留空即可）</param>
        public LocalAiMediaService(string baseUrl, string apiKey = "")
        {
            _baseUrl = (baseUrl ?? "http://localhost:8080").TrimEnd('/');
            _apiKey = apiKey ?? "";
            _timeoutMs = 120000; // 圖片生成可能需要較長時間
            IsAvailable = false;
            LastError = "";
        }

        /// <summary>
        /// 設定模型名稱
        /// </summary>
        public void SetModels(string imageModel, string ttsModel, string sttModel)
        {
            if (!string.IsNullOrEmpty(imageModel)) _imageModel = imageModel;
            if (!string.IsNullOrEmpty(ttsModel)) _ttsModel = ttsModel;
            if (!string.IsNullOrEmpty(sttModel)) _sttModel = sttModel;
        }

        /// <summary>
        /// 測試 LocalAI 連線
        /// </summary>
        public bool TestConnection()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_baseUrl + "/v1/models");
                req.Method = "GET";
                req.Timeout = 10000;
                if (!string.IsNullOrEmpty(_apiKey))
                    req.Headers["Authorization"] = "Bearer " + _apiKey;

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    IsAvailable = (resp.StatusCode == HttpStatusCode.OK);
                    return IsAvailable;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                IsAvailable = false;
                return false;
            }
        }

        /// <summary>
        /// 生成圖片 - 回傳儲存的檔案路徑
        /// </summary>
        /// <param name="prompt">圖片描述（中文或英文皆可）</param>
        /// <param name="savePath">儲存路徑（資料夾）</param>
        /// <param name="size">圖片大小，如 "512x512"</param>
        /// <returns>生成的圖片完整路徑，失敗回傳 null</returns>
        public string GenerateImage(string prompt, string savePath, string size = "512x512")
        {
            try
            {
                string url = _baseUrl + "/v1/images/generations";

                string jsonBody = string.Format(
                    "{{\"prompt\":\"{0}\",\"model\":\"{1}\",\"size\":\"{2}\",\"n\":1,\"response_format\":\"b64_json\"}}",
                    EscapeJson(prompt), _imageModel, size);

                string response = PostJson(url, jsonBody);
                if (response == null) return null;

                // 解析 base64 圖片資料
                string b64 = ExtractValue(response, "b64_json");
                if (string.IsNullOrEmpty(b64))
                {
                    // 嘗試 URL 格式
                    string imageUrl = ExtractValue(response, "url");
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        return DownloadFile(imageUrl, savePath, "generated_image.png");
                    }
                    LastError = "No image data in response";
                    return null;
                }

                // 儲存 base64 圖片
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                string fileName = string.Format("img_{0}.png",
                    DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                string filePath = Path.Combine(savePath, fileName);

                byte[] imageBytes = Convert.FromBase64String(b64);
                File.WriteAllBytes(filePath, imageBytes);

                return filePath;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 文字轉語音 - 回傳音檔路徑
        /// </summary>
        /// <param name="text">要朗讀的文字</param>
        /// <param name="savePath">儲存路徑（資料夾）</param>
        /// <param name="voice">語音名稱（依 LocalAI 配置）</param>
        /// <returns>音檔路徑，失敗回傳 null</returns>
        public string TextToSpeech(string text, string savePath, string voice = "alloy")
        {
            try
            {
                string url = _baseUrl + "/v1/audio/speech";

                string jsonBody = string.Format(
                    "{{\"input\":\"{0}\",\"model\":\"{1}\",\"voice\":\"{2}\",\"response_format\":\"mp3\"}}",
                    EscapeJson(text), _ttsModel, voice);

                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                string fileName = string.Format("tts_{0}.mp3",
                    DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                string filePath = Path.Combine(savePath, fileName);

                // TTS 回傳的是二進位音訊資料
                DownloadBinary(url, jsonBody, filePath);

                if (File.Exists(filePath) && new FileInfo(filePath).Length > 0)
                    return filePath;

                LastError = "TTS output file is empty";
                return null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 語音轉文字
        /// </summary>
        /// <param name="audioFilePath">音檔路徑</param>
        /// <returns>辨識的文字，失敗回傳 null</returns>
        public string SpeechToText(string audioFilePath)
        {
            try
            {
                if (!File.Exists(audioFilePath))
                {
                    LastError = "Audio file not found: " + audioFilePath;
                    return null;
                }

                string url = _baseUrl + "/v1/audio/transcriptions";
                string boundary = "----FormBoundary" + DateTime.Now.Ticks.ToString("x");

                byte[] fileBytes = File.ReadAllBytes(audioFilePath);
                string fileName = Path.GetFileName(audioFilePath);

                // 建構 multipart/form-data
                StringBuilder sb = new StringBuilder();
                sb.Append("--" + boundary + "\r\n");
                sb.Append("Content-Disposition: form-data; name=\"model\"\r\n\r\n");
                sb.Append(_sttModel + "\r\n");
                sb.Append("--" + boundary + "\r\n");
                sb.Append(string.Format(
                    "Content-Disposition: form-data; name=\"file\"; filename=\"{0}\"\r\n", fileName));
                sb.Append("Content-Type: application/octet-stream\r\n\r\n");

                byte[] headerBytes = Encoding.UTF8.GetBytes(sb.ToString());
                byte[] footerBytes = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "multipart/form-data; boundary=" + boundary;
                req.Timeout = _timeoutMs;
                if (!string.IsNullOrEmpty(_apiKey))
                    req.Headers["Authorization"] = "Bearer " + _apiKey;

                req.ContentLength = headerBytes.Length + fileBytes.Length + footerBytes.Length;

                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(headerBytes, 0, headerBytes.Length);
                    reqStream.Write(fileBytes, 0, fileBytes.Length);
                    reqStream.Write(footerBytes, 0, footerBytes.Length);
                }

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    return ExtractValue(json, "text");
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 取得可用模型列表
        /// </summary>
        public string GetAvailableModels()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_baseUrl + "/v1/models");
                req.Method = "GET";
                req.Timeout = 10000;
                if (!string.IsNullOrEmpty(_apiKey))
                    req.Headers["Authorization"] = "Bearer " + _apiKey;

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        // ========== 內部方法 ==========

        private string PostJson(string url, string jsonBody)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Timeout = _timeoutMs;
                if (!string.IsNullOrEmpty(_apiKey))
                    req.Headers["Authorization"] = "Bearer " + _apiKey;

                byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                req.ContentLength = bodyBytes.Length;

                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(bodyBytes, 0, bodyBytes.Length);
                }

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (StreamReader reader = new StreamReader(wex.Response.GetResponseStream()))
                    {
                        LastError = reader.ReadToEnd();
                    }
                }
                else
                {
                    LastError = wex.Message;
                }
                return null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        private void DownloadBinary(string url, string jsonBody, string saveTo)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Timeout = _timeoutMs;
            if (!string.IsNullOrEmpty(_apiKey))
                req.Headers["Authorization"] = "Bearer " + _apiKey;

            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            req.ContentLength = bodyBytes.Length;

            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream respStream = resp.GetResponseStream())
            using (FileStream fs = new FileStream(saveTo, FileMode.Create))
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = respStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, bytesRead);
                }
            }
        }

        private string DownloadFile(string fileUrl, string savePath, string defaultName)
        {
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            string filePath = Path.Combine(savePath, defaultName);

            using (WebClient wc = new WebClient())
            {
                wc.DownloadFile(fileUrl, filePath);
            }
            return filePath;
        }

        private string ExtractValue(string json, string key)
        {
            string pattern = string.Format("\"{0}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", key);
            Match m = Regex.Match(json, pattern);
            if (m.Success)
                return m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
            return null;
        }
        /// <summary>
        /// 取得文字的 embedding 向量（用於 TurboVec）
        /// </summary>
        public float[] GetEmbedding(string text, string model = "text-embedding-ada-002")
        {
            try
            {
                string url = _baseUrl + "/v1/embeddings";
                string jsonBody = string.Format(
                    "{{\"input\":\"{0}\",\"model\":\"{1}\"}}",
                    EscapeJson(text), model);

                string response = PostJson(url, jsonBody);
                if (response == null) return null;

                // 解析 embedding 陣列
                int dataStart = response.IndexOf("\"embedding\"");
                if (dataStart < 0) return null;
                int arrStart = response.IndexOf("[", dataStart);
                int arrEnd = response.IndexOf("]", arrStart);
                if (arrStart < 0 || arrEnd < 0) return null;

                string arrStr = response.Substring(arrStart + 1, arrEnd - arrStart - 1);
                string[] parts = arrStr.Split(',');
                float[] result = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    float val;
                    if (float.TryParse(parts[i].Trim(), out val))
                        result[i] = val;
                }
                return result;
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
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
