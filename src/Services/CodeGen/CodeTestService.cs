using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using AIAgentTool.Services.AI;
using AIAgentTool.Utils;

namespace AIAgentTool.Services.CodeGen
{
    /// <summary>
    /// 程式碼自動測試服務
    /// 讓 AI 生成測試程式碼，自動驗證功能是否正確
    /// </summary>
    public class CodeTestService
    {
        private readonly AiRouter _aiRouter;
        private readonly CodeCompilerService _compiler;

        public CodeTestService(AiRouter aiRouter, CodeCompilerService compiler)
        {
            _aiRouter = aiRouter;
            _compiler = compiler;
        }

        /// <summary>
        /// 分析程式碼，產生功能測試清單
        /// </summary>
        public List<TestCase> AnalyzeAndGenerateTests(string sourceCode, string userRequirement)
        {
            List<TestCase> tests = new List<TestCase>();
            tests.AddRange(StaticAnalysis(sourceCode, userRequirement));
            tests.AddRange(AiGenerateTestCases(sourceCode, userRequirement));
            return tests;
        }

        /// <summary>
        /// 靜態分析 - 不需要執行程式就能發現的問題
        /// </summary>
        private List<TestCase> StaticAnalysis(string code, string requirement)
        {
            List<TestCase> tests = new List<TestCase>();

            // 檢查 1：數字鍵盤按鈕是否有 Click 事件且能寫入 TextBox
            if (requirement.Contains("數字") || requirement.Contains("鍵盤") || requirement.Contains("keypad"))
            {
                bool hasNumberButtons = code.Contains("numberButtons") ||
                    Regex.IsMatch(code, @"btn\d|button\d|numBtn", RegexOptions.IgnoreCase);

                tests.Add(new TestCase(
                    "數字按鈕存在",
                    hasNumberButtons ? TestResult.Pass : TestResult.Fail,
                    hasNumberButtons ? "" : "找不到數字按鈕的宣告"));

                if (hasNumberButtons)
                {
                    bool usesActiveControl = code.Contains("ActiveControl");
                    bool usesFocused = code.Contains(".Focused");
                    bool usesLastFocused = code.Contains("_lastFocused") || code.Contains("lastFocused");
                    bool directWrite = Regex.IsMatch(code, @"txt\w+\.Text\s*(\+\=|\=)");

                    if (usesActiveControl && !usesFocused && !usesLastFocused && !directWrite)
                    {
                        tests.Add(new TestCase(
                            "數字鍵盤輸入方式",
                            TestResult.Fail,
                            "使用 ActiveControl 判斷焦點，但按按鈕時焦點會移到按鈕本身。應改用 _lastFocusedTextBox 變數或 .Focused 屬性"));
                    }
                    else if (usesFocused || usesLastFocused || directWrite)
                    {
                        tests.Add(new TestCase(
                            "數字鍵盤輸入方式",
                            TestResult.Pass, ""));
                    }
                }
            }

            // 檢查 2：倒數計時器是否正確計算分鐘+秒
            if (requirement.Contains("倒數") || requirement.Contains("計時") || requirement.Contains("timer"))
            {
                bool hasMixedCalc = code.Contains("* 60") || code.Contains("*60");

                tests.Add(new TestCase(
                    "分鐘+秒計算邏輯",
                    hasMixedCalc ? TestResult.Pass : TestResult.Fail,
                    hasMixedCalc ? "" : "未發現 (分鐘*60)+秒 的計算邏輯，分鐘輸入可能無效"));
            }

            // 檢查 3：重置功能是否恢復背景色
            if (requirement.Contains("重置") || requirement.Contains("reset") || requirement.Contains("復原"))
            {
                bool hasColorReset = code.Contains("SystemColors.Control") ||
                    code.Contains("DefaultBackColor") ||
                    Regex.IsMatch(code, @"BackColor\s*=.*(?:Control|Default|Original|original)");

                bool hasColorChange = code.Contains("Color.Red") || code.Contains("Color.Green");

                if (hasColorChange && !hasColorReset)
                {
                    tests.Add(new TestCase(
                        "重置後恢復背景色",
                        TestResult.Fail,
                        "有變色功能但重置時未恢復背景色"));
                }
                else if (hasColorChange && hasColorReset)
                {
                    tests.Add(new TestCase(
                        "重置後恢復背景色",
                        TestResult.Pass, ""));
                }
            }

            // 檢查 4：是否有響鈴功能
            if (requirement.Contains("響鈴") || requirement.Contains("提示音") || requirement.Contains("beep") || requirement.Contains("聲音"))
            {
                bool hasBeep = code.Contains("SystemSounds.Beep") || code.Contains("Console.Beep");
                tests.Add(new TestCase(
                    "響鈴提示功能",
                    hasBeep ? TestResult.Pass : TestResult.Fail,
                    hasBeep ? "" : "未實作響鈴功能"));
            }

            // 檢查 5：倒數到 0 時是否停止
            if (requirement.Contains("倒數") || requirement.Contains("計時"))
            {
                bool hasStopLogic = code.Contains(".Stop()");
                bool hasZeroCheck = code.Contains("<= 0") || code.Contains("== 0") || code.Contains("< 1");
                tests.Add(new TestCase(
                    "倒數到零停止計時",
                    (hasStopLogic && hasZeroCheck) ? TestResult.Pass : TestResult.Fail,
                    (hasStopLogic && hasZeroCheck) ? "" : "未發現倒數到零時停止計時的邏輯"));
            }

            return tests;
        }

        /// <summary>
        /// 用 AI 分析需求，但只檢查核心功能是否存在（不檢查語法風格）
        /// </summary>
        private List<TestCase> AiGenerateTestCases(string code, string requirement)
        {
            List<TestCase> tests = new List<TestCase>();
            if (_aiRouter == null) return tests;

            string prompt = string.Format(
                "分析以下程式碼是否實現了使用者的【核心功能需求】。\n\n" +
                "使用者需求：{0}\n\n" +
                "程式碼：\n```csharp\n{1}\n```\n\n" +
                "【重要規則】\n" +
                "- 只檢查功能是否存在，不要檢查程式碼風格、格式化方式、命名規範\n" +
                "- 不要報告 string.Format 的使用方式問題\n" +
                "- 不要報告 Controls.Add 或 Application.EnableVisualStyles 問題\n" +
                "- 不要報告 Color.Red 或 BackColor 的調用方式問題\n" +
                "- 不要報告任何「程式碼完整性」或「被截斷」問題\n" +
                "- 不要報告引號、括號、分號等語法問題（那是編譯器的工作）\n" +
                "- 只關注：使用者要求的功能（按鈕、計時、顯示等）是否被實作\n" +
                "- 最多列出 5 項\n\n" +
                "格式：\n" +
                "PASS: 功能描述\n" +
                "FAIL: 問題描述 | 修復建議\n" +
                "只輸出 PASS/FAIL 行。",
                requirement, TruncateForPrompt(code, 3000));

            string response = _aiRouter.SendMessage(prompt, "");
            if (string.IsNullOrEmpty(response)) return tests;

            string[] lines = response.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int count = 0;
            foreach (string line in lines)
            {
                if (count >= 5) break;
                string trimmed = line.Trim();
                if (trimmed.StartsWith("PASS:"))
                {
                    tests.Add(new TestCase(
                        trimmed.Substring(5).Trim(),
                        TestResult.Pass, ""));
                    count++;
                }
                else if (trimmed.StartsWith("FAIL:"))
                {
                    string content = trimmed.Substring(5).Trim();
                    if (IsFalsePositive(content)) continue;

                    string[] parts = content.Split(new char[] { '|' }, 2);
                    tests.Add(new TestCase(
                        parts[0].Trim(),
                        TestResult.Fail,
                        parts.Length > 1 ? parts[1].Trim() : ""));
                    count++;
                }
            }

            return tests;
        }

        /// <summary>
        /// 過濾 AI 測試的常見誤報
        /// </summary>
        private bool IsFalsePositive(string failContent)
        {
            string lower = failContent.ToLower();

            if (lower.Contains("string.format")) return true;
            if (lower.Contains("controls.add")) return true;
            if (lower.Contains("enablevisualstyles")) return true;
            if (lower.Contains("application.run")) return true;
            if (lower.Contains("截斷") || lower.Contains("truncat")) return true;
            if (lower.Contains("完整性") || lower.Contains("incomplete")) return true;
            if (lower.Contains("color.red") && lower.Contains("調用")) return true;
            if (lower.Contains("引號") || lower.Contains("quote")) return true;
            if (lower.Contains("格式化") && lower.Contains("引號")) return true;
            if (lower.Contains("命名") || lower.Contains("naming")) return true;
            if (lower.Contains("省略") && lower.Contains("部分")) return true;
            if (lower.Contains("缺少引號")) return true;
            if (lower.Contains("缺少分號")) return true;
            if (lower.Contains("括號")) return true;
            if (lower.Contains("被截斷")) return true;
            if (lower.Contains("不完整")) return true;
            if (lower.Contains("missing")) return true;

            return false;
        }

        /// <summary>
        /// 截斷程式碼避免 prompt 太長
        /// </summary>
        private string TruncateForPrompt(string code, int maxLen)
        {
            if (string.IsNullOrEmpty(code) || code.Length <= maxLen) return code;
            return code.Substring(0, maxLen) + "\n// ... (程式碼繼續)";
        }

        /// <summary>
        /// 根據測試結果，生成修復指令給 AI
        /// </summary>
        public string GenerateFixPrompt(string sourceCode, List<TestCase> failedTests)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("以下程式碼有功能性問題（編譯成功但行為不正確），請修正：");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(sourceCode);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("功能測試發現的問題：");

            int count = 1;
            foreach (TestCase test in failedTests)
            {
                if (test.Result == TestResult.Fail)
                {
                    sb.AppendLine(string.Format("{0}. {1}", count, test.Name));
                    if (!string.IsNullOrEmpty(test.FailReason))
                        sb.AppendLine(string.Format("   修復建議：{0}", test.FailReason));
                    count++;
                }
            }

            sb.AppendLine();
            sb.AppendLine("請修正以上問題，回傳完整修正後的程式碼。");
            sb.AppendLine("【重要限制】");
            sb.AppendLine("- 只修改有問題的部分，不要改變其他正常的功能");
            sb.AppendLine("- 不要修改程式碼的整體結構");
            sb.AppendLine("- 不要重新命名任何變數或方法");
            sb.AppendLine("- 不要新增使用者沒有要求的功能");
            sb.AppendLine("- 保持所有 using、namespace、class 不變");
            sb.AppendLine("- 用 ```csharp 包裹完整程式碼");

            return sb.ToString();
        }
    }

    public enum TestResult
    {
        Pass,
        Fail,
        Skip
    }

    public class TestCase
    {
        public string Name { get; set; }
        public TestResult Result { get; set; }
        public string FailReason { get; set; }

        public TestCase(string name, TestResult result, string failReason)
        {
            Name = name;
            Result = result;
            FailReason = failReason;
        }
    }
}
