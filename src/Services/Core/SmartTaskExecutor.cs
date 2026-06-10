using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
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
    public class SmartTaskExecutor
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        private const int SW_RESTORE = 9;

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
                    if (waitMs > 30000) waitMs = 30000;
                    Thread.Sleep(waitMs);
                    return null;

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

                // ═══════════════════════════════════════════
                // 新增：媒體生成
                // ═══════════════════════════════════════════
                case "generate_image":
                    return ExecuteGenerateImage(step.Text ?? "");

                case "generate_video":
                    return ExecuteGenerateVideo(step.Text ?? "");

                case "text_to_speech":
                    return ExecuteTextToSpeech(step.Text ?? "");

                // ═══════════════════════════════════════════
                // 新增：知識庫
                // ═══════════════════════════════════════════
                case "knowledge_search":
                    return ExecuteKnowledgeSearch(step.Text ?? "");

                case "knowledge_add":
                    return ExecuteKnowledgeAdd(step.Text ?? step.Path ?? "");

                // ═══════════════════════════════════════════
                // 新增：桌面自動化
                // ═══════════════════════════════════════════
                case "list_windows":
                    return ListAllWindows();

                case "focus_window":
                    return FocusWindow(step.Text ?? step.Target ?? "");

                default:
                    return string.Format("未知的操作類型: {0}", step.Type);
            }
        }

        // ═══════════════════════════════════════════
        // 圖片生成（LocalAI）
        // ═══════════════════════════════════════════
        private string ExecuteGenerateImage(string prompt)
        {
            try
            {
                string url = (_settings.LocalAiUrl ?? "http://localhost:8080").TrimEnd('/') + "/v1/images/generations";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = 120000;

                string json = "{\"prompt\":\"" + EscapeJson(prompt) + "\",\"size\":\"512x512\",\"n\":1}";
                byte[] data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;

                using (Stream reqStream = request.GetRequestStream())
                    reqStream.Write(data, 0, data.Length);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    string responseText = reader.ReadToEnd();

                    // 嘗試解析 b64_json 或 url
                    string imageUrl = ExtractJsonValueSimple(responseText, "url");
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        string savePath = Path.Combine(
                            _settings.DefaultSavePath,
                            "ai_image_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");

                        using (WebClient wc = new WebClient())
                            wc.DownloadFile(imageUrl, savePath);

                        return string.Format("✓ 圖片已生成並儲存: {0}", savePath);
                    }

                    string b64 = ExtractJsonValueSimple(responseText, "b64_json");
                    if (!string.IsNullOrEmpty(b64))
                    {
                        string savePath = Path.Combine(
                            _settings.DefaultSavePath,
                            "ai_image_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");

                        byte[] imageBytes = Convert.FromBase64String(b64);
                        File.WriteAllBytes(savePath, imageBytes);
                        return string.Format("✓ 圖片已生成並儲存: {0}", savePath);
                    }

                    return "✗ 圖片生成回傳格式無法解析";
                }
            }
            catch (WebException ex)
            {
                return string.Format("✗ 圖片生成失敗 (LocalAI 可能未啟動): {0}\n提示: 請確認 LocalAI 在 {1} 運行中",
                    ex.Message, _settings.LocalAiUrl ?? "http://localhost:8080");
            }
            catch (Exception ex)
            {
                return string.Format("✗ 圖片生成失敗: {0}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════
        // 影片生成（MoneyPrinterTurbo API）
        // ═══════════════════════════════════════════
        private string ExecuteGenerateVideo(string topic)
        {
            try
            {
                string baseUrl = (_settings.VideoApiUrl ?? "http://localhost:8501").TrimEnd('/');
                string url = baseUrl + "/api/v1/videos";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = 300000; // 5 分鐘

                string json = "{\"video_subject\":\"" + EscapeJson(topic) + "\"," +
                              "\"video_language\":\"zh-TW\"," +
                              "\"video_terms\":\"\"}";
                byte[] data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;

                using (Stream reqStream = request.GetRequestStream())
                    reqStream.Write(data, 0, data.Length);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    string responseText = reader.ReadToEnd();
                    string taskId = ExtractJsonValueSimple(responseText, "task_id");

                    if (!string.IsNullOrEmpty(taskId))
                        return string.Format("✓ 影片生成任務已提交！\n任務 ID: {0}\n影片完成後將儲存在 {1}",
                            taskId, _settings.DefaultSavePath);

                    string videoPath = ExtractJsonValueSimple(responseText, "video_path");
                    if (!string.IsNullOrEmpty(videoPath))
                        return string.Format("✓ 影片已生成: {0}", videoPath);

                    return "✓ 影片生成請求已送出，請稍等處理完成。";
                }
            }
            catch (WebException ex)
            {
                return string.Format("✗ 影片生成失敗 (MoneyPrinterTurbo 可能未啟動): {0}\n提示: 請確認服務在 {1} 運行中",
                    ex.Message, _settings.VideoApiUrl ?? "http://localhost:8501");
            }
            catch (Exception ex)
            {
                return string.Format("✗ 影片生成失敗: {0}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════
        // 語音合成（LocalAI TTS）
        // ═══════════════════════════════════════════
        private string ExecuteTextToSpeech(string input)
        {
            try
            {
                // 從指令中提取要朗讀的文字
                string textToSpeak = input;
                string[] prefixes = new string[] { "朗讀", "唸出", "唸", "語音", "說出", "tts" };
                foreach (string p in prefixes)
                {
                    int idx = textToSpeak.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0) textToSpeak = textToSpeak.Substring(idx + p.Length);
                }
                textToSpeak = textToSpeak.Trim().Trim(':', '：', ' ');
                if (string.IsNullOrEmpty(textToSpeak)) textToSpeak = input;

                string url = (_settings.LocalAiUrl ?? "http://localhost:8080").TrimEnd('/') + "/v1/audio/speech";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = 60000;

                string json = "{\"input\":\"" + EscapeJson(textToSpeak) + "\"," +
                              "\"model\":\"tts-1\"," +
                              "\"voice\":\"alloy\"}";
                byte[] data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;

                using (Stream reqStream = request.GetRequestStream())
                    reqStream.Write(data, 0, data.Length);

                string savePath = Path.Combine(
                    _settings.DefaultSavePath,
                    "tts_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mp3");

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (FileStream fs = new FileStream(savePath, FileMode.Create))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        fs.Write(buffer, 0, bytesRead);
                }

                // 嘗試播放
                try
                {
                    Process.Start(savePath);
                }
                catch { }

                return string.Format("✓ 語音已生成並播放: {0}\n文字: {1}", savePath, textToSpeak);
            }
            catch (WebException ex)
            {
                return string.Format("✗ 語音合成失敗 (LocalAI 可能未啟動): {0}\n提示: 請確認 LocalAI 在 {1} 運行中",
                    ex.Message, _settings.LocalAiUrl ?? "http://localhost:8080");
            }
            catch (Exception ex)
            {
                return string.Format("✗ 語音合成失敗: {0}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════
        // 知識庫搜尋（TurboVec）
        // ═══════════════════════════════════════════
        private string ExecuteKnowledgeSearch(string input)
        {
            try
            {
                string query = input;
                string[] prefixes = new string[] { "知識庫", "搜尋文件", "查資料", "search knowledge" };
                foreach (string p in prefixes)
                {
                    int idx = query.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0) query = query.Substring(idx + p.Length);
                }
                query = query.Trim().Trim(':', '：', ' ');
                if (string.IsNullOrEmpty(query)) query = input;

                string url = (_settings.TurboVecUrl ?? "http://localhost:5050").TrimEnd('/') + "/search";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = 30000;

                string json = "{\"query\":\"" + EscapeJson(query) + "\",\"top_k\":5}";
                byte[] data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;

                using (Stream reqStream = request.GetRequestStream())
                    reqStream.Write(data, 0, data.Length);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    string responseText = reader.ReadToEnd();
                    return "═══ 知識庫搜尋結果 ═══\n查詢: " + query + "\n\n" + responseText;
                }
            }
            catch (WebException ex)
            {
                return string.Format("✗ 知識庫搜尋失敗 (TurboVec 可能未啟動): {0}\n提示: 請確認服務在 {1} 運行中",
                    ex.Message, _settings.TurboVecUrl ?? "http://localhost:5050");
            }
            catch (Exception ex)
            {
                return string.Format("✗ 知識庫搜尋失敗: {0}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════
        // 知識庫新增文件
        // ═══════════════════════════════════════════
        private string ExecuteKnowledgeAdd(string input)
        {
            try
            {
                string filePath = input;
                string[] prefixes = new string[] { "加入知識庫", "匯入文件", "add to knowledge" };
                foreach (string p in prefixes)
                {
                    int idx = filePath.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0) filePath = filePath.Substring(idx + p.Length);
                }
                filePath = filePath.Trim().Trim(':', '：', ' ');

                if (!File.Exists(filePath))
                    return string.Format("✗ 檔案不存在: {0}\n請提供完整路徑，例如: 加入知識庫 C:\\Documents\\note.txt", filePath);

                string content = File.ReadAllText(filePath, Encoding.UTF8);
                if (string.IsNullOrEmpty(content))
                    return "✗ 檔案內容為空";

                string url = (_settings.TurboVecUrl ?? "http://localhost:5050").TrimEnd('/') + "/add";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                request.Timeout = 60000;

                string fileName = Path.GetFileName(filePath);
                string json = "{\"text\":\"" + EscapeJson(content) + "\"," +
                              "\"metadata\":{\"source\":\"" + EscapeJson(fileName) + "\"}}";
                byte[] data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;

                using (Stream reqStream = request.GetRequestStream())
                    reqStream.Write(data, 0, data.Length);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    reader.ReadToEnd();
                    return string.Format("✓ 已將檔案加入知識庫: {0} ({1} 字元)", fileName, content.Length);
                }
            }
            catch (WebException ex)
            {
                return string.Format("✗ 加入知識庫失敗 (TurboVec 可能未啟動): {0}", ex.Message);
            }
            catch (Exception ex)
            {
                return string.Format("✗ 加入知識庫失敗: {0}", ex.Message);
            }
        }

        // ═══════════════════════════════════════════
        // 列出所有視窗
        // ═══════════════════════════════════════════
        private string ListAllWindows()
        {
            List<string> windows = new List<string>();
            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd)) return true;
                StringBuilder title = new StringBuilder(256);
                GetWindowText(hWnd, title, 256);
                string t = title.ToString().Trim();
                if (!string.IsNullOrEmpty(t))
                    windows.Add(t);
                return true;
            }, IntPtr.Zero);

            if (windows.Count == 0)
                return "找不到任何可見視窗";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("═══ 目前開啟的視窗 ═══");
            for (int i = 0; i < windows.Count; i++)
                sb.AppendLine(string.Format("  {0}. {1}", i + 1, windows[i]));
            sb.AppendLine(string.Format("\n共 {0} 個視窗", windows.Count));
            return sb.ToString();
        }

        // ═══════════════════════════════════════════
        // 切換視窗焦點
        // ═══════════════════════════════════════════
        private string FocusWindow(string input)
        {
            string keyword = input;
            string[] prefixes = new string[] { "切換到", "打開視窗", "focus window", "switch to" };
            foreach (string p in prefixes)
            {
                int idx = keyword.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) keyword = keyword.Substring(idx + p.Length);
            }
            keyword = keyword.Trim();

            if (string.IsNullOrEmpty(keyword))
                return "✗ 請指定視窗名稱";

            IntPtr found = IntPtr.Zero;
            string foundTitle = "";

            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd)) return true;
                StringBuilder title = new StringBuilder(256);
                GetWindowText(hWnd, title, 256);
                string t = title.ToString();
                if (t.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = hWnd;
                    foundTitle = t;
                    return false; // stop
                }
                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero)
            {
                ShowWindow(found, SW_RESTORE);
                SetForegroundWindow(found);
                return string.Format("✓ 已切換到視窗: {0}", foundTitle);
            }

            return string.Format("✗ 找不到包含「{0}」的視窗", keyword);
        }

        // ═══════════════════════════════════════════
        // 原有方法
        // ═══════════════════════════════════════════
        private string FindAndLaunch(string keyword)
        {
            string directResult = _processManager.LaunchApplication(keyword);
            if (directResult.Contains("✓"))
                return directResult;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("搜尋程式: {0}", keyword));

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
                try { SearchForExecutable(basePath, keyword, foundFiles, 3); }
                catch { }
                if (foundFiles.Count > 0) break;
            }

            if (foundFiles.Count > 0)
            {
                string bestMatch = foundFiles[0];
                sb.AppendLine(string.Format("找到: {0}", bestMatch));

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

                    if ((ext == ".exe" || ext == ".lnk") && fileName.Contains(lowerKw))
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

        private string SendKeysToActive(string keys)
        {
            try
            {
                keys = keys.Replace("{ENTER}", "{ENTER}")
                           .Replace("{TAB}", "{TAB}")
                           .Replace("{ESC}", "{ESC}")
                           .Replace("{DELETE}", "{DELETE}")
                           .Replace("{BACKSPACE}", "{BACKSPACE}")
                           .Replace("{UP}", "{UP}")
                           .Replace("{DOWN}", "{DOWN}")
                           .Replace("{LEFT}", "{LEFT}")
                           .Replace("{RIGHT}", "{RIGHT}")
                           .Replace("{WIN}", "^{ESC}");

                keys = Regex.Replace(keys, @"\{ALT\}\+(.)", "%$1");
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

        // ═══════════════════════════════════════════
        // 工具方法
        // ═══════════════════════════════════════════
        private string ExtractJsonValueSimple(string json, string key)
        {
            string pattern = string.Format("\"{0}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"",
                Regex.Escape(key));
            Match m = Regex.Match(json, pattern);
            if (m.Success)
                return m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
            return null;
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
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
