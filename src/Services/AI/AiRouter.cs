using System;
using System.Text;
using AIAgentTool.Models;

namespace AIAgentTool.Services.AI
{
    public class AiRouter
    {
        private readonly GeminiApiService _gemini;
        private readonly DuckDuckGoAiService _ddgAi;
        private readonly AiSourceOption _preferredSource;

        private bool _geminiAvailable;
        private bool _ddgAvailable;
        private DateTime _lastGeminiFailTime;
        private DateTime _lastDdgFailTime;
        private int _geminiFailCount;
        private int _ddgFailCount;

        private const int MAX_FAIL_BEFORE_SKIP = 3;
        private static readonly TimeSpan RETRY_WAIT = TimeSpan.FromMinutes(5);

        public string LastUsedSource { get; private set; }

        public AiRouter(AppSettings settings)
        {
            _preferredSource = settings.AiSource;
            _gemini = new GeminiApiService(settings.GeminiApiKey);
            _ddgAi = new DuckDuckGoAiService();
            _geminiAvailable = _gemini.IsAvailable;
            _ddgAvailable = true;
            _lastGeminiFailTime = DateTime.MinValue;
            _lastDdgFailTime = DateTime.MinValue;
            _geminiFailCount = 0;
            _ddgFailCount = 0;
            LastUsedSource = "None";
        }

        public string SendMessage(string prompt, string systemInstruction)
        {
            string result = null;

            switch (_preferredSource)
            {
                case AiSourceOption.GeminiOnly:
                    result = TryGemini(prompt, systemInstruction);
                    break;
                case AiSourceOption.DuckDuckGoOnly:
                    result = TryDuckDuckGo(prompt);
                    break;
                case AiSourceOption.Offline:
                    LastUsedSource = "Offline";
                    return null;
                case AiSourceOption.Auto:
                default:
                    result = TryGemini(prompt, systemInstruction);
                    if (result == null)
                    {
                        result = TryDuckDuckGo(prompt);
                    }
                    break;
            }

            if (result == null)
                LastUsedSource = "None (All Failed)";

            return result;
        }

        public string SendMessage(string prompt)
        {
            return SendMessage(prompt, null);
        }

        public string Ask(string prompt)
        {
            return SendMessage(prompt, null);
        }

        private string TryGemini(string prompt, string systemInstruction)
        {
            if (!_gemini.IsAvailable)
                return null;

            if (_geminiFailCount >= MAX_FAIL_BEFORE_SKIP)
            {
                if (DateTime.Now - _lastGeminiFailTime < RETRY_WAIT)
                    return null;
                _geminiFailCount = 0;
            }

            string result = _gemini.SendMessage(prompt, systemInstruction);

            if (result != null)
            {
                _geminiFailCount = 0;
                _geminiAvailable = true;
                LastUsedSource = "Gemini";
                return result;
            }

            _geminiFailCount++;
            _lastGeminiFailTime = DateTime.Now;
            return null;
        }

        private string TryDuckDuckGo(string prompt)
        {
            if (_ddgFailCount >= MAX_FAIL_BEFORE_SKIP)
            {
                if (DateTime.Now - _lastDdgFailTime < RETRY_WAIT)
                    return null;
                _ddgFailCount = 0;
            }

            string result = _ddgAi.SendMessage(prompt);

            if (result != null)
            {
                _ddgFailCount = 0;
                _ddgAvailable = true;
                LastUsedSource = "DuckDuckGo AI";
                return result;
            }

            _ddgFailCount++;
            _lastDdgFailTime = DateTime.Now;
            return null;
        }

        public string GetStatusSummary()
        {
            return string.Format(
                "AI 狀態:\n" +
                "  Gemini: {0} (失敗 {1} 次)\n" +
                "  DuckDuckGo: {2} (失敗 {3} 次)\n" +
                "  偏好來源: {4}\n" +
                "  最後使用: {5}",
                _gemini.IsAvailable ? "已設定" : "未設定 Key",
                _geminiFailCount,
                _ddgAvailable ? "可用" : "不可用",
                _ddgFailCount,
                _preferredSource,
                LastUsedSource);
        }

        public void ResetFailCounts()
        {
            _geminiFailCount = 0;
            _ddgFailCount = 0;
        }

        public string TestAllConnections()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("測試 Gemini API... ");
            if (_gemini.IsAvailable)
            {
                bool ok = _gemini.TestConnection();
                sb.AppendLine(ok ? "✓ 成功" : "✗ 失敗");
            }
            else
            {
                sb.AppendLine("⚠ 未設定 API Key");
            }

            sb.Append("測試 DuckDuckGo AI... ");
            bool ddgOk = _ddgAi.TestConnection();
            sb.AppendLine(ddgOk ? "✓ 成功" : "✗ 失敗");

            return sb.ToString();
        }
    }
}
