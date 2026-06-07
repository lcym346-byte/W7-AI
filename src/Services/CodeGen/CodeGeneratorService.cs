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
            "你是一個 C# 程式碼生成器。請遵守以下規則：\n" +
            "1. 只使用 .NET Framework 4.0 可用的 API（不可用 async/await, HttpClient, string interpolation $\"\"）\n" +
            "2. 必須回傳完整可編譯的 .cs 原始碼，包含 using、namespace、class\n" +
            "3. 只回傳程式碼，不要加任何解釋文字\n" +
            "4. 用 ```csharp 和 ``` 包裹程式碼\n" +
            "5. 程式碼要有中文註解說明功能\n" +
            "6. 必須使用 Windows Forms (WinForms) GUI 介面，不要用 Console.ReadLine() 或 Console.ReadKey()\n" +
            "7. Main 方法要加 [STAThread] 屬性，並呼叫 Application.EnableVisualStyles() 和 Application.Run(new MainForm())\n" +
            "8. 所有使用者互動透過 TextBox、Button、Label 等 WinForms 控件完成\n" +
            "9. 引用 System.Windows.Forms 和 System.Drawing";


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
                "請只回傳修正後的完整程式碼，用 ```csharp 包裹。",
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
