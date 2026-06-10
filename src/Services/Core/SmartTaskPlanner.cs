using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AIAgentTool.Services.AI;

namespace AIAgentTool.Services.Core
{
    public class SmartTaskPlanner
    {
        private readonly AiRouter _ai;

        private const string PLAN_PROMPT =
            "你是一個 Windows 7 電腦操作助手。使用者會給你一個自然語言指令，你需要將它拆解為具體的電腦操作步驟。\n\n" +
            "可用的操作類型（type 欄位）：\n" +
            "- launch_app: 啟動程式（需要 target 欄位指定程式名或路徑）\n" +
            "- close_app: 關閉程式（需要 target 欄位）\n" +
            "- send_keys: 模擬鍵盤輸入（需要 keys 欄位）\n" +
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
            "- generate_image: 生成圖片（需要 text 欄位描述）\n" +
            "- generate_video: 生成影片（需要 text 欄位描述）\n" +
            "- text_to_speech: 文字轉語音（需要 text 欄位）\n" +
            "- knowledge_search: 搜尋知識庫（需要 text 欄位）\n" +
            "- knowledge_add: 加入文件到知識庫（需要 path 欄位）\n" +
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
            "- Windows 7 沒有內建鬧鐘程式，如需鬧鐘功能請用 generate_code 產生一個\n";

        public SmartTaskPlanner(AiRouter aiRouter)
        {
            _ai = aiRouter;
        }

        public List<TaskStep> PlanTask(string userInput)
        {
            string actualInput = userInput;
            int currentTag = userInput.IndexOf("[CURRENT] ");
            if (currentTag >= 0)
                actualInput = userInput.Substring(currentTag + 10).Trim();

            List<TaskStep> localPlan = TryLocalPlan(actualInput);
            if (localPlan != null) return localPlan;

            string response = _ai.SendMessage(userInput, PLAN_PROMPT);
            if (string.IsNullOrEmpty(response))
                return FallbackPlan(actualInput);

            return ParsePlanJson(response);
        }

        private List<TaskStep> TryLocalPlan(string input)
        {
            string lower = input.ToLower().Trim();

            // ★ 程式碼修正匹配（最優先）
            bool wantFix = lower.Contains("修正") || lower.Contains("修復") ||
                lower.Contains("修改") || lower.Contains("改一下") ||
                lower.Contains("問題") || lower.Contains("沒用") ||
                lower.Contains("不能") || lower.Contains("無法") ||
                lower.Contains("壞了") || lower.Contains("錯誤") ||
                lower.Contains("fix") || lower.Contains("bug");

            bool aboutCode = lower.Contains("程式") || lower.Contains("計時") ||
                lower.Contains("倒數") || lower.Contains("分鐘") ||
                lower.Contains("功能") || lower.Contains("按鈕") ||
                lower.Contains("code") || lower.Contains("timer");

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

            // ★ 圖片生成
            if ((lower.Contains("生成圖") || lower.Contains("畫一") || lower.Contains("畫個") ||
                 lower.Contains("畫張") || lower.Contains("產生圖") ||
                 lower.Contains("generate image") || lower.Contains("draw")) &&
                !lower.Contains("程式") && !lower.Contains("code"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "generate_image";
                step.Desc = "生成圖片";
                step.Text = input;
                steps.Add(step);
                return steps;
            }

            // ★ 影片生成
            if (lower.Contains("生成影片") || lower.Contains("做影片") ||
                lower.Contains("製作影片") || lower.Contains("generate video") ||
                lower.Contains("做一段影片") || lower.Contains("短影片"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "generate_video";
                step.Desc = "生成影片";
                step.Text = input;
                steps.Add(step);
                return steps;
            }

            // ★ 語音合成
            if (lower.Contains("朗讀") || lower.Contains("唸出") || lower.Contains("唸") ||
                lower.Contains("語音") || lower.Contains("tts") ||
                lower.Contains("text to speech") || lower.Contains("說出"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "text_to_speech";
                step.Desc = "語音合成";
                step.Text = input;
                steps.Add(step);
                return steps;
            }

            // ★ 知識庫搜尋
            if (lower.Contains("知識庫") || lower.Contains("搜尋文件") ||
                lower.Contains("search knowledge") || lower.Contains("查資料"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "knowledge_search";
                step.Desc = "搜尋知識庫";
                step.Text = input;
                steps.Add(step);
                return steps;
            }

            // ★ 加入文件到知識庫
            if (lower.Contains("加入知識庫") || lower.Contains("匯入文件") ||
                lower.Contains("add to knowledge"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "knowledge_add";
                step.Desc = "加入知識庫";
                step.Text = input;
                steps.Add(step);
                return steps;
            }

            // ★ 截圖
            if (lower.Contains("截圖") || lower.Contains("螢幕") ||
                lower.Contains("screenshot") || lower.Contains("screen capture") ||
                lower.Contains("capture screen"))
            {
                List<TaskStep> plan = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "screenshot";
                step.Desc = "螢幕截圖";
                plan.Add(step);
                return plan;
            }

            // ★ 列出視窗
            if (lower.Contains("列出視窗") || lower.Contains("有哪些視窗") ||
                lower.Contains("list windows") || lower.Contains("開了什麼"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "list_windows";
                step.Desc = "列出視窗";
                steps.Add(step);
                return steps;
            }

            // ★ 切換視窗
            if (lower.Contains("切換到") || lower.Contains("打開視窗") ||
                lower.Contains("focus window") || lower.Contains("switch to"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "focus_window";
                step.Desc = "切換視窗";
                step.Text = input;
                steps.Add(step);
                return steps;
            }

            // ★ 點擊操作
            if (lower.Contains("點擊") || lower.Contains("按一下") ||
                lower.Contains("click at") || lower.Contains("click on"))
            {
                List<TaskStep> steps = new List<TaskStep>();
                TaskStep step = new TaskStep();
                step.Step = 1;
                step.Type = "click";
                step.Desc = "滑鼠點擊";
                // 嘗試解析座標
                Match coordMatch = Regex.Match(input, @"(\d+)\s*[,，\s]\s*(\d+)");
                if (coordMatch.Success)
                {
                    int.TryParse(coordMatch.Groups[1].Value, out int cx);
                    int.TryParse(coordMatch.Groups[2].Value, out int cy);
                    step.X = cx;
                    step.Y = cy;
                }
                steps.Add(step);
                return steps;
            }

            // ★ 程式碼生成匹配
            bool wantCode = lower.Contains("write") || lower.Contains("make") ||
                lower.Contains("create") || lower.Contains("build") || lower.Contains("code") ||
                lower.Contains("寫") || lower.Contains("製作") ||
                lower.Contains("做") || lower.Contains("產生") ||
                lower.Contains("生成") || lower.Contains("建立") ||
                lower.Contains("程式") || lower.Contains("軟體") ||
                lower.Contains("工具");

            bool isCodeTarget = lower.Contains("app") || lower.Contains("gui") ||
                lower.Contains("winforms") || lower.Contains("program") ||
                lower.Contains("程式") || lower.Contains("軟體") ||
                lower.Contains("工具") || lower.Contains("計算機") ||
                lower.Contains("鬧鐘") || lower.Contains("遊戲") ||
                lower.Contains("視窗") || lower.Contains("介面") ||
                lower.Contains("放在") || lower.Contains("存到") ||
                lower.Contains("儲存");

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

            // ★ 開啟程式
            if (lower.Contains("open") || lower.Contains("launch") || lower.Contains("start") ||
                lower.Contains("開啟") || lower.Contains("打開") ||
                lower.Contains("啟動") || lower.Contains("執行"))
            {
                string target = input;
                string[] removePrefixes = new string[] {
                    "開啟", "打開", "啟動", "執行", "open", "launch", "start"
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
                    step.Desc = "啟動 " + target;
                    plan.Add(step);
                    return plan;
                }
            }

            // ★ 關閉程式
            if (lower.Contains("close") || lower.Contains("kill") || lower.Contains("stop") ||
                lower.Contains("關閉") || lower.Contains("結束") || lower.Contains("停止"))
            {
                string target = input;
                string[] removePrefixes = new string[] {
                    "關閉", "結束", "停止", "close", "kill", "stop"
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
                    step.Desc = "關閉 " + target;
                    plan.Add(step);
                    return plan;
                }
            }

            // ★ 搜尋網路
            if ((lower.Contains("搜尋") || lower.Contains("搜索") ||
                 lower.Contains("找") || lower.Contains("查") ||
                 lower.Contains("search")) &&
                !lower.Contains("檔案") && !lower.Contains("file") &&
                !lower.Contains("知識庫") && !lower.Contains("文件"))
            {
                string query = input;
                string[] removePrefixes = new string[] {
                    "搜尋", "搜索", "找", "查", "search"
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
                    step.Desc = "搜尋: " + query;
                    plan.Add(step);
                    return plan;
                }
            }

            return null;
        }

        private List<TaskStep> FallbackPlan(string input)
        {
            List<TaskStep> plan = new List<TaskStep>();
            TaskStep step = new TaskStep();
            step.Step = 1;
            step.Type = "message";
            step.Text = "AI 服務不可用，無法分析您的指令。請嘗試更簡單的指令如「開啟記事本」「截圖」等。";
            step.Desc = "AI unavailable";
            plan.Add(step);
            return plan;
        }

        private List<TaskStep> ParsePlanJson(string json)
        {
            List<TaskStep> steps = new List<TaskStep>();

            try
            {
                json = json.Trim();
                if (json.StartsWith("```"))
                {
                    int firstNewline = json.IndexOf('\n');
                    if (firstNewline > 0) json = json.Substring(firstNewline + 1);
                    int lastBacktick = json.LastIndexOf("```");
                    if (lastBacktick > 0) json = json.Substring(0, lastBacktick);
                    json = json.Trim();
                }

                int start = json.IndexOf('[');
                int end = json.LastIndexOf(']');
                if (start < 0 || end < 0 || end <= start)
                {
                    TaskStep errStep = new TaskStep();
                    errStep.Step = 1;
                    errStep.Type = "message";
                    errStep.Text = "AI 回覆格式錯誤，無法解析執行計畫。";
                    errStep.Desc = "parse failed";
                    steps.Add(errStep);
                    return steps;
                }

                json = json.Substring(start, end - start + 1);

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
