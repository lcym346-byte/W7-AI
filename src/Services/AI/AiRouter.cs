using System;
using System.Text;
using AIAgentTool.Models;

namespace AIAgentTool.Services.AI
{
    public class AiRouter
    {
        private readonly GeminiApiService _gemini;
        private readonly DuckDuckGoAiService _ddgAi;
        private readonly OpenAiCompatibleService[] _freeProviders;
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

            // 初始化免費 OpenAI 相容 providers
            _freeProviders = new OpenAiCompatibleService[]
            {
                // LLM7 - 完全不需要 API Key
                new OpenAiCompatibleService("LLM7",
                    "https://api.llm7.io/v1", "", "deepseek-v3-0324"),

                // Groq - 需要免費 Key (https://console.groq.com/keys)
                new OpenAiCompatibleService("Groq",
                    "https://api.groq.com/openai/v1",
                    settings.GroqApiKey, "llama-3.3-70b-versatile"),

                // Mistral - 需要免費 Key (https://console.mistral.ai/api-keys)
                new OpenAiCompatibleService("Mistral",
                    "https://api.mistral.ai/v1",
                    settings.MistralApiKey, "mistral-small-latest"),

                // OpenRouter - 需要免費 Key (https://openrouter.ai/keys)
                new OpenAiCompatibleService("OpenRouter",
                    "https://openrouter.ai/api/v1",
                    settings.OpenRouterApiKey, "meta-llama/llama-3.3-70b-instruct:free"),
              
                // Agnes AI - 需要免費 Key (https://agnes-ai.com)
                new OpenAiCompatibleService("Agnes",
                    "https://apihub.agnes-ai.com/v1",
                    settings.AgnesApiKey, "claw-3-mini"),

            };
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
                    // Gemini 優先
                    result = TryGemini(prompt, systemInstruction);
                    // DuckDuckGo 其次
                    if (result == null)
                        result = TryDuckDuckGo(prompt);
                    // 免費 providers 最後
                    if (result == null)
                        result = TryFreeProviders(prompt, systemInstruction);
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

        private string TryFreeProviders(string prompt, string systemInstruction)
        {
            foreach (var provider in _freeProviders)
            {
                // 跳過沒有 Key 的（LLM7 除外，它不需要 Key）
                if (provider.ProviderName != "LLM7" &&
                    !provider.IsAvailable)
                    continue;

                try
                {
                    string result = provider.SendMessage(prompt, systemInstruction);
                    if (!string.IsNullOrEmpty(result))
                    {
                        LastUsedSource = provider.ProviderName;
                        return result;
                    }
                }
                catch
                {
                    continue;
                }
            }
            return null;
        }

        public string GetStatusSummary()
        {
            return string.Format(
                "AI 狀態:\n" +
                "  Gemini: {0} (失敗 {1} 次)\n" +
                "  DuckDuckGo: {2} (失敗 {3} 次)\n" +
                "  免費 Providers: {4} 個\n" +
                "  偏好來源: {5}\n" +
                "  最後使用: {6}",
                _gemini.IsAvailable ? "已設定" : "未設定 Key",
                _geminiFailCount,
                _ddgAvailable ? "可用" : "不可用",
                _ddgFailCount,
                _freeProviders.Length,
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
                sb.AppendLine(ok ? "OK" : "FAILED");
            }
            else
            {
                sb.AppendLine("未設定 API Key");
            }

            sb.Append("測試 DuckDuckGo AI... ");
            bool ddgOk = _ddgAi.TestConnection();
            sb.AppendLine(ddgOk ? "OK" : "FAILED");

            foreach (var provider in _freeProviders)
            {
                sb.Append("測試 " + provider.ProviderName + "... ");
                try
                {
                    bool ok = provider.TestConnection();
                    sb.AppendLine(ok ? "OK" : "FAILED");
                }
                catch
                {
                    sb.AppendLine("FAILED");
                }
            }

            return sb.ToString();
        }
    }
}
