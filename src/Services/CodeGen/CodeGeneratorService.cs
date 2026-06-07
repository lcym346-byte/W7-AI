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
            "\u4f60\u662f\u4e00\u500b C# \u7a0b\u5f0f\u78bc\u751f\u6210\u5668\u3002\u8acb\u9075\u5b88\u4ee5\u4e0b\u898f\u5247\uff1a\n" +
            "1. \u53ea\u4f7f\u7528 .NET Framework 4.0 \u53ef\u7528\u7684 API\uff08\u4e0d\u53ef\u7528 async/await, HttpClient, string interpolation $\"\")\n" +
            "2. \u5fc5\u9808\u56de\u50b3\u5b8c\u6574\u53ef\u7de8\u8b6f\u7684 .cs \u539f\u59cb\u78bc\uff0c\u5305\u542b using\u3001namespace\u3001class\n" +
            "3. \u53ea\u56de\u50b3\u7a0b\u5f0f\u78bc\uff0c\u4e0d\u8981\u52a0\u4efb\u4f55\u89e3\u91cb\u6587\u5b57\n" +
            "4. \u7528 ```csharp \u548c ``` \u5305\u88f9\u7a0b\u5f0f\u78bc\n" +
            "5. \u7a0b\u5f0f\u78bc\u8981\u6709\u4e2d\u6587\u8a3b\u89e3\u8aaa\u660e\u529f\u80fd\n" +
            "6. \u5fc5\u9808\u4f7f\u7528 Windows Forms (WinForms) GUI \u4ecb\u9762\uff0c\u4e0d\u8981\u7528 Console.ReadLine() \u6216 Console.ReadKey()\n" +
            "7. Main \u65b9\u6cd5\u8981\u52a0 [STAThread] \u5c6c\u6027\uff0c\u4e26\u547c\u53eb Application.EnableVisualStyles() \u548c Application.Run(new MainForm())\n" +
            "8. \u6240\u6709\u4f7f\u7528\u8005\u4e92\u52d5\u900f\u904e TextBox\u3001Button\u3001Label \u7b49 WinForms \u63a7\u4ef6\u5b8c\u6210\n" +
            "9. \u5f15\u7528 System.Windows.Forms\u3001System.Drawing \u548c System.Media\uff08\u7a0b\u5f0f\u958b\u982d\u5fc5\u9808\u52a0 using System.Media;\uff09\n" +            "10. \u5fc5\u9808\u5b8c\u6574\u5be6\u73fe\u4f7f\u7528\u8005\u63cf\u8ff0\u7684\u6bcf\u4e00\u500b\u529f\u80fd\u9ede\uff0c\u4e0d\u53ef\u7701\u7565\u4efb\u4f55\u8981\u6c42\n" +
            "11. \u4ed4\u7d30\u95b1\u8b80\u4f7f\u7528\u8005\u7684\u63cf\u8ff0\uff0c\u5340\u5206\u300c\u9b27\u9418\u300d\uff08\u8a2d\u5b9a\u6642\u9593\u5230\u4e86\u63d0\u9192\uff09\u548c\u300c\u8a08\u6642\u5668\u300d\uff08\u78bc\u8868\uff09\u7684\u5dee\u5225\n" +
            "12. \u5982\u679c\u4f7f\u7528\u8005\u8981\u6c42\u653e\u5728\u7279\u5b9a\u8def\u5f91\uff0c\u7a0b\u5f0f\u672c\u8eab\u4e0d\u9700\u8655\u7406\u8def\u5f91\uff0c\u53ea\u9700\u5beb\u597d\u7a0b\u5f0f\u529f\u80fd\n" +
            "13. \u7576\u4f7f\u7528\u8005\u8981\u6c42\u300c\u8b8a\u984f\u8272\u300d\u300c\u63d0\u793a\u300d\u300c\u97ff\u9234\u300d\u7b49\u529f\u80fd\u6642\uff0c\u5fc5\u9808\u5be6\u4f5c\uff08\u7528 SystemSounds.Beep \u6216 Console.Beep \u97ff\u9234\uff0c\u7528 BackColor \u8b8a\u8272\uff09";



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
