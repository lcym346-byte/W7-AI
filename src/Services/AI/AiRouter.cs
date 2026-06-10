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

        // === 新增：本地 LLM ===
        private readonly OpenAiCompatibleService _localLlm;
        private readonly bool _useLocalLlm;
        private int _localLlmFailCount;
        private DateTime _lastLocalLlmFailTime;

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

            // === 新增：初始化本地 LLM ===
            _useLocalLlm = settings.UseLocalLlm;
            _localLlmFailCount = 0;
            _lastLocalLlmFailTime = DateTime.MinValue;

            if (_useLocalLlm && !string.IsNullOrEmpty(settings.LocalLlmUrl))
            {
                _localLlm = new OpenAiCompatibleService(
                    "LocalLLM",
                    settings.LocalLlmUrl + "/v1",
                    "",
                    settings.LocalLlmModel);
            }
            else
            {
                _localLlm = null;
            }

            // 初始化免費 OpenAI 相容 providers
            _freeProviders = new OpenAiCompatibleService[]
            {
                new OpenAiCompatibleService("LLM7",
                    "https://api.llm7.io/v1", "", "deepseek-v3-0324"),

                new OpenAiCompatibleService("Groq",
                    "https://api.groq.com/openai/v1",
                    settings.GroqApiKey, "llama-3.3-70b-versatile"),

                new OpenAiCompatibleService("Mistral",
                    "https://api.mistral.ai/v1",
                    settings.MistralApiKey, "mistral-small-latest"),

                new OpenAiCompatibleService("OpenRouter",
                    "https://openrouter.ai/api/v1",
                    settings.OpenRouterApiKey, "meta-llama/llama-3.3-70b-instruct:free"),

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
                case AiSourceOption.LLM7Only:
                    result = TrySpecificFreeProvider("LLM7", prompt, systemInstruction);
                    break;
                case AiSourceOption.GroqOnly:
                    result = TrySpecificFreeProvider("Groq", prompt, systemInstruction);
                    break;
                case AiSourceOption.MistralOnly:
                    result = TrySpecificFreeProvider("Mistral", prompt, systemInstruction);
                    break;
                case AiSourceOption.OpenRouterOnly:
                    result = TrySpecificFreeProvider("OpenRouter", prompt, systemInstruction);
                    break;
                case AiSourceOption.AgnesOnly:
                    result = TrySpecificFreeProvider("Agnes", prompt, systemInstruction);
                    break;

                // === 新增：本地 LLM 專用模式 ===
                case AiSourceOption.LocalLlmOnly:
                    result = TryLocalLlm(prompt, systemInstruction);
                    break;

                case AiSourceOption.Auto:
                default:
                    // === 修改：本地 LLM 最優先 ===
                    result = TryLocalLlm(prompt, systemInstruction);
                    // Gemini 其次
                    if (result == null)
                        result = TryGemini(prompt, systemInstruction);
                    // DuckDuckGo 第三
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

        // === 新增：嘗試本地 LLM ===
        private string TryLocalLlm(string prompt, string systemInstruction)
        {
            if (_localLlm == null || !_useLocalLlm)
                return null;

            if (_localLlmFailCount >= MAX_FAIL_BEFORE_SKIP)
            {
                if (DateTime.Now - _lastLocalLlmFailTime < RETRY_WAIT)
                    return null;
                _localLlmFailCount = 0;
            }

            try
            {
                string result = _localLlm.SendMessage(prompt, systemInstruction);
                if (!string.IsNullOrEmpty(result))
                {
                    _localLlmFailCount = 0;
                    LastUsedSource = "LocalLLM (KoboldCpp)";
                    return result;
                }
            }
            catch
            {
                // 本地 LLM 未啟動或連線失敗
            }

            _localLlmFailCount++;
            _lastLocalLlmFailTime = DateTime.Now;
            return null;
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
                "  本地 LLM: {0}\n" +
                "  Gemini: {1} (失敗 {2} 次)\n" +
                "  DuckDuckGo: {3} (失敗 {4} 次)\n" +
                "  免費 Providers: {5} 個\n" +
                "  偏好來源: {6}\n" +
                "  最後使用: {7}",
                _localLlm != null ? "已啟用 (失敗 " + _localLlmFailCount + " 次)" : "未啟用",
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
            _localLlmFailCount = 0;
        }

        public string TestAllConnections()
        {
            StringBuilder sb = new StringBuilder();

            // === 新增：測試本地 LLM ===
            sb.Append("測試本地 LLM (KoboldCpp)... ");
            if (_localLlm != null && _useLocalLlm)
            {
                try
                {
                    bool ok = _localLlm.TestConnection();
                    sb.AppendLine(ok ? "OK" : "FAILED (請確認 KoboldCpp 是否已啟動)");
                }
                catch
                {
                    sb.AppendLine("FAILED (無法連線)");
                }
            }
            else
            {
                sb.AppendLine("未啟用");
            }

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

        private string TrySpecificFreeProvider(string name, string prompt, string systemInstruction)
        {
            foreach (var provider in _freeProviders)
            {
                if (provider.ProviderName == name)
                {
                    try
                    {
                        string result = provider.SendMessage(prompt, systemInstruction);
                        if (!string.IsNullOrEmpty(result))
                        {
                            LastUsedSource = provider.ProviderName;
                            return result;
                        }
                    }
                    catch { }
                }
            }
            return null;
        }
    }
}
