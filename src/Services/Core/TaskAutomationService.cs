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
    /// 任務自動化服務 - 智慧 AI 代理，自動規劃並執行使用者指令
    /// </summary>
    public class TaskAutomationService
    {
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
        private readonly SmartTaskPlanner _planner;
        private readonly SmartTaskExecutor _executor;
        private readonly AppSettings _settings;

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

            // 智慧規劃與執行
            _planner = new SmartTaskPlanner(_aiRouter);
            _executor = new SmartTaskExecutor(
                _processManager, _sysAutomation, _fileManager,
                _webSearch, _codeGenerator, _codeCompiler,
                _aiRouter, settings);

            _executor.OnStepExecuted += delegate(string s)
            {
                if (OnStepUpdate != null) OnStepUpdate(s);
            };
            _executor.OnProgress += delegate(int current, int total)
            {
                if (OnProgressUpdate != null && total > 0)
                    OnProgressUpdate((current * 100) / total);
            };
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
                // 決定是否使用智慧規劃
                if (forceType.HasValue)
                {
                    // 使用者手動選了類型，走舊邏輯
                    task.Type = forceType.Value;
                    ReportStep(task, string.Format("指定模式: {0}", task.Type));
                    ReportProgress(10);
                    ExecuteByType(task);
                }
                else
                {
                    // 自動模式：使用 AI 智慧規劃
                    task.Type = TaskType.AutoResearch; // 預設類型標記
                    ReportStep(task, "分析指令中...");
                    ReportProgress(5);

                    List<TaskStep> plan = _planner.PlanTask(query);

                    if (plan.Count == 1 && plan[0].Type == "search_web")
                    {
                        task.Type = TaskType.Search;
                        ExecuteSearch(task);
                    }
                    else if (plan.Count == 1 && plan[0].Type == "generate_code")
                    {
                        task.Type = TaskType.GenerateCode;
                        task.Query = plan[0].Description ?? plan[0].Text ?? query;
                        ExecuteGenerateCode(task);
                    }
                    else
                    {
                        // 智慧執行
                        task.Type = TaskType.AutoResearch;
                        ReportStep(task, string.Format("規劃完成，共 {0} 個步驟", plan.Count));
                        ReportProgress(15);

                        string result = _executor.ExecutePlan(plan);
                        task.Result = result;
                    }
                }

                if (task.Status != TaskStatus.Failed)
                    task.Status = TaskStatus.Completed;
            }
            catch (Exception ex)
            {
                task.Status = TaskStatus.Failed;
                task.Result = "執行錯誤: " + ex.Message;
                ReportStep(task, "錯誤: " + ex.Message);
            }

            task.CompletedAt = DateTime.Now;
            ReportProgress(100);
            return task;
        }

        /// <summary>
        /// 按指定類型執行（手動選擇模式）
        /// </summary>
        private void ExecuteByType(AgentTask task)
        {
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
                    task.Result = _reasoning.Calculate(task.Query);
                    break;
                case TaskType.SystemInfo:
                    task.Result = _sysAutomation.GetSystemInfo();
                    break;
                case TaskType.LaunchApp:
                    ExecuteLaunchApp(task);
                    break;
                case TaskType.CloseApp:
                    ExecuteCloseApp(task);
                    break;
                case TaskType.ListProcesses:
                    task.Result = _processManager.ListRunningProcesses();
                    break;
                case TaskType.FileManagement:
                    ExecuteFileManagement(task);
                    break;
                case TaskType.RunCommand:
                    ExecuteRunCommand(task);
                    break;
                case TaskType.ScreenCapture:
                    task.Result = _sysAutomation.CaptureScreen();
                    break;
                case TaskType.ClipboardOp:
                    ExecuteClipboard(task);
                    break;
                case TaskType.InstalledApps:
                    task.Result = _processManager.ListInstalledPrograms();
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
        }

        // ═══════════════════════════════════════════
        // 搜尋
        // ═══════════════════════════════════════════
        private void ExecuteSearch(AgentTask task)
        {
            ReportStep(task, "開始搜尋...");
            ReportProgress(20);

            List<SearchResult> results = _webSearch.SearchAll(task.Query);
            task.SearchResults = results;
            ReportStep(task, string.Format("搜尋完成，{0} 筆結果", results.Count));
            ReportProgress(60);

            // 嘗試 AI 增強
            string aiAnswer = null;
            if (results.Count > 0)
            {
                try
                {
                    string info = GetTopSnippets(results, 3, 500);
                    string prompt = string.Format(
                        "根據以下資訊，用繁體中文簡潔回答：「{0}」\n\n{1}", task.Query, info);
                    aiAnswer = _aiRouter.Ask(prompt);
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(aiAnswer))
            {
                task.Result = "【AI 回答】\n" + aiAnswer + "\n\n" +
                    _reasoning.SynthesizeAnswer(task.Query, results);
            }
            else
            {
                task.Result = _reasoning.SynthesizeAnswer(task.Query, results);
            }

            _reasoning.CacheKnowledge(task.Query, task.Result, "Search");
            ReportProgress(90);
        }

        // ═══════════════════════════════════════════
        // 自動研究
        // ═══════════════════════════════════════════
        private void ExecuteAutoResearch(AgentTask task)
        {
            ReportStep(task, "啟動自動研究...");
            ReportProgress(10);

            List<SearchResult> results = _webSearch.SearchAll(task.Query);
            task.SearchResults = results;
            ReportProgress(30);

            ReportStep(task, "查詢 Wikipedia...");
            KnowledgeItem wikiItem = _wikipedia.GetArticleSummary(task.Query);
            if (wikiItem != null && !string.IsNullOrEmpty(wikiItem.Content))
            {
                SearchResult wr = new SearchResult();
                wr.Title = "Wikipedia: " + wikiItem.Topic;
                wr.Snippet = wikiItem.Content;
                wr.Source = "Wikipedia";
                wr.RelevanceScore = 0.9;
                results.Insert(0, wr);
            }
            ReportProgress(50);

            // AI 分析
            string aiSummary = null;
            try
            {
                ReportStep(task, "AI 分析中...");
                string combined = GetTopSnippets(results, 3, 500);
                string prompt = string.Format(
                    "根據以下資訊，用繁體中文完整回答：「{0}」\n\n{1}", task.Query, combined);
                aiSummary = _aiRouter.Ask(prompt);
            }
            catch { }
            ReportProgress(80);

            if (!string.IsNullOrEmpty(aiSummary))
                task.Result = "【AI 分析】\n" + aiSummary + "\n\n" +
                    _reasoning.SynthesizeAnswer(task.Query, results);
            else
                task.Result = _reasoning.SynthesizeAnswer(task.Query, results);

            _reasoning.CacheKnowledge(task.Query, task.Result, "AutoResearch");
            ReportProgress(95);
        }

        // ═══════════════════════════════════════════
        // 摘要
        // ═══════════════════════════════════════════
        private void ExecuteSummarize(AgentTask task)
        {
            ReportStep(task, "開始摘要...");
            string content = "";

            if (task.Query.Contains("http"))
            {
                string url = ExtractUrl(task.Query);
                if (!string.IsNullOrEmpty(url))
                    content = _scraper.FetchMainContent(url);
            }

            if (string.IsNullOrEmpty(content))
            {
                List<SearchResult> results = _webSearch.SearchAll(task.Query);
                task.SearchResults = results;
                content = GetTopSnippets(results, 5, 2000);
            }

            task.Result = "【摘要】\n" + _reasoning.Summarize(content, 7);
        }

        // ═══════════════════════════════════════════
        // 比較
        // ═══════════════════════════════════════════
        private void ExecuteCompare(AgentTask task)
        {
            string[] topics = _reasoning.ParseCompareTopics(task.Query);
            if (topics == null || topics.Length < 2)
            {
                task.Result = "無法識別比較對象。請用格式：「比較 A vs B」";
                task.Status = TaskStatus.Failed;
                return;
            }

            List<SearchResult> a = _webSearch.SearchAll(topics[0]);
            List<SearchResult> b = _webSearch.SearchAll(topics[1]);
            task.SearchResults = new List<SearchResult>();
            task.SearchResults.AddRange(a);
            task.SearchResults.AddRange(b);
            task.Result = _reasoning.CompareTopics(topics[0], topics[1], a, b);
        }

        // ═══════════════════════════════════════════
        // 開啟/關閉程式
        // ═══════════════════════════════════════════
        private void ExecuteLaunchApp(AgentTask task)
        {
            string appName = ExtractAppName(task.Query,
                new string[] { "開啟", "打開", "啟動", "執行", "open", "launch", "start", "run" });
            task.Result = _processManager.LaunchApplication(appName);
        }

        private void ExecuteCloseApp(AgentTask task)
        {
            string appName = ExtractAppName(task.Query,
                new string[] { "關閉", "結束", "停止", "kill", "close", "stop" });
            task.Result = _processManager.CloseApplication(appName);
        }

        // ═══════════════════════════════════════════
        // 檔案管理
        // ═══════════════════════════════════════════
        private void ExecuteFileManagement(AgentTask task)
        {
            string query = task.Query;
            if (query.Contains("搜尋") || query.Contains("找") || query.Contains("search"))
            {
                string term = ExtractAppName(query,
                    new string[] { "搜尋檔案", "找檔案", "搜尋", "找", "search" });
                task.Result = _fileManager.SearchFiles(term);
            }
            else if (query.Contains("瀏覽") || query.Contains("列出"))
            {
                string path = ExtractAppName(query, new string[] { "瀏覽", "列出" });
                task.Result = _fileManager.BrowseDirectory(path);
            }
            else
            {
                string path = ExtractAppName(query,
                    new string[] { "開啟資料夾", "檔案", "資料夾", "目錄" });
                task.Result = _fileManager.OpenInExplorer(path);
            }
        }

        // ═══════════════════════════════════════════
        // CMD
        // ═══════════════════════════════════════════
        private void ExecuteRunCommand(AgentTask task)
        {
            string cmd = Regex.Replace(task.Query,
                @"^(cmd\s*[:：]?\s*|命令\s*[:：]?\s*)", "", RegexOptions.IgnoreCase).Trim();
            task.Result = _sysAutomation.ExecuteCommand(cmd);
        }

        // ═══════════════════════════════════════════
        // 剪貼簿
        // ═══════════════════════════════════════════
        private void ExecuteClipboard(AgentTask task)
        {
            string q = task.Query.ToLower();
            if (q.Contains("讀取") || q.Contains("取得") || q.Contains("get"))
                task.Result = _sysAutomation.GetClipboard();
            else if (q.Contains("設定") || q.Contains("複製") || q.Contains("set"))
            {
                string content = ExtractAppName(task.Query,
                    new string[] { "複製", "設定剪貼簿", "set clipboard" });
                task.Result = _sysAutomation.SetClipboard(content);
            }
            else
                task.Result = _sysAutomation.GetClipboard();
        }

        // ═══════════════════════════════════════════
        // 程式碼生成
        // ═══════════════════════════════════════════
                private void ExecuteGenerateCode(AgentTask task)
        {
            ReportStep(task, "生成程式碼中...");
            ReportProgress(20);

            string desc = Regex.Replace(task.Query,
                @"^(寫程式|寫代碼|產生程式|generate code|write code|幫我寫|編寫)\s*[:：]?\s*",
                "", RegexOptions.IgnoreCase).Trim();

            string code = _codeGenerator.GenerateCode(desc);
            if (string.IsNullOrEmpty(code))
            {
                task.Result = "無法生成程式碼。";
                task.Status = TaskStatus.Failed;
                return;
            }

            ReportProgress(50);

            // 自動修復迴圈（最多 3 次）
            const int MAX_FIX = 3;
            CompileResult cr = null;
            int fixCount = 0;

            for (int i = 0; i <= MAX_FIX; i++)
            {
                cr = _codeCompiler.Compile(code);
                if (cr.Success) break;

                if (i >= MAX_FIX) break;

                ReportStep(task, string.Format("編譯錯誤，AI 修復中... ({0}/{1})", i + 1, MAX_FIX));
                ReportProgress(50 + (i + 1) * 10);

                string fixedCode = _codeGenerator.FixCompileErrors(code, cr.Errors);
                if (string.IsNullOrEmpty(fixedCode) || fixedCode == code)
                    break; // AI 無法修復

                code = fixedCode;
                fixCount++;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("【生成的程式碼】");
            sb.AppendLine("```csharp");
            sb.AppendLine(code);
            sb.AppendLine("```");
            sb.AppendLine();

            if (cr.Success)
            {
                if (fixCount > 0)
                    sb.AppendLine(string.Format("✓ 經過 AI 修復 {0} 次後編譯成功！", fixCount));
                else
                    sb.AppendLine("✓ 編譯成功！");
                sb.AppendLine("輸出: " + cr.OutputPath);
            }
            else
            {
                sb.AppendLine(string.Format("✗ 編譯失敗（已嘗試 AI 修復 {0} 次）:", MAX_FIX));
                foreach (string err in cr.Errors)
                    sb.AppendLine("  " + err);
                sb.AppendLine();
                sb.AppendLine("💡 提示：輸入「修正 + 錯誤描述」讓 AI 再次嘗試修復");
            }

            task.Result = sb.ToString();
            ReportProgress(95);
        }


        // ═══════════════════════════════════════════
        // 批次
        // ═══════════════════════════════════════════
        private void ExecuteBatch(AgentTask task)
        {
            string[] lines = task.Query.Split(new char[] { '\n', '；', ';' },
                StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("═══ 批次執行 ═══\n");

            int success = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("批次")) continue;

                ReportStep(task, string.Format("批次 {0}/{1}: {2}", i + 1, lines.Length, line));
                try
                {
                    AgentTask sub = ExecuteTask(line, null);
                    sb.AppendLine(string.Format("【{0}】{1}", i + 1, line));
                    sb.AppendLine(sub.Result ?? "(無結果)");
                    sb.AppendLine();
                    success++;
                }
                catch (Exception ex)
                {
                    sb.AppendLine(string.Format("【{0}】失敗: {1}", i + 1, ex.Message));
                }
            }

            sb.AppendLine(string.Format("完成 {0}/{1}", success, lines.Length));
            task.Result = sb.ToString();
        }

        // ═══════════════════════════════════════════
        // 公開屬性
        // ═══════════════════════════════════════════
        public AIReasoningEngine ReasoningEngine { get { return _reasoning; } }
        public CodeCompilerService CodeCompiler { get { return _codeCompiler; } }
        public CodeGeneratorService CodeGenerator { get { return _codeGenerator; } }

        // ═══════════════════════════════════════════
        // 輔助
        // ═══════════════════════════════════════════
        private void ReportStep(AgentTask task, string step)
        {
            task.AddStep(step);
            if (OnStepUpdate != null) OnStepUpdate(step);
        }

        private void ReportProgress(int percent)
        {
            if (OnProgressUpdate != null) OnProgressUpdate(Math.Min(percent, 100));
        }

        private string ExtractAppName(string query, string[] prefixes)
        {
            string result = query;
            foreach (string p in prefixes)
            {
                int idx = result.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) result = result.Substring(idx + p.Length);
            }
            return result.Trim().Trim(':', '：', ' ');
        }

        private string ExtractUrl(string text)
        {
            Match m = Regex.Match(text, @"https?://[^\s]+");
            return m.Success ? m.Value : null;
        }

        private string GetTopSnippets(List<SearchResult> results, int count, int maxLen)
        {
            StringBuilder sb = new StringBuilder();
            int added = 0;
            foreach (SearchResult r in results)
            {
                if (added >= count || sb.Length >= maxLen) break;
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
