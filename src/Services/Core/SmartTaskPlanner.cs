using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AIAgentTool.Services.AI;

namespace AIAgentTool.Services.Core
{
    /// <summary>
    /// 智慧任務規劃器 - 將自然語言指令轉為可執行步驟
    /// </summary>
    public class SmartTaskPlanner
    {
        private readonly AiRouter _ai;

        private const string PLAN_PROMPT =
            "你是一個 Windows 7 電腦操作助手。使用者會給你一個自然語言指令，你需要將它拆解為具體的電腦操作步驟。\n\n" +
            "可用的操作類型（type 欄位）：\n" +
            "- launch_app: 啟動程式（需要 target 欄位指定程式名或路徑）\n" +
            "- close_app: 關閉程式（需要 target 欄位）\n" +
            "- send_keys: 模擬鍵盤輸入（需要 keys 欄位，特殊鍵用 {ENTER},{TAB},{ALT},{CTRL},{WIN},{F1}-{F12},{UP},{DOWN},{LEFT},{RIGHT},{ESC},{DELETE},{BACKSPACE}）\n" +
            "- wait: 等待（需要 ms 欄位，毫秒數）\n" +
            "- cmd: 執行 CMD 命令（需要 command 欄位）\n" +
            "- find_and_launch: 搜尋電腦中的程式並啟動（需要 keyword 欄位）\n" +
            "- click: 滑鼠點擊（需要 x, y 欄位，座標）\n" +
            "- set_clipboard: 設定剪貼簿（需要 text 欄位）\n" +
            "- open_url: 開啟網址（需要 url 欄位）\n" +
            "- open_file: 開啟檔案（需要 path 欄位）\n" +
            "- search_file: 搜尋檔案（需要 keyword 欄位）\n" +
            "- screenshot: 截圖\n" +
            "- message: 顯示訊息給使用者（需要 text 欄位）\n" +
            "- generate_code: 生成程式碼（需要 description 欄位）\n" +
            "- search_web: 搜尋網路（需要 query 欄位）\n" +
            "\n" +
            "回覆格式必須是 JSON 陣列，每個元素是一個步驟物件。範例：\n" +
            "[{\"step\":1,\"type\":\"launch_app\",\"target\":\"calc.exe\",\"desc\":\"開啟計算機\"}," +
            "{\"step\":2,\"type\":\"wait\",\"ms\":1000,\"desc\":\"等待程式啟動\"}," +
            "{\"step\":3,\"type\":\"message\",\"text\":\"計算機已開啟\",\"desc\":\"通知使用者\"}]\n\n" +
            "注意：\n" +
            "- 只回傳 JSON 陣列，不要加任何其他文字\n" +
            "- desc 欄位用繁體中文描述該步驟\n" +
            "- 如果不確定程式在哪裡，使用 find_and_launch\n" +
            "- 每個步驟之間如需等待程式載入，加入 wait 步驟\n" +
            "- 如果指令太模糊無法執行，回傳: [{\"step\":1,\"type\":\"message\",\"text\":\"請更具體描述...\",\"desc\":\"需要更多資訊\"}]\n" +
            "- Windows 7 沒有內建鬧鐘程式，如需鬧鐘功能請用 generate_code 產生一個\n" +
            "- 常見 Windows 7 內建程式：notepad, calc, mspaint, wordpad, cmd, explorer, iexplore, wmplayer, SnippingTool\n";

        public SmartTaskPlanner(AiRouter aiRouter)
        {
            _ai = aiRouter;
        }

        /// <summary>
        /// 將自然語言指令轉為執行計畫
        /// </summary>
                public List<TaskStep> PlanTask(string userInput)
        {
            // 從上下文中提取最新指令（如果有 [CURRENT] 標記）
            string actualInput = userInput;
            int currentTag = userInput.IndexOf("[CURRENT] ");
            if (currentTag >= 0)
                actualInput = userInput.Substring(currentTag + 10).Trim();

            // 先用「實際指令」嘗試本地快速匹配
            List<TaskStep> localPlan = TryLocalPlan(actualInput);
            if (localPlan != null) return localPlan;

            // 使用 AI 規劃（傳完整上下文）
            string response = _ai.SendMessage(userInput, PLAN_PROMPT);
            if (string.IsNullOrEmpty(response))
            {
                return FallbackPlan(actualInput);
            }

            return ParsePlanJson(response);
        }


        /// <summary>
        /// 本地快速匹配常見指令
        /// </summary>
        private List<TaskStep> TryLocalPlan(string input)
        {
            string lower = input.ToLower().Trim();
                        // ★ 程式碼修正匹配（最優先）
            bool wantFix = input.Contains("\u4fee\u6b63") || input.Contains("\u4fee\u5fa9") ||
                input.Contains("\u4fee\u6539") || input.Contains("\u6539\u4e00\u4e0b") ||
                input.Contains("\u554f\u984c") || input.Contains("\u6c92\u7528") ||
                input.Contains("\u4e0d\u80fd") || input.Contains("\u7121\u6cd5") ||
                input.Contains("\u58de\u4e86") || input.Contains("\u932f\u8aa4") ||
                input.Contains("fix") || input.Contains("bug");

            bool aboutCode = input.Contains("\u7a0b\u5f0f") || input.Contains("\u8a08\u6642") ||
                input.Contains("\u5012\u6578") || input.Contains("\u5206\u9418") ||
                input.Contains("\u529f\u80fd") || input.Contains("\u6309\u9215") ||
                input.Contains("code") || input.Contains("timer");

            if (wantFix && aboutCode)
            {
                List<TaskStep> plan = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "generate_code";
                step.Description = input;
                step.Desc = "fix/regenerate code: " + input;
                plan.Add(step);
                return plan;
            }
            // ★ 媒體生成匹配
            string lower = userInput.ToLower();

            // 圖片生成
            if ((lower.Contains("生成圖") || lower.Contains("畫") || lower.Contains("產生圖") ||
                 lower.Contains("generate image") || lower.Contains("draw")) &&
                !lower.Contains("程式") && !lower.Contains("code"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "generate_image";
                step.Desc = userInput;
                step.Text = userInput;
                steps.Add(step);
                return steps;
            }

            // 語音合成
            if (lower.Contains("朗讀") || lower.Contains("唸") || lower.Contains("語音") ||
                lower.Contains("tts") || lower.Contains("text to speech") ||
                lower.Contains("說出"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "text_to_speech";
                step.Desc = userInput;
                step.Text = userInput;
                steps.Add(step);
                return steps;
            }

            // 螢幕截圖
            if (lower.Contains("截圖") || lower.Contains("螢幕") ||
                lower.Contains("screenshot") || lower.Contains("capture screen"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "screenshot";
                step.Desc = userInput;
                steps.Add(step);
                return steps;
            }

            // 視窗操作
            if (lower.Contains("切換到") || lower.Contains("打開視窗") ||
                lower.Contains("focus window") || lower.Contains("switch to"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "focus_window";
                step.Desc = userInput;
                step.Text = userInput;
                steps.Add(step);
                return steps;
            }

            // 列出視窗
            if (lower.Contains("列出視窗") || lower.Contains("有哪些視窗") ||
                lower.Contains("list windows") || lower.Contains("開了什麼"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "list_windows";
                step.Desc = userInput;
                steps.Add(step);
                return steps;
            }

            // 點擊操作
            if (lower.Contains("點擊") || lower.Contains("按一下") ||
                lower.Contains("click at") || lower.Contains("click on"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "click";
                step.Desc = userInput;
                steps.Add(step);
                return steps;
            }


            // ★ 程式碼生成匹配（最優先，用 Contains 避免編碼問題）
            bool wantCode = lower.Contains("write") || lower.Contains("make") ||
                lower.Contains("create") || lower.Contains("build") || lower.Contains("code") ||
                input.Contains("\u5beb") || input.Contains("\u88fd\u4f5c") ||
                input.Contains("\u505a") || input.Contains("\u7522\u751f") ||
                input.Contains("\u751f\u6210") || input.Contains("\u5efa\u7acb") ||
                input.Contains("\u7a0b\u5f0f") || input.Contains("\u8edf\u9ad4") ||
                input.Contains("\u5de5\u5177");

            bool isCodeTarget = lower.Contains("app") || lower.Contains("gui") ||
                lower.Contains("winforms") || lower.Contains("program") ||
                input.Contains("\u7a0b\u5f0f") || input.Contains("\u8edf\u9ad4") ||
                input.Contains("\u5de5\u5177") || input.Contains("\u8a08\u7b97\u6a5f") ||
                input.Contains("\u9b27\u9418") || input.Contains("\u904a\u6232") ||
                input.Contains("\u8996\u7a97") || input.Contains("\u4ecb\u9762") ||
                input.Contains("\u653e\u5728") || input.Contains("\u5b58\u5230") ||
                input.Contains("\u5132\u5b58");

            if (wantCode && isCodeTarget)
            {
                List<TaskStep> plan = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "generate_code";
                step.Description = input;
                step.Desc = "generate code: " + input;
                plan.Add(step);
                return plan;
            }

            // 開啟程式
            if (lower.Contains("open") || lower.Contains("launch") || lower.Contains("start") ||
                input.Contains("\u958b\u555f") || input.Contains("\u6253\u958b") ||
                input.Contains("\u555f\u52d5") || input.Contains("\u57f7\u884c"))
            {
                string target = input;
                string[] removePrefixes = new string[] {
                    "\u958b\u555f", "\u6253\u958b", "\u555f\u52d5", "\u57f7\u884c",
                    "open", "launch", "start"
                };
                foreach (string p in removePrefixes)
                {
                    int idx = target.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0) target = target.Substring(idx + p.Length);
                }
                target = target.Trim();
                if (target.Length > 0)
                {
                    List<TaskStep> plan = new List<TaskStep>();
                    TaskStep step = new TaskStep();
                    step.Step = 1;
                    step.Type = "find_and_launch";
                    step.Keyword = target;
                    step.Desc = "launch " + target;
                    plan.Add(step);
                    return plan;
                }
            }

            // 關閉程式
            if (lower.Contains("close") || lower.Contains("kill") || lower.Contains("stop") ||
                input.Contains("\u95dc\u9589") || input.Contains("\u7d50\u675f") ||
                input.Contains("\u505c\u6b62"))
            {
                string target = input;
                string[] removePrefixes = new string[] {
                    "\u95dc\u9589", "\u7d50\u675f", "\u505c\u6b62",
                    "close", "kill", "stop"
                };
                foreach (string p in removePrefixes)
                {
                    int idx = target.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0) target = target.Substring(idx + p.Length);
                }
                target = target.Trim();
                if (target.Length > 0)
                {
                    List<TaskStep> plan = new List<TaskStep>();
                    TaskStep step = new TaskStep();
                    step.Step = 1;
                    step.Type = "close_app";
                    step.Target = target;
                    step.Desc = "close " + target;
                    plan.Add(step);
                    return plan;
                }
            }

            // 截圖
            if (input.Contains("\u622a\u5716") || lower.Contains("screenshot") ||
                lower.Contains("screen capture"))
            {
                List<TaskStep> plan = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "screenshot";
                step.Desc = "screen capture";
                plan.Add(step);
                return plan;
            }

            // 搜尋網路
            if ((input.Contains("\u641c\u5c0b") || input.Contains("\u641c\u7d22") ||
                 input.Contains("\u627e") || input.Contains("\u67e5") ||
                 lower.Contains("search")) &&
                !input.Contains("\u6a94\u6848") && !lower.Contains("file"))
            {
                string query = input;
                string[] removePrefixes = new string[] {
                    "\u641c\u5c0b", "\u641c\u7d22", "\u627e", "\u67e5", "search"
                };
                foreach (string p in removePrefixes)
                {
                    int idx = query.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0) query = query.Substring(idx + p.Length);
                }
                query = query.Trim();
                if (query.Length > 0)
                {
                    List<TaskStep> plan = new List<TaskStep>();
                    TaskStep step = new TaskStep();
                    step.Step = 1;
                    step.Type = "search_web";
                    step.Query = query;
                    step.Desc = "search: " + query;
                    plan.Add(step);
                    return plan;
                }
            }

            return null; // 無法本地匹配，交給 AI
        }

        /// <summary>
        /// AI 不可用時的備用規劃
        /// </summary>
        private List<TaskStep> FallbackPlan(string input)
        {
            List<TaskStep> plan = new List<TaskStep>();
            TaskStep step = new TaskStep();
            step.Step = 1;
            step.Type = "message";
            step.Text = "AI \u670d\u52d9\u4e0d\u53ef\u7528\uff0c\u7121\u6cd5\u5206\u6790\u60a8\u7684\u6307\u4ee4\u3002\u8acb\u5617\u8a66\u66f4\u7c21\u55ae\u7684\u6307\u4ee4\u5982\u300c\u958b\u555f\u8a18\u4e8b\u672c\u300d\u300c\u622a\u5716\u300d\u7b49\u3002";
            step.Desc = "AI unavailable";
            plan.Add(step);
            return plan;
        }

        /// <summary>
        /// 解析 AI 回傳的 JSON 計畫
        /// </summary>
        private List<TaskStep> ParsePlanJson(string json)
        {
            List<TaskStep> steps = new List<TaskStep>();

            try
            {
                // 清理 JSON（可能包含 markdown code block）
                json = json.Trim();
                if (json.StartsWith("```"))
                {
                    int firstNewline = json.IndexOf('\n');
                    if (firstNewline > 0) json = json.Substring(firstNewline + 1);
                    int lastBacktick = json.LastIndexOf("```");
                    if (lastBacktick > 0) json = json.Substring(0, lastBacktick);
                    json = json.Trim();
                }

                // 找到 JSON 陣列的開始和結束
                int start = json.IndexOf('[');
                int end = json.LastIndexOf(']');
                if (start < 0 || end < 0 || end <= start)
                {
                    TaskStep errStep = new TaskStep();
                    errStep.Step = 1;
                    errStep.Type = "message";
                    errStep.Text = "AI \u56de\u8986\u683c\u5f0f\u932f\u8aa4\uff0c\u7121\u6cd5\u89e3\u6790\u57f7\u884c\u8a08\u756b\u3002";
                    errStep.Desc = "parse failed";
                    steps.Add(errStep);
                    return steps;
                }

                json = json.Substring(start, end - start + 1);

                // 簡易 JSON 陣列解析（逐個物件）
                MatchCollection objMatches = Regex.Matches(json,
                    @"\{[^{}]*\}", RegexOptions.Singleline);

                foreach (Match objMatch in objMatches)
                {
                    TaskStep step = new TaskStep();
                    string obj = objMatch.Value;

                    step.Step = ExtractInt(obj, "step");
                    step.Type = ExtractString(obj, "type") ?? "message";
                    step.Desc = ExtractString(obj, "desc") ?? "";
                    step.Target = ExtractString(obj, "target");
                    step.Keys = ExtractString(obj, "keys");
                    step.Ms = ExtractInt(obj, "ms");
                    step.Command = ExtractString(obj, "command");
                    step.Keyword = ExtractString(obj, "keyword");
                    step.X = ExtractInt(obj, "x");
                    step.Y = ExtractInt(obj, "y");
                    step.Text = ExtractString(obj, "text");
                    step.Url = ExtractString(obj, "url");
                    step.Path = ExtractString(obj, "path");
                    step.Query = ExtractString(obj, "query");
                    step.Description = ExtractString(obj, "description");

                    steps.Add(step);
                }

                if (steps.Count == 0)
                {
                    TaskStep errStep = new TaskStep();
                    errStep.Step = 1;
                    errStep.Type = "message";
                    errStep.Text = "AI \u672a\u7522\u751f\u4efb\u4f55\u57f7\u884c\u6b65\u9a5f\u3002";
                    errStep.Desc = "empty plan";
                    steps.Add(errStep);
                }
            }
            catch (Exception ex)
            {
                TaskStep errStep = new TaskStep();
                errStep.Step = 1;
                errStep.Type = "message";
                errStep.Text = "parse error: " + ex.Message;
                errStep.Desc = "exception";
                steps.Add(errStep);
            }

            return steps;
        }

        private string ExtractString(string json, string key)
        {
            string pattern = string.Format("\"{0}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"",
                Regex.Escape(key));
            Match m = Regex.Match(json, pattern);
            if (m.Success)
            {
                return m.Groups[1].Value
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\")
                    .Replace("\\n", "\n")
                    .Replace("\\t", "\t");
            }
            return null;
        }

        private int ExtractInt(string json, string key)
        {
            string pattern = string.Format("\"{0}\"\\s*:\\s*(\\d+)", Regex.Escape(key));
            Match m = Regex.Match(json, pattern);
            if (m.Success)
            {
                int val;
                if (int.TryParse(m.Groups[1].Value, out val)) return val;
            }
            return 0;
        }
    }

    /// <summary>
    /// 單一執行步驟
    /// </summary>
    public class TaskStep
    {
        public int Step { get; set; }
        public string Type { get; set; }
        public string Desc { get; set; }
        public string Target { get; set; }
        public string Keys { get; set; }
        public int Ms { get; set; }
        public string Command { get; set; }
        public string Keyword { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Text { get; set; }
        public string Url { get; set; }
        public string Path { get; set; }
        public string Query { get; set; }
        public string Description { get; set; }
    }
}
