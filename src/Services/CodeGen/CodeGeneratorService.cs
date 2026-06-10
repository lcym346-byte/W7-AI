using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using AIAgentTool.Services.AI;
using AIAgentTool.Utils;


namespace AIAgentTool.Services.CodeGen
{
    /// <summary>
    /// 程式碼生成服務
    /// 接收自然語言需求 → 呼叫 AI 或使用本地模板 → 回傳 C# 原始碼
    /// </summary>
    public class CodeGeneratorService
    {
        private readonly AiRouter _aiRouter;
        private readonly CodeTemplateLibrary _templates;

        // AI 系統指令 — 指導 AI 如何生成程式碼
         private const string SYSTEM_INSTRUCTION =
    "你是一個 C# 程式碼生成器。請嚴格遵守以下規則：\n" +
    "1. 只使用 .NET Framework 4.0 可用的 API（不可用 async/await, HttpClient, string interpolation $\"\")\n" +
    "2. 必須回傳完整可編譯的 .cs 原始碼，包含 using、namespace、class\n" +
    "3. 只回傳程式碼，不要加任何解釋文字\n" +
    "4. 用 ```csharp 和 ``` 包裹程式碼\n" +
    "5. 程式碼要有中文註解說明功能\n" +
    "6. 必須使用 Windows Forms (WinForms) GUI 介面，不要用 Console.ReadLine() 或 Console.ReadKey()\n" +
    "7. Main 方法要加 [STAThread] 屬性，並呼叫 Application.EnableVisualStyles() 和 Application.Run(new MainForm())\n" +
    "8. 所有使用者互動透過 TextBox、Button、Label 等 WinForms 控件完成\n" +
    "9. 引用 System.Windows.Forms、System.Drawing 和 System.Media（程式開頭必須加 using System.Media;）\n" +
    "10. 必須完整實現使用者描述的每一個功能點，不可省略任何要求\n" +
    "11. 仔細閱讀使用者的描述，區分「鬧鐘」（設定時間到了提醒）和「計時器」（碼表/倒數）的差別\n" +
    "12. 如果使用者要求放在特定路徑，程式本身不需處理路徑，只需寫好程式功能\n" +
    "13. 當使用者要求「變顏色」「提示」「響鈴」等功能時，必須實作（用 SystemSounds.Beep 或 Console.Beep 響鈴，用 BackColor 變色）\n" +
    "14. 【最重要】所有 WinForms 控件必須在 class 中宣告為欄位（private Label xxx; private Button xxx; 等），" +
        "並在建構函式中 new 出來、設定屬性、加入 this.Controls。絕對不可以使用 InitializeComponent()，" +
        "因為沒有 Designer 檔案。每個控件都必須手動設定 Location、Size、Text 等屬性。\n" +
    "15. 不要宣告沒有初始化的變數。所有控件和變數都必須在使用前完整建立和賦值。\n" +
    "16. Timer 使用 System.Windows.Forms.Timer，在建構函式中 new 並設定 Interval 和 Tick 事件。\n" +
    "17. 程式碼中的 class 名稱必須叫 MainForm，繼承 Form。\n" +
    "18. 計時器/倒數功能：如果有「分鐘」和「秒」兩個輸入框，總秒數必須用 (分鐘*60)+秒 計算。不可忽略分鐘欄位。倒數時每秒遞減總秒數，顯示格式用 (totalSeconds/60) 和 (totalSeconds%60) 轉換為 mm:ss。\n" +
    "19. 當使用者報告「輸入分鐘沒用」「只能60秒」等問題時，代表你的程式沒有正確讀取分鐘欄的數字，請確保 int totalSeconds = (int.Parse(txtMin.Text) * 60) + int.Parse(txtSec.Text); 這樣的邏輯存在。\n" +
    "20. Event handlers must use the delegate keyword. NEVER use arrow => lambda syntax. Correct: timer.Tick += delegate(object sender, EventArgs e) { ... }; Wrong: timer.Tick += (s,e) => { ... };\n" +
    "21. 【C# 版本限制 - 極重要】編譯器為 .NET 4.0 的 csc（等同 C# 5.0），以下語法全部禁止使用：\n" +
    "    - 禁止 pattern matching：不可寫 'if (x is Type y)'，必須改為 'Type y = x as Type; if (y != null)'\n" +
    "    - 禁止 string interpolation：不可寫 '$\"...\"'，必須改為 string.Format()\n" +
    "    - 禁止 null conditional operator：不可寫 'x?.Method()'，必須改為 'if (x != null) x.Method()'\n" +
    "    - 禁止 expression-bodied members：不可寫 'int Foo() => 123;'，必須用完整大括號 '{ return 123; }'\n" +
    "    - 禁止 nameof() 運算子：改用字串常數\n" +
    "    - 禁止 auto-property initializer：不可寫 'public int X { get; } = 5;'，必須在建構函式中賦值\n" +
    "    - 禁止 using static\n" +
    "    - 禁止 exception filter (when)\n" +
    "    - 禁止 local function\n" +
    "    違反以上任何一條都會導致編譯失敗！\n";


        
        public CodeGeneratorService(AiRouter aiRouter)
        {
            _aiRouter = aiRouter;
            _templates = new CodeTemplateLibrary();
        }

        /// <summary>
        /// 從自然語言描述生成程式碼
        /// </summary>
        /// <param name="description">使用者的自然語言描述</param>
        /// <returns>C# 原始碼，或錯誤訊息</returns>
        public string GenerateCode(string description)
        {
            // 清理使用者輸入
            description = CleanDescription(description);

            if (string.IsNullOrEmpty(description))
                return "// 請描述你想要的程式功能";

            // 策略 1: 嘗試用 AI 生成
            string aiCode = TryAiGeneration(description);
            if (!string.IsNullOrEmpty(aiCode))
                return aiCode;

            // 策略 2: 使用本地模板
            string templateCode = _templates.FindBestTemplate(description);
            if (!string.IsNullOrEmpty(templateCode))
                return templateCode;

            // 策略 3: 回傳基本骨架
            return GenerateBasicSkeleton(description);
        }

        /// <summary>
        /// 嘗試用 AI 生成程式碼
        /// </summary>
        private string TryAiGeneration(string description)
        {
            if (_aiRouter == null) return null;

            string prompt = string.Format(
                "請用 C# (.NET Framework 4.0) 寫一個程式：{0}\n\n" +
                "要求：完整的 .cs 檔案，可以直接編譯執行。",
                description);

            string response = _aiRouter.SendMessage(prompt, SYSTEM_INSTRUCTION);

            if (string.IsNullOrEmpty(response))
                return null;

            // 從 AI 回應中擷取程式碼區塊
            string code = HtmlHelper.ExtractCodeBlock(response);

            // 驗證是否為有效的 C# 程式碼
            if (!string.IsNullOrEmpty(code) && IsValidCSharpSource(code))
                return code;

            // 如果沒有 code block，但回應看起來像程式碼
            if (IsValidCSharpSource(response))
                return response.Trim();

            return null;
        }
        /// <summary>
        /// 請 AI 修正編譯錯誤（接受錯誤列表）
        /// </summary>
        public string FixCompileErrors(string source, List<string> errors)
        {
            string combined = string.Join("\n", errors.ToArray());
            return FixCompileErrors(source, combined);
        }


        /// <summary>
        /// 請 AI 修正編譯錯誤
        /// </summary>
        public string FixCompileErrors(string source, string errors)
{
    if (_aiRouter == null) return null;

    string prompt = string.Format(
        "以下 C# 程式碼有編譯錯誤，請修正後回傳完整的正確程式碼：\n\n" +
        "原始碼：\n```csharp\n{0}\n```\n\n" +
        "編譯錯誤：\n{1}\n\n" +
        "修正規則（必須遵守）：\n" +
        "- 目標平台是 .NET Framework 4.0 + C# 5（csc 4.6 編譯器）\n" +
        "- 不可使用 string interpolation ($\"...\")\n" +
        "- 不可使用 var 在匿名委派或 lambda 裡面\n" +
        "- Timer.Tick 事件必須用 delegate(object s, EventArgs ev) {{ }} 語法，不可用 lambda (s,e) => {{}}\n" +
        "- 匿名委派內部不可宣告與外部同名的變數\n" +
        "- 所有區域變數必須在使用前宣告和賦值\n" +
        "- 不可使用 InitializeComponent()\n" +
        "- 不可使用 C# 6/7/8 的任何語法（pattern matching 'if(x is Type y)'、string interpolation '$\"\"'、null conditional '?.'、expression body '=>'、nameof、local function）\n" +
        "- 如果錯誤是 CS1525 或 CS1056，通常是用了 'if (x is Type y)' 這種 C#7 pattern matching，必須改為 'Type y = x as Type; if (y != null)'\n" +
        "- 如果錯誤是 CS1644，代表使用了目標編譯器不支援的語言功能\n" +
        "- 請只回傳修正後的完整程式碼，用 ```csharp 包裹\n",
        source, errors);

    string response = _aiRouter.SendMessage(prompt, SYSTEM_INSTRUCTION);

    if (string.IsNullOrEmpty(response))
        return null;

    string fixedCode = HtmlHelper.ExtractCodeBlock(response);
    if (!string.IsNullOrEmpty(fixedCode))
        return fixedCode;

    if (IsValidCSharpSource(response))
        return response.Trim();

    return null;
}



        /// <summary>
        /// 清理使用者描述
        /// </summary>
        private string CleanDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return "";

            // 移除常見的前綴指令詞
            description = Regex.Replace(description,
                @"^(寫程式|寫一個程式|寫個程式|程式|code|write|generate|create|make)\s*",
                "", RegexOptions.IgnoreCase).Trim();

            // 移除多餘的標點
            description = Regex.Replace(description, @"[，,。.：:]+$", "").Trim();

            return description;
        }

        /// <summary>
        /// 檢查是否為有效的 C# 原始碼
        /// </summary>
        private bool IsValidCSharpSource(string code)
        {
            if (string.IsNullOrEmpty(code)) return false;
            if (code.Length < 50) return false;

            // 基本結構檢查
            bool hasUsing = code.Contains("using ");
            bool hasClass = code.Contains("class ");
            bool hasMethod = code.Contains("void ") || code.Contains("static ");

            return hasUsing && hasClass && hasMethod;
        }

        /// <summary>
        /// 生成基本程式骨架（最終 fallback）
        /// </summary>
        private string GenerateBasicSkeleton(string description)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine();
            sb.AppendLine("namespace GeneratedApp");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine(string.Format("    /// {0}", description));
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    class Program");
            sb.AppendLine("    {");
            sb.AppendLine("        static void Main(string[] args)");
            sb.AppendLine("        {");
            sb.AppendLine(string.Format(
                "            Console.WriteLine(\"程式功能: {0}\");", description));
            sb.AppendLine("            Console.WriteLine(\"請在此處實作功能...\");");
            sb.AppendLine();
            sb.AppendLine("            // TODO: 在此加入你的程式碼");
            sb.AppendLine();
            sb.AppendLine("            Console.WriteLine(\"\\n按任意鍵結束...\");");
            sb.AppendLine("            Console.ReadKey();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
