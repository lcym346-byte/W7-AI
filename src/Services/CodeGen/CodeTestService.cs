using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private const string TEST_SYSTEM_INSTRUCTION =
            "你是一個 C# 測試程式碼生成器。請嚴格遵守以下規則：\n" +
            "1. 目標平台是 .NET Framework 4.0，C# 5.0 語法\n" +
            "2. 不可使用 async/await、string interpolation $\"\"、pattern matching、lambda =>\n" +
            "3. Event handlers 必須用 delegate 關鍵字\n" +
            "4. 測試程式會用 System.Reflection 載入被測試的 DLL，找到 MainForm\n" +
            "5. 測試程式是一個獨立的 Console Application\n" +
            "6. 用 ```csharp 和 ``` 包裹程式碼\n" +
            "7. 只回傳程式碼，不要加解釋文字\n" +
            "8. 測試結果用 Console.WriteLine 輸出，格式為：\n" +
            "   PASS: 測試描述\n" +
            "   FAIL: 測試描述 - 原因\n" +
            "9. 最後輸出 SUMMARY: X passed, Y failed\n" +
            "10. 測試邏輯：建立 MainForm 實例，用 Reflection 取得控件，模擬操作，檢查結果\n";

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

            // 靜態分析：檢查控件是否正確連接
            tests.AddRange(StaticAnalysis(sourceCode, userRequirement));

            // AI 分析：根據需求生成測試項目
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
                // 檢查是否有數字按鈕
                bool hasNumberButtons = code.Contains("numberButtons") || 
                    Regex.IsMatch(code, @"btn\d|button\d|numBtn", RegexOptions.IgnoreCase);
                
                tests.Add(new TestCase(
                    "數字按鈕存在",
                    hasNumberButtons ? TestResult.Pass : TestResult.Fail,
                    hasNumberButtons ? "" : "找不到數字按鈕的宣告"));

                // 檢查數字按鈕 Click 事件是否正確寫入 TextBox
                if (hasNumberButtons)
                {
                    // 常見錯誤：用 ActiveControl 取得焦點控件，但按按鈕時焦點會移到按鈕本身
                    bool usesActiveControl = code.Contains("ActiveControl");
                    bool usesFocused = code.Contains(".Focused");
                    bool directWrite = Regex.IsMatch(code, @"txt\w+\.Text\s*(\+\=|\=)");

                    if (usesActiveControl && !usesFocused && !directWrite)
                    {
                        tests.Add(new TestCase(
                            "數字鍵盤輸入方式",
                            TestResult.Fail,
                            "使用 ActiveControl 取得焦點控件，但按下按鈕時焦點會移到按鈕本身，" +
                            "導致數字無法寫入 TextBox。應改用 .Focused 屬性判斷或用變數記錄最後焦點的 TextBox"));
                    }
                    else if (usesFocused || directWrite)
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
                bool hasMixedCalc = code.Contains("* 60") && code.Contains("+ int.Parse");
                if (!hasMixedCalc)
                    hasMixedCalc = code.Contains("*60") && code.Contains("+int.Parse");

                tests.Add(new TestCase(
                    "分鐘+秒計算邏輯",
                    hasMixedCalc ? TestResult.Pass : TestResult.Fail,
                    hasMixedCalc ? "" : "未發現 (分鐘*60)+秒 的計算邏輯，分鐘輸入可能無效"));
            }

            // 檢查 3：重置功能是否恢復背景色
            if (requirement.Contains("重置") || requirement.Contains("reset") || requirement.Contains("復原"))
            {
                bool hasColorReset = code.Contains("BackColor") && 
                    (code.Contains("SystemColors.Control") || code.Contains("Color.White") || 
                     code.Contains("DefaultBackColor") || Regex.IsMatch(code, @"BackColor\s*=.*(?:Control|White|Default)"));

                bool hasColorChange = Regex.IsMatch(code, @"BackColor\s*=\s*.*Color\.Red");

                if (hasColorChange && !hasColorReset)
                {
                    tests.Add(new TestCase(
                        "重置後恢復背景色",
                        TestResult.Fail,
                        "有變色功能但重置時未恢復背景色，應加入 this.BackColor = SystemColors.Control;"));
                }
                else if (hasColorChange && hasColorReset)
                {
                    tests.Add(new TestCase(
                        "重置後恢復背景色",
                        TestResult.Pass, ""));
                }
            }

            // 檢查 4：是否有響鈴功能
            if (requirement.Contains("響鈴") || requirement.Contains("提示") || requirement.Contains("beep"))
            {
                bool hasBeep = code.Contains("SystemSounds.Beep") || code.Contains("Console.Beep");
                tests.Add(new TestCase(
                    "響鈴提示功能",
                    hasBeep ? TestResult.Pass : TestResult.Fail,
                    hasBeep ? "" : "未實作響鈴功能"));
            }

            // 檢查 5：空白輸入預設為 0
            if (requirement.Contains("預設") || requirement.Contains("空白") || requirement.Contains("default"))
            {
                bool hasDefault = code.Contains("TryParse") || 
                    (code.Contains(".Text") && code.Contains("\"0\""));
                tests.Add(new TestCase(
                    "空白輸入預設值處理",
                    hasDefault ? TestResult.Pass : TestResult.Fail,
                    hasDefault ? "" : "未處理空白輸入的預設值，可能導致 FormatException"));
            }

            // 檢查 6：倒數到 0 時是否停止
            if (requirement.Contains("倒數") || requirement.Contains("計時"))
            {
                bool hasStopLogic = code.Contains("timer.Stop()") || code.Contains("Stop()");
                bool hasZeroCheck = code.Contains("<= 0") || code.Contains("< 0") || code.Contains("== 0");
                tests.Add(new TestCase(
                    "倒數到零停止計時",
                    (hasStopLogic && hasZeroCheck) ? TestResult.Pass : TestResult.Fail,
                    (hasStopLogic && hasZeroCheck) ? "" : "未發現倒數到零時停止計時的邏輯"));
            }

            return tests;
        }

        /// <summary>
        /// 用 AI 分析需求，找出可能的功能缺陷
        /// </summary>
        private List<TestCase> AiGenerateTestCases(string code, string requirement)
        {
            List<TestCase> tests = new List<TestCase>();
            if (_aiRouter == null) return tests;

            string prompt = string.Format(
                "分析以下程式碼是否完整實現了使用者的需求。\n\n" +
                "使用者需求：{0}\n\n" +
                "程式碼：\n```csharp\n{1}\n```\n\n" +
                "請列出所有功能缺陷，每行一個，格式為：\n" +
                "FAIL: 問題描述 | 修復建議\n" +
                "如果某個功能正確，格式為：\n" +
                "PASS: 功能描述\n" +
                "只輸出 PASS/FAIL 行，不要其他文字。最多列出 10 項。",
                requirement, code);

            string response = _aiRouter.SendMessage(prompt, "");
            if (string.IsNullOrEmpty(response)) return tests;

            // 解析 AI 回應
            string[] lines = response.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("PASS:"))
                {
                    tests.Add(new TestCase(
                        trimmed.Substring(5).Trim(),
                        TestResult.Pass, ""));
                }
                else if (trimmed.StartsWith("FAIL:"))
                {
                    string content = trimmed.Substring(5).Trim();
                    string[] parts = content.Split(new char[] { '|' }, 2);
                    tests.Add(new TestCase(
                        parts[0].Trim(),
                        TestResult.Fail,
                        parts.Length > 1 ? parts[1].Trim() : ""));
                }
            }

            return tests;
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
            foreach (var test in failedTests)
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
            sb.AppendLine("請修正以上所有問題，回傳完整修正後的程式碼。");
            sb.AppendLine("【重要】只修改有問題的部分，不要改變其他正常的功能。");

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
