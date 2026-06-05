using System;
using AIAgentTool.Models;
using System.Text;

namespace AIAgentTool.Services.AI
{
    /// <summary>
    /// AI 路由器 — 自動選擇最佳 AI 來源，含三層 Fallback
    /// 
    /// 優先順序:
    /// 1. Google Gemini API (品質最佳，需 API Key)
    /// 2. DuckDuckGo AI Chat (免費，不需 Key)
    /// 3. 離線模式 (回傳 null，由上層決定用本地模板)
    /// </summary>
    public class AiRouter
    {
        private readonly GeminiApiService _gemini;
        private readonly DuckDuckGoAiService _ddgAi;
        private readonly AiSourceOption _preferredSource;

        // 追蹤各來源狀態
        private bool _geminiAvailable;
        private bool _ddgAvailable;
        private DateTime _lastGeminiFailTime;
        private DateTime _lastDdgFailTime;
        private int _geminiFailCount;
        private int _ddgFailCount;

        // 連續失敗超過此次數，暫時跳過該來源
        private const int MAX_FAIL_BEFORE_SKIP = 3;
        // 跳過後等待此時間再重試
        private static readonly TimeSpan RETRY_WAIT = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 最後實際使用的 AI 來源名稱
        /// </summary>
        public string LastUsedSource { get; private set; }

        public AiRouter(AppSettings settings)
        {
            _preferredSource = settings.AiSource;

            // 初始化各 AI 服務
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

        /// <summary>
        /// 發送訊息到 AI（自動選擇來源 + Fallback）
        /// </summary>
        /// <param name="prompt">提示詞</param>
        /// <param name="systemInstruction">系統指令（可選，僅 Gemini 支援）</param>
        /// <returns>AI 回應文字，全部失敗回傳 null</returns>
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
                    // 離線模式不呼叫任何 AI
                    LastUsedSource = "Offline";
                    return null;

                case AiSourceOption.Auto:
                default:
                    // 自動模式：Gemini → DuckDuckGo → null
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

        /// <summary>
        /// 發送訊息（不含系統指令）
        /// </summary>
        public string SendMessage(string prompt)
        {
            return SendMessage(prompt, null);
        }

        /// <summary>
        /// Ask — SendMessage 的別名，供 TaskAutomationService 呼叫
        /// </summary>
        public string Ask(string prompt)
        {
            return SendMessage(prompt, null);
        }


        /// <summary>
        /// 嘗試使用 Gemini API
        /// </summary>
        private string TryGemini(string prompt, string systemInstruction)
        {
            if (!_gemini.IsAvailable)
                return null;

            // 檢查是否因連續失敗而暫時跳過
            if (_geminiFailCount >= MAX_FAIL_BEFORE_SKIP)
            {
                if (DateTime.Now - _lastGeminiFailTime < RETRY_WAIT)
                    return null;
                // 等待時間已過，重置失敗計數
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

            // 失敗
            _geminiFailCount++;
            _lastGeminiFailTime = DateTime.Now;
            return null;
        }

        /// <summary>
        /// 嘗試使用 DuckDuckGo AI Chat
        /// </summary>
        private string TryDuckDuckGo(string prompt)
        {
            // 檢查是否因連續失敗而暫時跳過
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

            // 失敗
            _ddgFailCount++;
            _lastDdgFailTime = DateTime.Now;
            return null;
        }

        /// <summary>
        /// 取得目前 AI 連線狀態摘要
        /// </summary>
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

        /// <summary>
        /// 重置所有失敗計數
        /// </summary>
        public void ResetFailCounts()
        {
            _geminiFailCount = 0;
            _ddgFailCount = 0;
        }

        /// <summary>
        /// 測試所有 AI 來源連線
        /// </summary>
        public string TestAllConnections()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();


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
 
