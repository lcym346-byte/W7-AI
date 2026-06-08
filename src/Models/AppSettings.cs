using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AIAgentTool.Models
{
    /// <summary>
    /// AI 來源選項
    /// </summary>
        public enum AiSourceOption
    {
        Auto,           // 自動選擇 (Gemini → DDG → Free → 離線)
        GeminiOnly,     // 僅 Gemini
        DuckDuckGoOnly, // 僅 DuckDuckGo AI
        Offline,        // 僅離線模板
        LLM7Only,       // 僅 LLM7
        GroqOnly,       // 僅 Groq
        MistralOnly,    // 僅 Mistral
        OpenRouterOnly, // 僅 OpenRouter
        AgnesOnly       // 僅 Agnes
    }

    /// <summary>
    /// 安全等級
    /// </summary>
    public enum SafetyLevel
    {
        Strict,   // 嚴格：僅白名單 CMD，所有操作確認
        Medium,   // 中等：白名單 + 安全管道，程式碼確認
        Relaxed   // 寬鬆：除黑名單外都允許
    }

    /// <summary>
    /// 應用程式設定 — 讀寫 JSON 設定檔
    /// </summary>
    public class AppSettings
    {
        // 設定值
        public string GeminiApiKey { get; set; }
        public string GroqApiKey { get; set; }
        public string MistralApiKey { get; set; }
        public string OpenRouterApiKey { get; set; }
        public string AgnesApiKey { get; set; }
        public AiSourceOption AiSource { get; set; }
        public SafetyLevel Safety { get; set; }
        public string DefaultSavePath { get; set; }
        public bool MinimizeToTray { get; set; }
        public bool ShowBalloonNotify { get; set; }

        // 設定檔路徑
        private static readonly string SettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public AppSettings()
        {
            GeminiApiKey = "";
            GroqApiKey = "";
            MistralApiKey = "";
            OpenRouterApiKey = "";
            AiSource = AiSourceOption.Auto;
            Safety = SafetyLevel.Medium;
            DefaultSavePath = Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop);
            MinimizeToTray = true;
            ShowBalloonNotify = true;
        }

        /// <summary>
        /// 儲存設定到 JSON 檔
        /// </summary>
        public void Save()
        {
            try
            {
                string json = string.Format(
                    "{{\n" +
                    "  \"GeminiApiKey\": \"{0}\",\n" +
                    "  \"GroqApiKey\": \"{1}\",\n" +
                    "  \"MistralApiKey\": \"{2}\",\n" +
                    "  \"OpenRouterApiKey\": \"{3}\",\n" +
                    "  \"AiSource\": \"{4}\",\n" +
                    "  \"Safety\": \"{5}\",\n" +
                    "  \"DefaultSavePath\": \"{6}\",\n" +
                    "  \"MinimizeToTray\": {7},\n" +
                    "  \"ShowBalloonNotify\": {8}\n" +
                    "}}",
                    EscapeJsonString(GeminiApiKey),
                    EscapeJsonString(GroqApiKey),
                    EscapeJsonString(MistralApiKey),
                    EscapeJsonString(OpenRouterApiKey),
                    AiSource.ToString(),
                    Safety.ToString(),
                    EscapeJsonString(DefaultSavePath),
                    MinimizeToTray.ToString().ToLower(),
                    ShowBalloonNotify.ToString().ToLower());

                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("儲存設定失敗: " + ex.Message);
            }
        }

        /// <summary>
        /// 從 JSON 檔載入設定
        /// </summary>
        public static AppSettings Load()
        {
            var settings = new AppSettings();

            try
            {
                if (!File.Exists(SettingsFilePath))
                    return settings;

                string json = File.ReadAllText(SettingsFilePath);

                // 手動解析 JSON (不依賴外部套件)
                settings.GeminiApiKey = ExtractJsonStringValue(json, "GeminiApiKey");
                settings.GroqApiKey = ExtractJsonStringValue(json, "GroqApiKey");
                settings.MistralApiKey = ExtractJsonStringValue(json, "MistralApiKey");
                settings.OpenRouterApiKey = ExtractJsonStringValue(json, "OpenRouterApiKey");
                settings.DefaultSavePath = ExtractJsonStringValue(json, "DefaultSavePath");
                settings.AgnesApiKey = ExtractJsonStringValue(json, "AgnesApiKey");

                string aiSource = ExtractJsonStringValue(json, "AiSource");
                if (!string.IsNullOrEmpty(aiSource))
                {
                    try { settings.AiSource = (AiSourceOption)Enum.Parse(
                        typeof(AiSourceOption), aiSource, true); }
                    catch { }
                }

                string safety = ExtractJsonStringValue(json, "Safety");
                if (!string.IsNullOrEmpty(safety))
                {
                    try { settings.Safety = (SafetyLevel)Enum.Parse(
                        typeof(SafetyLevel), safety, true); }
                    catch { }
                }

                string tray = ExtractJsonStringValue(json, "MinimizeToTray");
                if (tray == "true") settings.MinimizeToTray = true;
                else if (tray == "false") settings.MinimizeToTray = false;

                string balloon = ExtractJsonStringValue(json, "ShowBalloonNotify");
                if (balloon == "true") settings.ShowBalloonNotify = true;
                else if (balloon == "false") settings.ShowBalloonNotify = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("載入設定失敗: " + ex.Message);
            }

            return settings;
        }

        /// <summary>
        /// 簡易 JSON 字串值擷取
        /// </summary>
        private static string ExtractJsonStringValue(string json, string key)
        {
            // 匹配 "key": "value" 或 "key": value
            string pattern = string.Format(
                "\"{0}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", Regex.Escape(key));
            var match = Regex.Match(json, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\")
                    .Replace("\\/", "/");
            }

            // 嘗試非字串值 (bool/number)
            pattern = string.Format(
                "\"{0}\"\\s*:\\s*([^,\\}}\\]]+)", Regex.Escape(key));
            match = Regex.Match(json, pattern);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return "";
        }

        /// <summary>
        /// JSON 字串跳脫
        /// </summary>
        private static string EscapeJsonString(string s)
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
