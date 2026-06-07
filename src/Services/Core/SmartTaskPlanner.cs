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
            // 先嘗試本地快速匹配（不需要 AI）
            List<TaskStep> localPlan = TryLocalPlan(userInput);
            if (localPlan != null) return localPlan;

            // 使用 AI 規劃
            string response = _ai.SendMessage(userInput, PLAN_PROMPT);
            if (string.IsNullOrEmpty(response))
            {
                // AI 不可用，嘗試基本意圖分析
                return FallbackPlan(userInput);
            }

            return ParsePlanJson(response);
        }

        /// <summary>
        /// 本地快速匹配常見指令
        /// </summary>
        private List<TaskStep> TryLocalPlan(string input)
        {
            string lower = input.ToLower().Trim();

            // 開啟程式的簡單匹配
            Match launchMatch = Regex.Match(lower,
                @"^(開啟|打開|啟動|執行|open|launch|start)\s*(.+)$");
            if (launchMatch.Success)
            {
                string target = launchMatch.Groups[2].Value.Trim();
                List<TaskStep> plan = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "find_and_launch";
                step.Keyword = target;
                step.Desc = "啟動 " + target;
                plan.Add(step);
                return plan;
            }

            // 關閉程式
            Match closeMatch = Regex.Match(lower,
                @"^(關閉|結束|停止|close|kill|stop)\s*(.+)$");
            if (closeMatch.Success)
            {
                string target = closeMatch.Groups[2].Value.Trim();
                List<TaskStep> plan = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "close_app";
                step.Target = target;
                step.Desc = "關閉 " + target;
                plan.Add(step);
                return plan;
            }

            // 截圖
            if (lower.Contains("截圖") || lower.Contains("screenshot"))
            {
                List<TaskStep> plan = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "screenshot";
                step.Desc = "擷取螢幕截圖";
                plan.Add(step);
                return plan;
            }

            // 搜尋網路
            Match searchMatch = Regex.Match(lower,
                @"^(搜尋|搜索|search|找|查)\s*(.+)$");
            if (searchMatch.Success && !lower.Contains("檔案") && !lower.Contains("file"))
            {
                string query = searchMatch.Groups[2].Value.Trim();
                List<TaskStep> plan = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "search_web";
                step.Query = query;
                step.Desc = "搜尋: " + query;
                plan.Add(step);
                return plan;
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
            step.Text = "AI 服務不可用，無法分析您的指令。請嘗試更簡單的指令如「開啟記事本」「截圖」等。";
            step.Desc = "AI 不可用";
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
                    errStep.Text = "AI 回覆格式錯誤，無法解析執行計畫。";
                    errStep.Desc = "解析失敗";
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
                    errStep.Text = "AI 未產生任何執行步驟。";
                    errStep.Desc = "空計畫";
                    steps.Add(errStep);
                }
            }
            catch (Exception ex)
            {
                TaskStep errStep = new TaskStep();
                errStep.Step = 1;
                errStep.Type = "message";
                errStep.Text = "解析失敗: " + ex.Message;
                errStep.Desc = "例外";
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
