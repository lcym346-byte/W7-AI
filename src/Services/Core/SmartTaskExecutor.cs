using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using AIAgentTool.Models;
using AIAgentTool.Services.AI;
using AIAgentTool.Services.System;
using AIAgentTool.Services.Search;
using AIAgentTool.Services.CodeGen;

namespace AIAgentTool.Services.Core
{
    /// <summary>
    /// 智慧任務執行器 - 逐步執行 TaskStep 計畫
    /// </summary>
    public class SmartTaskExecutor
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;

        private readonly ProcessManagerService _processManager;
        private readonly SystemAutomationService _sysAutomation;
        private readonly FileManagerService _fileManager;
        private readonly WebSearchService _webSearch;
        private readonly CodeGeneratorService _codeGenerator;
        private readonly CodeCompilerService _codeCompiler;
        private readonly AiRouter _aiRouter;
        private readonly AppSettings _settings;

        public event Action<string> OnStepExecuted;
        public event Action<int, int> OnProgress;

        public SmartTaskExecutor(
            ProcessManagerService processManager,
            SystemAutomationService sysAutomation,
            FileManagerService fileManager,
            WebSearchService webSearch,
            CodeGeneratorService codeGenerator,
            CodeCompilerService codeCompiler,
            AiRouter aiRouter,
            AppSettings settings)
        {
            _processManager = processManager;
            _sysAutomation = sysAutomation;
            _fileManager = fileManager;
            _webSearch = webSearch;
            _codeGenerator = codeGenerator;
            _codeCompiler = codeCompiler;
            _aiRouter = aiRouter;
            _settings = settings;
        }

        /// <summary>
        /// 執行計畫中的所有步驟
        /// </summary>
        public string ExecutePlan(List<TaskStep> plan)
        {
            StringBuilder result = new StringBuilder();
            int total = plan.Count;

            for (int i = 0; i < plan.Count; i++)
            {
                TaskStep step = plan[i];
                ReportStep(string.Format("步驟 {0}/{1}: {2}", i + 1, total, step.Desc));
                ReportProgress(i + 1, total);

                try
                {
                    string stepResult = ExecuteStep(step);
                    if (!string.IsNullOrEmpty(stepResult))
                    {
                        result.AppendLine(string.Format("[步驟{0}] {1}", step.Step, step.Desc));
                        result.AppendLine(stepResult);
                        result.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    result.AppendLine(string.Format("[步驟{0}] {1} → 失敗: {2}",
                        step.Step, step.Desc, ex.Message));
                    result.AppendLine();
                }
            }

            if (result.Length == 0)
                result.AppendLine("所有步驟已完成。");

            return result.ToString();
        }

        /// <summary>
        /// 執行單一步驟
        /// </summary>
        private string ExecuteStep(TaskStep step)
        {
            switch (step.Type.ToLower())
            {
                case "launch_app":
                    return _processManager.LaunchApplication(step.Target ?? "");

                case "close_app":
                    return _processManager.CloseApplication(step.Target ?? "");

                case "find_and_launch":
                    return FindAndLaunch(step.Keyword ?? "");

                case "send_keys":
                    return SendKeysToActive(step.Keys ?? "");

                case "wait":
                    int waitMs = step.Ms > 0 ? step.Ms : 1000;
                    if (waitMs > 30000) waitMs = 30000; // 最多等 30 秒
                    Thread.Sleep(waitMs);
                    return null; // 靜默

                case "cmd":
                    return _sysAutomation.ExecuteCommand(step.Command ?? "");

                case "click":
                    return ClickAt(step.X, step.Y);

                case "set_clipboard":
                    return _sysAutomation.SetClipboard(step.Text ?? "");

                case "open_url":
                    return _processManager.OpenUrl(step.Url ?? "");

                case "open_file":
                    return _processManager.OpenFile(step.Path ?? "");

                case "search_file":
                    return _fileManager.SearchFiles(step.Keyword ?? "");

                case "screenshot":
                    return _sysAutomation.CaptureScreen();

                case "message":
                    return step.Text ?? "";

                case "generate_code":
                    return GenerateAndCompile(step.Description ?? step.Text ?? "");

                case "search_web":
                    return ExecuteWebSearch(step.Query ?? "");

                default:
                    return string.Format("未知的操作類型: {0}", step.Type);
            }
        }

        /// <summary>
        /// 搜尋並啟動程式
        /// </summary>
        private string FindAndLaunch(string keyword)
        {
            // 先嘗試直接啟動（別名匹配）
            string directResult = _processManager.LaunchApplication(keyword);
            if (directResult.Contains("✓"))
                return directResult;

            // 搜尋已安裝程式
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("搜尋程式: {0}", keyword));

            // 在常見路徑搜尋
            string[] searchPaths = new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                @"C:\Program Files",
                @"C:\Program Files (x86)",
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            List<string> foundFiles = new List<string>();
            foreach (string basePath in searchPaths)
            {
                if (!Directory.Exists(basePath)) continue;
                try
                {
                    SearchForExecutable(basePath, keyword, foundFiles, 3);
                }
                catch { }
                if (foundFiles.Count > 0) break;
            }

            if (foundFiles.Count > 0)
            {
                string bestMatch = foundFiles[0];
                sb.AppendLine(string.Format("找到: {0}", bestMatch));

                // 如果是 .lnk 捷徑或 .exe
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = bestMatch;
                psi.UseShellExecute = true;
                Process p = Process.Start(psi);
                if (p != null)
                    sb.AppendLine(string.Format("✓ 已啟動 (PID: {0})", p.Id));
                else
                    sb.AppendLine("✓ 已啟動");

                return sb.ToString();
            }

            sb.AppendLine(string.Format("✗ 找不到與「{0}」相關的程式", keyword));
            return sb.ToString();
        }

        private void SearchForExecutable(string path, string keyword,
            List<string> results, int depth)
        {
            if (depth <= 0 || results.Count >= 5) return;

            try
            {
                string lowerKw = keyword.ToLower();
                foreach (string file in Directory.GetFiles(path))
                {
                    string fileName = Path.GetFileName(file).ToLower();
                    string ext = Path.GetExtension(file).ToLower();

                    if ((ext == ".exe" || ext == ".lnk") &&
                        fileName.Contains(lowerKw))
                    {
                        results.Add(file);
                        if (results.Count >= 5) return;
                    }
                }

                foreach (string dir in Directory.GetDirectories(path))
                {
                    try { SearchForExecutable(dir, keyword, results, depth - 1); }
                    catch { }
                }
            }
            catch { }
        }

        /// <summary>
        /// 模擬鍵盤輸入
        /// </summary>
        private string SendKeysToActive(string keys)
        {
            try
            {
                // 轉換自定義格式為 SendKeys 格式
                keys = keys.Replace("{ENTER}", "{ENTER}")
                           .Replace("{TAB}", "{TAB}")
                           .Replace("{ESC}", "{ESC}")
                           .Replace("{DELETE}", "{DELETE}")
                           .Replace("{BACKSPACE}", "{BACKSPACE}")
                           .Replace("{UP}", "{UP}")
                           .Replace("{DOWN}", "{DOWN}")
                           .Replace("{LEFT}", "{LEFT}")
                           .Replace("{RIGHT}", "{RIGHT}")
                           .Replace("{WIN}", "^{ESC}"); // WIN 鍵用 Ctrl+Esc 模擬

                // ALT 組合鍵
                keys = Regex.Replace(keys, @"\{ALT\}\+(.)", "%$1");
                // CTRL 組合鍵
                keys = Regex.Replace(keys, @"\{CTRL\}\+(.)", "^$1");

                Thread thread = new Thread(delegate()
                {
                    Thread.Sleep(100);
                    SendKeys.SendWait(keys);
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(5000);

                return string.Format("✓ 已送出按鍵: {0}", keys);
            }
            catch (Exception ex)
            {
                return string.Format("✗ 按鍵失敗: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 滑鼠點擊
        /// </summary>
        private string ClickAt(int x, int y)
        {
            try
            {
                SetCursorPos(x, y);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
                return string.Format("✓ 已點擊 ({0}, {1})", x, y);
            }
            catch (Exception ex)
            {
                return string.Format("✗ 點擊失敗: {0}", ex.Message);
            }
        }

        /// <summary>
        /// 生成程式碼並編譯
        /// </summary>
        private string GenerateAndCompile(string description)
        {
            StringBuilder sb = new StringBuilder();

            string code = _codeGenerator.GenerateCode(description);
            if (string.IsNullOrEmpty(code))
            {
                sb.AppendLine("✗ 無法生成程式碼");
                return sb.ToString();
            }

            CompileResult result = _codeCompiler.Compile(code);
            sb.AppendLine("```csharp");
            sb.AppendLine(code);
            sb.AppendLine("```");

            if (result.Success)
            {
                sb.AppendLine(string.Format("✓ 編譯成功: {0}", result.OutputPath));
            }
            else
            {
                sb.AppendLine("✗ 編譯錯誤:");
                foreach (string err in result.Errors)
                    sb.AppendLine("  " + err);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 網路搜尋
        /// </summary>
        private string ExecuteWebSearch(string query)
        {
            List<SearchResult> results = _webSearch.SearchAll(query);
            if (results.Count == 0) return "未找到結果";

            StringBuilder sb = new StringBuilder();
            int count = Math.Min(results.Count, 5);
            for (int i = 0; i < count; i++)
            {
                sb.AppendLine(string.Format("{0}. {1}", i + 1, results[i].Title));
                if (!string.IsNullOrEmpty(results[i].Snippet))
                    sb.AppendLine("   " + results[i].Snippet.Substring(0,
                        Math.Min(results[i].Snippet.Length, 150)));
                if (!string.IsNullOrEmpty(results[i].Url))
                    sb.AppendLine("   " + results[i].Url);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private void ReportStep(string text)
        {
            if (OnStepExecuted != null) OnStepExecuted(text);
        }

        private void ReportProgress(int current, int total)
        {
            if (OnProgress != null) OnProgress(current, total);
        }
    }
}
