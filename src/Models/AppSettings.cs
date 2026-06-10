using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AIAgentTool.Models
{
    public enum AiSourceOption
    {
        Auto,
        GeminiOnly,
        DuckDuckGoOnly,
        Offline,
        LLM7Only,
        GroqOnly,
        MistralOnly,
        OpenRouterOnly,
        AgnesOnly
    }

    public enum SafetyLevel
    {
        Strict,
        Medium,
        Relaxed
    }

    public class AppSettings
    {
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
        public string LocalAiUrl { get; set; }
        public string TurboVecUrl { get; set; }
        public string VideoApiUrl { get; set; }

        private static readonly string SettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public AppSettings()
        {
            GeminiApiKey = "";
            GroqApiKey = "";
            MistralApiKey = "";
            OpenRouterApiKey = "";
            AgnesApiKey = "";
            AiSource = AiSourceOption.Auto;
            Safety = SafetyLevel.Medium;
            DefaultSavePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            MinimizeToTray = true;
            ShowBalloonNotify = true;
            LocalAiUrl = "http://localhost:8080";
            TurboVecUrl = "http://localhost:5050";
            VideoApiUrl = "http://localhost:8501";
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"GeminiApiKey\": \"" + EscapeJsonString(GeminiApiKey) + "\",");
                sb.AppendLine("  \"GroqApiKey\": \"" + EscapeJsonString(GroqApiKey) + "\",");
                sb.AppendLine("  \"MistralApiKey\": \"" + EscapeJsonString(MistralApiKey) + "\",");
                sb.AppendLine("  \"OpenRouterApiKey\": \"" + EscapeJsonString(OpenRouterApiKey) + "\",");
                sb.AppendLine("  \"AgnesApiKey\": \"" + EscapeJsonString(AgnesApiKey) + "\",");
                sb.AppendLine("  \"AiSource\": \"" + AiSource.ToString() + "\",");
                sb.AppendLine("  \"Safety\": \"" + Safety.ToString() + "\",");
                sb.AppendLine("  \"DefaultSavePath\": \"" + EscapeJsonString(DefaultSavePath) + "\",");
                sb.AppendLine("  \"MinimizeToTray\": " + MinimizeToTray.ToString().ToLower() + ",");
                sb.AppendLine("  \"ShowBalloonNotify\": " + ShowBalloonNotify.ToString().ToLower() + ",");
                sb.AppendLine("  \"LocalAiUrl\": \"" + EscapeJsonString(LocalAiUrl) + "\",");
                sb.AppendLine("  \"TurboVecUrl\": \"" + EscapeJsonString(TurboVecUrl) + "\",");
                sb.AppendLine("  \"VideoApiUrl\": \"" + EscapeJsonString(VideoApiUrl) + "\"");
                sb.AppendLine("}");

                File.WriteAllText(SettingsFilePath, sb.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("儲存設定失敗: " + ex.Message);
            }
        }

        public static AppSettings Load()
        {
            var settings = new AppSettings();

            try
            {
                if (!File.Exists(SettingsFilePath))
                    return settings;

                string json = File.ReadAllText(SettingsFilePath);

                settings.GeminiApiKey = ExtractJsonStringValue(json, "GeminiApiKey");
                settings.GroqApiKey = ExtractJsonStringValue(json, "GroqApiKey");
                settings.MistralApiKey = ExtractJsonStringValue(json, "MistralApiKey");
                settings.OpenRouterApiKey = ExtractJsonStringValue(json, "OpenRouterApiKey");
                settings.AgnesApiKey = ExtractJsonStringValue(json, "AgnesApiKey");
                settings.DefaultSavePath = ExtractJsonStringValue(json, "DefaultSavePath");

                settings.LocalAiUrl = ExtractJsonStringValue(json, "LocalAiUrl");
                if (string.IsNullOrEmpty(settings.LocalAiUrl))
                    settings.LocalAiUrl = "http://localhost:8080";

                settings.TurboVecUrl = ExtractJsonStringValue(json, "TurboVecUrl");
                if (string.IsNullOrEmpty(settings.TurboVecUrl))
                    settings.TurboVecUrl = "http://localhost:5050";

                settings.VideoApiUrl = ExtractJsonStringValue(json, "VideoApiUrl");
                if (string.IsNullOrEmpty(settings.VideoApiUrl))
                    settings.VideoApiUrl = "http://localhost:8501";

                string aiSource = ExtractJsonStringValue(json, "AiSource");
                if (!string.IsNullOrEmpty(aiSource))
                {
                    try { settings.AiSource = (AiSourceOption)Enum.Parse(typeof(AiSourceOption), aiSource, true); }
                    catch { }
                }

                string safety = ExtractJsonStringValue(json, "Safety");
                if (!string.IsNullOrEmpty(safety))
                {
                    try { settings.Safety = (SafetyLevel)Enum.Parse(typeof(SafetyLevel), safety, true); }
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

        private static string ExtractJsonStringValue(string json, string key)
        {
            string pattern = string.Format("\"{0}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", Regex.Escape(key));
            var match = Regex.Match(json, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\")
                    .Replace("\\/", "/");
            }

            pattern = string.Format("\"{0}\"\\s*:\\s*([^,\\}}\\]]+)", Regex.Escape(key));
            match = Regex.Match(json, pattern);
            if (match.Success)
                return match.Groups[1].Value.Trim();

            return "";
        }

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
