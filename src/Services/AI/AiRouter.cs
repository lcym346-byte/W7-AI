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
                _localLlm.SetTimeout(10000);
            }
            else
            {
                _localLlm = null;
            }

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
                case AiSourceOption.LocalLlmOnly:
                    result = TryLocalLlm(prompt, systemInstruction);
                    break;

                case AiSourceOption.Auto:
                default:
                    // === 智能路由：判斷任務類型決定用本地或線上 ===
                    if (NeedsOnlineAi(prompt))
                    {
                        // 需要即時資料或複雜任務 → 線上優先
                        result = TryGemini(prompt, systemInstruction);
                        if (result == null)
                            result = TryDuckDuckGo(prompt);
                        if (result == null)
                            result = TryFreeProviders(prompt, systemInstruction);
                        // 線上全失敗 → 退回本地
                        if (result == null)
                            result = TryLocalLlm(prompt, systemInstruction);
                    }
                    else
                    {
                        // 一般任務 → 本地優先（節省流量）
                        result = TryLocalLlm(prompt, systemInstruction);
                        // 本地失敗或品質差 → 自動升級到線上
                        if (result == null || IsLowQualityResponse(result))
                        {
                            string onlineResult = TryGemini(prompt, systemInstruction);
                            if (onlineResult == null)
                                onlineResult = TryDuckDuckGo(prompt);
                            if (onlineResult == null)
                                onlineResult = TryFreeProviders(prompt, systemInstruction);
                            if (onlineResult != null)
                                result = onlineResult;
                        }
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

        /// <summary>
        /// 判斷是否需要線上 AI（即時資料、搜尋、複雜任務）
        /// </summary>
        private bool NeedsOnlineAi(string prompt)
        {
            if (string.IsNullOrEmpty(prompt)) return false;
            string lower = prompt.ToLower();

            string[] onlineKeywords = new string[] {
                "\u4eca\u5929", "\u73fe\u5728", "\u6700\u65b0", "\u65b0\u805e",
                "\u5929\u6c23", "\u80a1\u50f9", "\u80a1\u7968", "\u532f\u7387",
                "\u5373\u6642", "\u76ee\u524d", "\u641c\u5c0b", "\u67e5\u8a62",
                "\u4e0a\u7db2", "today", "current", "latest", "news",
                "weather", "search", "google", "find online",
                "\u7db2\u8def", "\u7db2\u9801", "\u7db2\u7ad9", "url", "http"
            };

            for (int i = 0; i < onlineKeywords.Length; i++)
            {
                if (lower.Contains(onlineKeywords[i]))
                    return true;
            }

            // 超長提示可能需要更強模型
            if (prompt.Length > 500)
                return true;

            return false;
        }

        /// <summary>
        /// 判斷回答品質是否太差
        /// </summary>
        private bool IsLowQualityResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return true;

            // 回答太短
            if (response.Length < 10)
            {
                string lower = response.ToLower().Trim();
                if (lower == "ok" || lower == "\u597d" || lower == "\u662f" || lower == "no")
                    return false;
                return true;
            }

            // 明顯的無法回答
            if (response.Contains("\u6211\u7121\u6cd5") ||
                response.Contains("\u6211\u4e0d\u80fd") ||
                response.Contains("\u6211\u6c92\u6709\u8fa6\u6cd5"))
                return true;

            // 全是重複字元
            if (response.Length > 20)
            {
                char first = response[0];
                bool allSame = true;
                for (int i = 1; i < response.Length && i < 50; i++)
                {
                    if (response[i] != first) { allSame = false; break; }
                }
                if (allSame) return true;
            }

            return false;
        }

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
            catch { }

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
                "AI \u72c0\u614b:\n" +
                "  \u672c\u5730 LLM: {0}\n" +
                "  Gemini: {1} (\u5931\u6557 {2} \u6b21)\n" +
                "  DuckDuckGo: {3} (\u5931\u6557 {4} \u6b21)\n" +
                "  \u514d\u8cbb Providers: {5} \u500b\n" +
                "  \u504f\u597d\u4f86\u6e90: {6}\n" +
                "  \u6700\u5f8c\u4f7f\u7528: {7}",
                _localLlm != null ? "\u5df2\u555f\u7528 (\u5931\u6557 " + _localLlmFailCount + " \u6b21)" : "\u672a\u555f\u7528",
                _gemini.IsAvailable ? "\u5df2\u8a2d\u5b9a" : "\u672a\u8a2d\u5b9a Key",
                _geminiFailCount,
                _ddgAvailable ? "\u53ef\u7528" : "\u4e0d\u53ef\u7528",
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

            sb.Append("\u6e2c\u8a66\u672c\u5730 LLM (KoboldCpp)... ");
            if (_localLlm != null && _useLocalLlm)
            {
                try
                {
                    bool ok = _localLlm.TestConnection();
                    sb.AppendLine(ok ? "OK" : "FAILED (\u8acb\u78ba\u8a8d KoboldCpp \u662f\u5426\u5df2\u555f\u52d5)");
                }
                catch
                {
                    sb.AppendLine("FAILED (\u7121\u6cd5\u9023\u7dda)");
                }
            }
            else
            {
                sb.AppendLine("\u672a\u555f\u7528");
            }

            sb.Append("\u6e2c\u8a66 Gemini API... ");
            if (_gemini.IsAvailable)
            {
                bool ok = _gemini.TestConnection();
                sb.AppendLine(ok ? "OK" : "FAILED");
            }
            else
            {
                sb.AppendLine("\u672a\u8a2d\u5b9a API Key");
            }

            sb.Append("\u6e2c\u8a66 DuckDuckGo AI... ");
            bool ddgOk = _ddgAi.TestConnection();
            sb.AppendLine(ddgOk ? "OK" : "FAILED");

            foreach (var provider in _freeProviders)
            {
                sb.Append("\u6e2c\u8a66 " + provider.ProviderName + "... ");
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
