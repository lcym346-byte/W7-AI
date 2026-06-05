using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.CSharp;

namespace AIAgentTool.Services.CodeGen
{
    /// <summary>
    /// 程式碼編譯服務
    /// 使用 CSharpCodeProvider 動態編譯 C# 原始碼為 .exe
    /// .NET 4.0 內建，不需額外安裝
    /// </summary>
    public class CodeCompilerService
    {
        // 預設輸出目錄
        private readonly string _outputDirectory;

        public CodeCompilerService()
        {
            _outputDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Generated");

            if (!Directory.Exists(_outputDirectory))
            {
                try { Directory.CreateDirectory(_outputDirectory); }
                catch { }
            }
        }
        /// <summary>
        /// 編譯 C# 原始碼（自動產生檔名）
        /// </summary>
        public CompileResult Compile(string sourceCode)
        {
            return Compile(sourceCode, null);
        }


        /// <summary>
        /// 編譯 C# 原始碼為 .exe
        /// </summary>
        /// <param name="sourceCode">完整的 C# 原始碼</param>
        /// <param name="outputFileName">輸出檔名（不含路徑）</param>
        /// <returns>編譯結果</returns>
        public CompileResult Compile(string sourceCode, string outputFileName)
        {
            CompileResult result = new CompileResult();

            if (string.IsNullOrEmpty(sourceCode))
            {
                result.Success = false;
                result.Errors.Add("原始碼為空");
                return result;
            }

            // 確定輸出檔名
            if (string.IsNullOrEmpty(outputFileName))
            {
                outputFileName = string.Format("Generated_{0}.exe",
                    DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            }
            if (!outputFileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                outputFileName += ".exe";

            string outputPath = Path.Combine(_outputDirectory, outputFileName);

            try
            {
                // 設定編譯參數
                CompilerParameters parameters = new CompilerParameters();
                parameters.GenerateExecutable = true;
                parameters.OutputAssembly = outputPath;
                parameters.GenerateInMemory = false;
                parameters.TreatWarningsAsErrors = false;
                parameters.IncludeDebugInformation = false;

                // 加入常用的組件參考
                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("System.Core.dll");
                parameters.ReferencedAssemblies.Add("System.Drawing.dll");
                parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
                parameters.ReferencedAssemblies.Add("System.Data.dll");
                parameters.ReferencedAssemblies.Add("System.Xml.dll");
                parameters.ReferencedAssemblies.Add("System.Net.dll");
                parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");

                // 判斷是否為 WinForms 程式
                if (sourceCode.Contains("Application.Run") ||
                    sourceCode.Contains("System.Windows.Forms"))
                {
                    parameters.CompilerOptions = "/target:winexe";
                }

                // 執行編譯
                CSharpCodeProvider provider = new CSharpCodeProvider();
                CompilerResults compilerResults =
                    provider.CompileAssemblyFromSource(parameters, sourceCode);

                // 處理結果
                if (compilerResults.Errors.HasErrors)
                {
                    result.Success = false;
                    foreach (CompilerError error in compilerResults.Errors)
                    {
                        if (!error.IsWarning)
                        {
                            result.Errors.Add(string.Format(
                                "第 {0} 行: {1} ({2})",
                                error.Line, error.ErrorText, error.ErrorNumber));
                        }
                    }
                }
                else
                {
                    result.Success = true;
                    result.OutputPath = outputPath;
                    result.FileSize = new FileInfo(outputPath).Length;
                }

                // 收集警告
                foreach (CompilerError error in compilerResults.Errors)
                {
                    if (error.IsWarning)
                    {
                        result.Warnings.Add(string.Format(
                            "第 {0} 行: {1}", error.Line, error.ErrorText));
                    }
                }

                provider.Dispose();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add("編譯過程例外: " + ex.Message);
            }

            return result;
        }

        /// <summary>
        /// 執行已編譯的 .exe 並擷取輸出
        /// </summary>
        public string ExecuteCompiled(string exePath, int timeoutMs)
        {
            if (!File.Exists(exePath))
                return "✗ 找不到執行檔: " + exePath;

            StringBuilder sb = new StringBuilder();

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = exePath;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;

                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                bool exited = process.WaitForExit(timeoutMs);

                if (!exited)
                {
                    process.Kill();
                    sb.AppendLine("⚠ 程式執行逾時，已強制終止");
                }

                if (!string.IsNullOrEmpty(output))
                {
                    sb.AppendLine("═══ 程式輸出 ═══");
                    sb.AppendLine(output);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    sb.AppendLine("═══ 錯誤輸出 ═══");
                    sb.AppendLine(error);
                }

                if (exited)
                {
                    sb.AppendLine(string.Format("\n結束代碼: {0}", process.ExitCode));
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine(string.Format("✗ 執行失敗: {0}", ex.Message));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 儲存原始碼到檔案
        /// </summary>
        public string SaveSource(string sourceCode, string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = Path.Combine(_outputDirectory,
                        string.Format("Source_{0}.cs",
                            DateTime.Now.ToString("yyyyMMdd_HHmmss")));
                }

                File.WriteAllText(filePath, sourceCode, Encoding.UTF8);
                return string.Format("✓ 原始碼已儲存: {0}", filePath);
            }
            catch (Exception ex)
            {
                return string.Format("✗ 儲存失敗: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 取得輸出目錄路徑
        /// </summary>
        public string GetOutputDirectory()
        {
            return _outputDirectory;
        }
    }

    /// <summary>
    /// 編譯結果模型
    /// </summary>
    public class CompileResult
    {
        public bool Success { get; set; }
        public string OutputPath { get; set; }
        public long FileSize { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }

        public CompileResult()
        {
            Success = false;
            OutputPath = "";
            FileSize = 0;
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public string GetSummary()
        {
            StringBuilder sb = new StringBuilder();

            if (Success)
            {
                sb.AppendLine("✓ 編譯成功！");
                sb.AppendLine(string.Format("  輸出: {0}", OutputPath));
                sb.AppendLine(string.Format("  大小: {0:F1} KB", FileSize / 1024.0));
            }
            else
            {
                sb.AppendLine("✗ 編譯失敗");
                sb.AppendLine(string.Format("  共 {0} 個錯誤:", Errors.Count));
                foreach (string err in Errors)
                {
                    sb.AppendLine(string.Format("    • {0}", err));
                }
            }

            if (Warnings.Count > 0)
            {
                sb.AppendLine(string.Format("\n  ⚠ {0} 個警告:", Warnings.Count));
                foreach (string warn in Warnings)
                {
                    sb.AppendLine(string.Format("    • {0}", warn));
                }
            }

            return sb.ToString();
        }
    }
}
