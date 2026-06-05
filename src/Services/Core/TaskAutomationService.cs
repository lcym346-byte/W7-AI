using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AIAgentTool.Models;
using AIAgentTool.Services.AI;
using AIAgentTool.Services.Search;
using AIAgentTool.Services.System;
using AIAgentTool.Services.CodeGen;



namespace AIAgentTool.Services.Core
{
    /// <summary>
    /// 任務自動化服務 - 協調所有子服務完成使用者指令
    /// </summary>
    public class TaskAutomationService
    {
        // ═══════════════════════════════════════════
        // 依賴注入的服務
        // ═══════════════════════════════════════════
        private readonly AIReasoningEngine _reasoning;
        private readonly AiRouter _aiRouter;
        private readonly WebSearchService _webSearch;
        private readonly WikipediaService _wikipedia;
        private readonly WebScraperService _scraper;
        private readonly ProcessManagerService _processManager;
        private readonly SystemAutomationService _sysAutomation;
        private readonly FileManagerService _fileManager;
        private readonly CodeGeneratorService _codeGenerator;
        private readonly CodeCompilerService _codeCompiler;
        private readonly AppSettings _settings;

        // ═══════════════════════════════════════════
        // 事件
        // ═══════════════════════════════════════════
        public event Action<string> OnStepUpdate;
        public event Action<int> OnProgressUpdate;

        public TaskAutomationService(AppSettings settings)
        {
            _settings = settings;
            _reasoning = new AIReasoningEngine();
            _aiRouter = new AiRouter(settings);
            _webSearch = new WebSearchService();
            _wikipedia = new WikipediaService(_webSearch);
            _scraper = new WebScraperService();
            _processManager = new ProcessManagerService();
            _sysAutomation = new SystemAutomationService(settings);
            _fileManager = new FileManagerService();
            _codeGenerator = new CodeGeneratorService(_aiRouter);
            _codeCompiler = new CodeCompilerService();
        }

        // ═══════════════════════════════════════════
        // 主入口：執行任務
        // ═══════════════════════════════════════════
        public AgentTask ExecuteTask(string query, TaskType? forceType = null)
        {
            AgentTask task = new AgentTask();
            task.Query = query;
            task.Status = TaskStatus.Running;

            try
            {
                // 1. 意圖分析
                task.Type = forceType.HasValue ? forceType.Value : _reasoning.AnalyzeIntent(query);
                ReportStep(task, string.Format("意圖分析完成：{0}", task.Type));
                ReportProgress(10);

                // 2. 檢查快取
                KnowledgeItem cached = _reasoning.GetCachedKnowledge(query);
                if (cached != null && task.Type == TaskType.Search)
                {
                    task.Result = "[快取命中]\n" + cached.Content;
                    task.Status = TaskStatus.Completed;
                    ReportStep(task, "使用快取結果");
                    ReportProgress(100);
                    return task;
                }

                // 3. 依任務類型分派
                switch (task.Type)
                {
                    case TaskType.Search:
                        ExecuteSearch(task);
                        break;
                    case TaskType.AutoResearch:
                        ExecuteAutoResearch(task);
                        break;
                    case TaskType.Summarize:
                        ExecuteSummarize(task);
                        break;
                    case TaskType.Compare:
                        ExecuteCompare(task);
                        break;
                    case TaskType.Calculate:
                        ExecuteCalculate(task);
                        break;
                    case TaskType.SystemInfo:
                        ExecuteSystemInfo(task);
                        break;
                    case TaskType.LaunchApp:
                        ExecuteLaunchApp(task);
                        break;
                    case TaskType.CloseApp:
                        ExecuteCloseApp(task);
                        break;
                    case TaskType.ListProcesses:
                        ExecuteListProcesses(task);
                        break;
                    case TaskType.FileManagement:
                        ExecuteFileManagement(task);
                        break;
                    case TaskType.RunCommand:
                        ExecuteRunCommand(task);
                        break;
                    case TaskType.ScreenCapture:
                        ExecuteScreenCapture(task);
                        break;
                    case TaskType.ClipboardOp:
                        ExecuteClipboard(task);
                        break;
                    case TaskType.InstalledApps:
                        ExecuteInstalledApps(task);
                        break;
                    case TaskType.GenerateCode:
                        ExecuteGenerateCode(task);
                        break;
                    case TaskType.BatchOperation:
                        ExecuteBatch(task);
                        break;
                    default:
                        ExecuteAutoResearch(task);
                        break;
                }

                if (task.Status != TaskStatus.Failed)
                    task.Status = TaskStatus.Completed;
            }
            catch (Exception ex)
            {
                task.Status = TaskStatus.Failed;
                task.Result = "執行錯誤：" + ex.Message;
                ReportStep(task, "錯誤：" + ex.Message);
            }

            task.CompletedAt = DateTime.Now;
            ReportProgress(100);
            return task;
        }

        // ═══════════════════════════════════════════
        // 搜尋
        // ═══════════════════════════════════════════
        private void ExecuteSearch(AgentTask task)
        {
            ReportStep(task, "開始多源搜尋...");
            ReportProgress(20);

            List<SearchResult> results = _webSearch.SearchAll(task.Query);
            task.SearchResults = results;
            ReportStep(task, string.Format("搜尋完成，取得 {0} 筆結果", results.Count));
            ReportProgress(60);

            task.Result = _reasoning.SynthesizeAnswer(task.Query, results);
            _reasoning.CacheKnowledge(task.Query, task.Result, "Search");
            ReportProgress(90);
        }

        // ═══════════════════════════════════════════
        // 自動研究
        // ═══════════════════════════════════════════
        private void ExecuteAutoResearch(AgentTask task)
        {
            ReportStep(task, "啟動自動研究流程...");
            ReportProgress(10);

            // 多源搜尋
            List<SearchResult> results = _webSearch.SearchAll(task.Query);
            task.SearchResults = results;
            ReportStep(task, string.Format("多源搜尋完成：{0} 筆結果", results.Count));
            ReportProgress(30);

            // Wikipedia 深入查詢
            ReportStep(task, "查詢 Wikipedia...");
            KnowledgeItem wikiItem = _wikipedia.GetArticleSummary(task.Query);
            if (wikiItem != null && !string.IsNullOrEmpty(wikiItem.Content))
            {
                SearchResult wikiResult = new SearchResult();
                wikiResult.Title = "Wikipedia: " + wikiItem.Topic;
                wikiResult.Snippet = wikiItem.Content;
                wikiResult.Source = "Wikipedia";
                wikiResult.RelevanceScore = 0.9;
                results.Insert(0, wikiResult);
                ReportStep(task, "Wikipedia 資料取得成功");
            }
            ReportProgress(50);

            // 深入閱讀前2筆
            ReportStep(task, "深入閱讀排名前列網頁...");
            int deepReadCount = 0;
            for (int i = 0; i < Math.Min(results.Count, 2); i++)
            {
                if (!string.IsNullOrEmpty(results[i].Url) && results[i].Source != "Wikipedia")
                {
                    try
                    {
                        string content = _scraper.FetchMainContent(results[i].Url);
                        if (!string.IsNullOrEmpty(content) && content.Length > results[i].Snippet.Length)
                        {
                            results[i].Snippet = content.Length > 1000
                                ? content.Substring(0, 1000) : content;
                            deepReadCount++;
                        }
                    }
                    catch { /* 忽略深讀失敗 */ }
                }
            }
            ReportStep(task, string.Format("深入閱讀完成（成功 {0} 頁）", deepReadCount));
            ReportProgress(70);

            // AI 增強（可選）
            string aiSummary = null;
            try
            {
                ReportStep(task, "嘗試 AI 增強分析...");
                string combinedInfo = GetTopSnippets(results, 3, 500);
                string aiPrompt = string.Format(
                    "根據以下資訊，用繁體中文回答問題：「{0}」\n\n參考資料：\n{1}\n\n請提供完整且有條理的回答。",
                    task.Query, combinedInfo);
                aiSummary = _aiRouter.Ask(aiPrompt);
            }
            catch
            {
                ReportStep(task, "AI 增強不可用，使用本地合成");
            }
            ReportProgress(85);

            // 合成最終答案
            if (!string.IsNullOrEmpty(aiSummary))
            {
                task.Result = "【AI 分析結果】\n" + aiSummary + "\n\n" +
                              "───────────────────\n" +
                              _reasoning.SynthesizeAnswer(task.Query, results);
            }
            else
            {
                task.Result = _reasoning.SynthesizeAnswer(task.Query, results);
            }

            _reasoning.CacheKnowledge(task.Query, task.Result, "AutoResearch");
            ReportProgress(95);
        }

        // ═══════════════════════════════════════════
        // 摘要
        // ═══════════════════════════════════════════
        private void ExecuteSummarize(AgentTask task)
        {
            ReportStep(task, "開始摘要...");
            ReportProgress(20);

            string content = "";

            // 判斷是否為 URL
            if (task.Query.StartsWith("http://") || task.Query.StartsWith("https://") ||
                task.Query.Contains("摘要 http"))
            {
                string url = ExtractUrl(task.Query);
                if (!string.IsNullOrEmpty(url))
                {
                    ReportStep(task, "擷取網頁內容：" + url);
                    content = _scraper.FetchMainContent(url);
                }
            }

            if (string.IsNullOrEmpty(content))
            {
                // 搜尋後摘要
                List<SearchResult> results = _webSearch.SearchAll(task.Query);
                task.SearchResults = results;
                content = GetTopSnippets(results, 5, 2000);
            }

            ReportProgress(60);
            task.Result = "【摘要】\n" + _reasoning.Summarize(content, 7);
            ReportProgress(90);
        }

        // ═══════════════════════════════════════════
        // 比較
        // ═══════════════════════════════════════════
        private void ExecuteCompare(AgentTask task)
        {
            ReportStep(task, "解析比較對象...");
            string[] topics = _reasoning.ParseCompareTopics(task.Query);

            if (topics == null || topics.Length < 2)
            {
                task.Result = "無法識別比較對象。請使用格式：「比較 A vs B」或「A 和 B 比較」";
                task.Status = TaskStatus.Failed;
                return;
            }

            ReportStep(task, string.Format("比較：{0} vs {1}", topics[0], topics[1]));
            ReportProgress(20);

            List<SearchResult> resultsA = _webSearch.SearchAll(topics[0]);
            ReportProgress(45);
            List<SearchResult> resultsB = _webSearch.SearchAll(topics[1]);
            ReportProgress(70);

            task.SearchResults = new List<SearchResult>();
            task.SearchResults.AddRange(resultsA);
            task.SearchResults.AddRange(resultsB);

            task.Result = _reasoning.CompareTopics(topics[0], topics[1], resultsA, resultsB);
            ReportProgress(90);
        }

        // ═══════════════════════════════════════════
        // 計算
        // ═══════════════════════════════════════════
        private void ExecuteCalculate(AgentTask task)
        {
            task.Result = _reasoning.Calculate(task.Query);
        }

        // ═══════════════════════════════════════════
        // 系統資訊
        // ═══════════════════════════════════════════
        private void ExecuteSystemInfo(AgentTask task)
        {
            task.Result = _sysAutomation.GetSystemInfo();
        }

        // ═══════════════════════════════════════════
        // 開啟程式
        // ═══════════════════════════════════════════
        private void ExecuteLaunchApp(AgentTask task)
        {
            ReportStep(task, "解析應用程式名稱...");
            string appName = ExtractAppName(task.Query,
                new string[] { "開啟", "打開", "啟動", "執行", "open", "launch", "start", "run" });
            task.Result = _processManager.LaunchApplication(appName);
        }

        // ═══════════════════════════════════════════
        // 關閉程式
        // ═══════════════════════════════════════════
        private void ExecuteCloseApp(AgentTask task)
        {
            string appName = ExtractAppName(task.Query,
                new string[] { "關閉", "結束", "停止", "kill", "close", "stop" });
            task.Result = _processManager.CloseApplication(appName);
        }

        // ═══════════════════════════════════════════
        // 列出處理程序
        // ═══════════════════════════════════════════
        private void ExecuteListProcesses(AgentTask task)
        {
            task.Result = _processManager.ListRunningProcesses();
        }

        // ═══════════════════════════════════════════
        // 檔案管理
        // ═══════════════════════════════════════════
        private void ExecuteFileManagement(AgentTask task)
        {
            ReportStep(task, "解析檔案操作...");

            string query = task.Query;

            // 搜尋檔案
            if (query.Contains("搜尋") || query.Contains("找") || query.Contains("search"))
            {
                string searchTerm = ExtractAppName(query,
                    new string[] { "搜尋檔案", "找檔案", "搜尋", "找", "search file", "search" });
                task.Result = _fileManager.SearchFiles(searchTerm);
            }
            // 瀏覽目錄
            else if (query.Contains("瀏覽") || query.Contains("列出") || query.Contains("ls") || query.Contains("dir"))
            {
                string path = ExtractAppName(query,
                    new string[] { "瀏覽", "列出", "ls", "dir" });
                task.Result = _fileManager.BrowseDirectory(path);
            }
            // 開啟資料夾
            else
            {
                string path = ExtractAppName(query,
                    new string[] { "開啟資料夾", "開啟目錄", "打開資料夾", "檔案", "文件", "資料夾", "目錄" });
                task.Result = _fileManager.OpenInExplorer(path);
            }
        }

        // ═══════════════════════════════════════════
        // 執行 CMD 命令
        // ═══════════════════════════════════════════
        private void ExecuteRunCommand(AgentTask task)
        {
            string cmd = task.Query;
            cmd = Regex.Replace(cmd,
                @"^(cmd\s*[:：]?\s*|命令\s*[:：]?\s*|command\s*[:：]?\s*)", "",
                RegexOptions.IgnoreCase).Trim();

            task.Result = _sysAutomation.ExecuteCommand(cmd);
        }

        // ═══════════════════════════════════════════
        // 截圖
        // ═══════════════════════════════════════════
        private void ExecuteScreenCapture(AgentTask task)
        {
            task.Result = _sysAutomation.CaptureScreen();
        }

        // ═══════════════════════════════════════════
        // 剪貼簿
        // ═══════════════════════════════════════════
        private void ExecuteClipboard(AgentTask task)
        {
            string query = task.Query.ToLower();
            if (query.Contains("讀取") || query.Contains("取得") || query.Contains("get") || query.Contains("read"))
            {
                task.Result = _sysAutomation.GetClipboard();
            }
            else if (query.Contains("設定") || query.Contains("複製") || query.Contains("set") || query.Contains("copy"))
            {
                string content = ExtractAppName(task.Query,
                    new string[] { "複製", "設定剪貼簿", "set clipboard", "copy" });
                task.Result = _sysAutomation.SetClipboard(content);
            }
            else
            {
                task.Result = _sysAutomation.GetClipboard();
            }
        }

        // ═══════════════════════════════════════════
        // 已安裝程式
        // ═══════════════════════════════════════════
        private void ExecuteInstalledApps(AgentTask task)
        {
            task.Result = _processManager.ListInstalledPrograms();
        }

        // ═══════════════════════════════════════════
        // 程式碼生成
        // ═══════════════════════════════════════════
        private void ExecuteGenerateCode(AgentTask task)
        {
            ReportStep(task, "分析程式碼需求...");
            ReportProgress(15);

            string description = task.Query;
            description = Regex.Replace(description,
                @"^(寫程式|寫代碼|產生程式|生成代碼|generate code|write code|幫我寫|編寫)\s*[:：]?\s*",
                "", RegexOptions.IgnoreCase).Trim();

            ReportStep(task, "生成程式碼中...");
            ReportProgress(30);

            string code = _codeGenerator.GenerateCode(description);

            if (string.IsNullOrEmpty(code))
            {
                task.Result = "無法生成程式碼，請嘗試更詳細描述需求。";
                task.Status = TaskStatus.Failed;
                return;
            }

            ReportStep(task, "程式碼生成完成，嘗試編譯驗證...");
            ReportProgress(60);

            // 嘗試編譯
CompileResult compileResult = _codeCompiler.Compile(code);
            ReportProgress(80);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("【生成的程式碼】");
            sb.AppendLine("```csharp");
            sb.AppendLine(code);
            sb.AppendLine("```");
            sb.AppendLine();

            if (compileResult.Success)
            {
                sb.AppendLine("✓ 編譯成功！");
                sb.AppendLine("輸出路徑：" + compileResult.OutputPath);
                sb.AppendLine("檔案大小：" + compileResult.FileSize);
                ReportStep(task, "編譯驗證成功");
            }
            else
            {
                sb.AppendLine("✗ 編譯有錯誤：");
                foreach (string err in compileResult.Errors)
                    sb.AppendLine("  " + err);

                if (compileResult.Warnings.Count > 0)
                {
                    sb.AppendLine("警告：");
                    foreach (string warn in compileResult.Warnings)
                        sb.AppendLine("  " + warn);
                }

                // 嘗試 AI 修復
                ReportStep(task, "嘗試 AI 修復編譯錯誤...");
                string fixedCode = _codeGenerator.FixCompileErrors(code, compileResult.Errors);
                if (!string.IsNullOrEmpty(fixedCode) && fixedCode != code)
                {
CompileResult retryResult = _codeCompiler.Compile(fixedCode);
                    if (retryResult.Success)
                    {
                        sb.AppendLine();
                        sb.AppendLine("【修復後的程式碼】");
                        sb.AppendLine("```csharp");
                        sb.AppendLine(fixedCode);
                        sb.AppendLine("```");
                        sb.AppendLine("✓ 修復後編譯成功！");
                        sb.AppendLine("輸出路徑：" + retryResult.OutputPath);
                        ReportStep(task, "AI 修復成功");
                    }
                    else
                    {
                        sb.AppendLine("\n修復嘗試失敗，請手動調整程式碼。");
                    }
                }
            }

            task.Result = sb.ToString();
            ReportProgress(95);
        }

        // ═══════════════════════════════════════════
        // 批次操作
        // ═══════════════════════════════════════════
        private void ExecuteBatch(AgentTask task)
        {
            ReportStep(task, "解析批次命令...");

            string[] lines = task.Query.Split(new char[] { '\n', '；', ';' },
                StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("═══ 批次執行結果 ═══");
            sb.AppendLine();

            int total = lines.Length;
            int success = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("批次") || line.StartsWith("batch"))
                    continue;

                ReportStep(task, string.Format("執行第 {0}/{1} 項：{2}", i + 1, total, line));
                ReportProgress((i * 100) / total);

                try
                {
                    AgentTask subTask = ExecuteTask(line);
                    sb.AppendLine(string.Format("【{0}】{1}", i + 1, line));
                    sb.AppendLine(subTask.Result ?? "(無結果)");
                    sb.AppendLine();
                    success++;
                }
                catch (Exception ex)
                {
                    sb.AppendLine(string.Format("【{0}】{1} → 失敗：{2}", i + 1, line, ex.Message));
                    sb.AppendLine();
                }
            }

            sb.AppendLine(string.Format("───────────────────\n完成 {0}/{1} 項任務", success, total));
            task.Result = sb.ToString();
        }

        // ═══════════════════════════════════════════
        // 公開屬性 - 提供給 UI 使用
        // ═══════════════════════════════════════════
        public AIReasoningEngine ReasoningEngine { get { return _reasoning; } }
        public CodeCompilerService CodeCompiler { get { return _codeCompiler; } }
        public CodeGeneratorService CodeGenerator { get { return _codeGenerator; } }

        // ═══════════════════════════════════════════
        // 輔助方法
        // ═══════════════════════════════════════════
        private void ReportStep(AgentTask task, string step)
        {
            task.AddStep(step);
            if (OnStepUpdate != null)
                OnStepUpdate(step);
        }

        private void ReportProgress(int percent)
        {
            if (OnProgressUpdate != null)
                OnProgressUpdate(Math.Min(percent, 100));
        }

        private string ExtractAppName(string query, string[] prefixes)
        {
            string result = query;
            foreach (string p in prefixes)
            {
                int idx = result.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    result = result.Substring(idx + p.Length);
            }
            return result.Trim().Trim(':', '：', ' ');
        }

        private string ExtractUrl(string text)
        {
            Match m =
                Regex.Match(text, @"https?://[^\s]+");
            return m.Success ? m.Value : null;
        }

        private string GetTopSnippets(List<SearchResult> results, int count, int maxTotalLength)
        {
            StringBuilder sb = new StringBuilder();
            int added = 0;
            foreach (SearchResult r in results)
            {
                if (added >= count || sb.Length >= maxTotalLength) break;
                if (!string.IsNullOrEmpty(r.Snippet))
                {
                    sb.AppendLine(r.Snippet);
                    added++;
                }
            }
            return sb.ToString();
        }
    }
}
