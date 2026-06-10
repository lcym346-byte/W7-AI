using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace AIAgentTool.Services.Media
{
    /// <summary>
    /// 影片生成服務 - 透過 MoneyPrinterTurbo API 生成短影片
    /// 也支援 LocalAI 的影片端點（如果有安裝 LTX 模型）
    /// </summary>
    public class VideoGeneratorService
    {
        private readonly string _videoApiUrl;
        private readonly string _localAiUrl;
        private readonly int _timeoutMs;

        public string LastError { get; private set; }

        public VideoGeneratorService(string videoApiUrl, string localAiUrl)
        {
            _videoApiUrl = (videoApiUrl ?? "http://localhost:8501").TrimEnd('/');
            _localAiUrl = (localAiUrl ?? "http://localhost:8080").TrimEnd('/');
            _timeoutMs = 300000; // 影片生成需要很長時間
        }

        /// <summary>
        /// 用 MoneyPrinterTurbo 生成短影片
        /// </summary>
        /// <param name="topic">影片主題</param>
        /// <param name="savePath">儲存資料夾</param>
        /// <param name="language">語言 zh-TW / en</param>
        /// <returns>影片檔案路徑，失敗回傳 null</returns>
        public string GenerateShortVideo(string topic, string savePath, string language = "zh-TW")
        {
            try
            {
                string url = _videoApiUrl + "/api/v1/videos";

                string jsonBody = string.Format(
                    "{{" +
                    "\"video_subject\":\"{0}\"," +
                    "\"video_language\":\"{1}\"," +
                    "\"voice_name\":\"zh-TW-HsiaoChenNeural\"," +
                    "\"paragraph_number\":3" +
                    "}}",
                    EscapeJson(topic), language);

                string response = PostJson(url, jsonBody, _timeoutMs);
                if (response == null) return null;

                // 解析回傳的影片路徑
                string videoPath = ExtractValue(response, "video_path");
                if (string.IsNullOrEmpty(videoPath))
                    videoPath = ExtractValue(response, "output");

                if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
                    return videoPath;

                // 嘗試下載
                string videoUrl = ExtractValue(response, "video_url");
                if (!string.IsNullOrEmpty(videoUrl))
                {
                    if (!Directory.Exists(savePath))
                        Directory.CreateDirectory(savePath);

                    string fileName = string.Format("video_{0}.mp4",
                        DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                    string filePath = Path.Combine(savePath, fileName);

                    using (WebClient wc = new WebClient())
                    {
                        wc.DownloadFile(videoUrl, filePath);
                    }
                    return filePath;
                }

                // 回傳任務 ID（非同步生成）
                string taskId = ExtractValue(response, "task_id");
                if (!string.IsNullOrEmpty(taskId))
                    return "TASK_ID:" + taskId;

                LastError = "No video path in response";
                return null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 檢查影片生成任務狀態
        /// </summary>
        public string CheckTaskStatus(string taskId)
        {
            try
            {
                string url = _videoApiUrl + "/api/v1/videos/" + taskId;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = 10000;

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

        /// <summary>
        /// 測試 MoneyPrinterTurbo 連線
        /// </summary>
        public bool TestVideoApi()
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_videoApiUrl);
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

        private string PostJson(string url, string jsonBody, int timeout)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Timeout = timeout;

                byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                req.ContentLength = bodyBytes.Length;

                using (Stream s = req.GetRequestStream())
                {
                    s.Write(bodyBytes, 0, bodyBytes.Length);
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
                    using (StreamReader r = new StreamReader(wex.Response.GetResponseStream()))
                        LastError = r.ReadToEnd();
                }
                else
                    LastError = wex.Message;
                return null;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        private string ExtractValue(string json, string key)
        {
            string pattern = string.Format("\"{0}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", key);
            Match m = Regex.Match(json, pattern);
            return m.Success ? m.Groups[1].Value : null;
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
