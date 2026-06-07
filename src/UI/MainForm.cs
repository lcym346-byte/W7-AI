using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using AIAgentTool.Models;
using AIAgentTool.Services.Core;
using AIAgentTool.Services.CodeGen;
using AIAgentTool.Utils;


namespace AIAgentTool
{
    /// <summary>
    /// 主視窗 - UI 邏輯與事件處理
    /// </summary>
    public partial class MainForm : Form
    {
        // ═══════════════════════════════════════════
        // 核心服務
        // ═══════════════════════════════════════════
        private AppSettings _settings;
        private TaskAutomationService _automationService;
        private BackgroundTaskRunner _taskRunner;
        private List<AgentTask> _taskHistory;
        private bool _isExecuting;
        private ContextMenuStrip _historyMenu;

        private static readonly string HistoryFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "history.json");

        public MainForm()
        {
            InitializeComponent();
            InitializeServices();
            WireEvents();
            LoadHistory();
            ShowWelcome();
        }

        // ═══════════════════════════════════════════
        // 初始化
        // ═══════════════════════════════════════════
        private void InitializeServices()
        {
            _settings = AppSettings.Load();
            _automationService = new TaskAutomationService(_settings);
            _taskRunner = new BackgroundTaskRunner(_automationService);
            _taskHistory = new List<AgentTask>();

            // 串接事件
            _taskRunner.OnTaskCompleted += TaskRunner_OnTaskCompleted;
            _taskRunner.OnStepUpdate += TaskRunner_OnStepUpdate;
            _taskRunner.OnProgressUpdate += TaskRunner_OnProgressUpdate;
            _taskRunner.OnError += TaskRunner_OnError;

            // 啟動背景任務佇列
            _taskRunner.Start();
        }

        private void WireEvents()
        {
            btnExecute.Click += BtnExecute_Click;
            btnClear.Click += BtnClear_Click;
            btnSettings.Click += BtnSettings_Click;
            btnExport.Click += BtnExport_Click;
            btnCopyResult.Click += BtnCopyResult_Click;
            btnRunCode.Click += BtnRunCode_Click;
            btnSaveCode.Click += BtnSaveCode_Click;

            txtQuery.KeyDown += TxtQuery_KeyDown;
            lstHistory.DoubleClick += LstHistory_DoubleClick;
            dgvSearchResults.CellDoubleClick += DgvSearchResults_CellDoubleClick;

            this.FormClosing += MainForm_FormClosing;
            this.Resize += MainForm_Resize;

            // 歷史紀錄右鍵選單
            _historyMenu = new ContextMenuStrip();
            _historyMenu.Items.Add("\u522a\u9664\u6b64\u7b46\u7d00\u9304", null, HistoryMenu_Delete);
            _historyMenu.Items.Add("\u6e05\u9664\u6240\u6709\u7d00\u9304", null, HistoryMenu_ClearAll);
            lstHistory.ContextMenuStrip = _historyMenu;
            lstHistory.MouseDown += LstHistory_MouseDown;
        }

        // ═══════════════════════════════════════════
        // 歷史紀錄存檔/讀檔
        // ═══════════════════════════════════════════
        private void SaveHistory()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[");

                int count = Math.Min(_taskHistory.Count, 200);
                int startIdx = _taskHistory.Count - count;

                for (int i = startIdx; i < _taskHistory.Count; i++)
                {
                    AgentTask t = _taskHistory[i];
                    string query = EscapeJson(t.Query ?? "");
                    string result = EscapeJson(t.Result ?? "");
                    string type = t.Type.ToString();
                    string status = t.Status.ToString();
                    string created = t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    string completed = t.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss");

                    sb.Append(string.Format(
                        "  {{\"query\":\"{0}\",\"type\":\"{1}\",\"status\":\"{2}\"," +
                        "\"created\":\"{3}\",\"completed\":\"{4}\",\"result\":\"{5}\"}}",
                        query, type, status, created, completed, result));

                    if (i < _taskHistory.Count - 1)
                        sb.AppendLine(",");
                    else
                        sb.AppendLine();
                }

                sb.AppendLine("]");
                File.WriteAllText(HistoryFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void LoadHistory()
        {
            try
            {
                if (!File.Exists(HistoryFilePath)) return;

                string json = File.ReadAllText(HistoryFilePath, Encoding.UTF8);
                if (string.IsNullOrEmpty(json)) return;

                MatchCollection matches = Regex.Matches(json,
                    @"\{[^{}]+\}", RegexOptions.Singleline);

                foreach (Match m in matches)
                {
                    string obj = m.Value;
                    AgentTask task = new AgentTask();

                    task.Query = ExtractHistoryJsonValue(obj, "query");
                    task.Result = ExtractHistoryJsonValue(obj, "result");

                    string typeStr = ExtractHistoryJsonValue(obj, "type");
                    if (!string.IsNullOrEmpty(typeStr))
                    {
                        try { task.Type = (TaskType)Enum.Parse(typeof(TaskType), typeStr, true); }
                        catch { task.Type = TaskType.AutoResearch; }
                    }

                    string statusStr = ExtractHistoryJsonValue(obj, "status");
                    if (!string.IsNullOrEmpty(statusStr))
                    {
                        try { task.Status = (TaskStatus)Enum.Parse(typeof(TaskStatus), statusStr, true); }
                        catch { task.Status = TaskStatus.Completed; }
                    }

                    string createdStr = ExtractHistoryJsonValue(obj, "created");
                    if (!string.IsNullOrEmpty(createdStr))
                    {
                        DateTime dt;
                        if (DateTime.TryParse(createdStr, out dt)) task.CreatedAt = dt;
                    }

                    string completedStr = ExtractHistoryJsonValue(obj, "completed");
                    if (!string.IsNullOrEmpty(completedStr))
                    {
                        DateTime dt;
                        if (DateTime.TryParse(completedStr, out dt)) task.CompletedAt = dt;
                    }

                    _taskHistory.Add(task);
                }

                // 顯示到 ListBox（最新在前）
                for (int i = _taskHistory.Count - 1; i >= 0; i--)
                {
                    AgentTask t = _taskHistory[i];
                    string historyItem = string.Format("[{0}] {1}",
                        t.Type.ToString().Substring(0, Math.Min(4, t.Type.ToString().Length)),
                        t.Query.Length > 30 ? t.Query.Substring(0, 30) + "..." : t.Query);
                    lstHistory.Items.Add(historyItem);
                }
            }
            catch { }
        }

        private string ExtractHistoryJsonValue(string json, string key)
        {
            string pattern = string.Format("\"{0}\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"",
                Regex.Escape(key));
            Match m = Regex.Match(json, pattern);
            if (m.Success)
            {
                return m.Groups[1].Value
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\")
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t");
            }
            return "";
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

        // ═══════════════════════════════════════════
        // 歷史右鍵選單
        // ═══════════════════════════════════════════
        private void LstHistory_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int idx = lstHistory.IndexFromPoint(e.Location);
                if (idx >= 0)
                {
                    lstHistory.SelectedIndex = idx;
                }
            }
        }

        private void HistoryMenu_Delete(object sender, EventArgs e)
        {
            if (lstHistory.SelectedIndex < 0) return;

            int listIdx = lstHistory.SelectedIndex;
            int taskIdx = _taskHistory.Count - 1 - listIdx;

            if (taskIdx >= 0 && taskIdx < _taskHistory.Count)
            {
                _taskHistory.RemoveAt(taskIdx);
                lstHistory.Items.RemoveAt(listIdx);
                SaveHistory();
                SetStatus("\u5df2\u522a\u9664\u7d00\u9304");
            }
        }

        private void HistoryMenu_ClearAll(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                "\u78ba\u5b9a\u8981\u6e05\u9664\u6240\u6709\u6b77\u53f2\u7d00\u9304\u55ce\uff1f",
                "\u78ba\u8a8d", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr == DialogResult.Yes)
            {
                _taskHistory.Clear();
                lstHistory.Items.Clear();
                SaveHistory();
                SetStatus("\u5df2\u6e05\u9664\u6240\u6709\u7d00\u9304");
            }
        }

        private void ShowWelcome()
        {
            rtbResult.Clear();
            AppendColoredText(rtbResult, "=======================================\n", Color.FromArgb(100, 180, 255));
            AppendColoredText(rtbResult, "   [AI] \u667a\u6167\u4ee3\u7406\u5de5\u5177 v1.0\n", Color.FromArgb(100, 255, 150));
            AppendColoredText(rtbResult, "   \u9069\u7528\u65bc Windows 7\uff08\u542b\u7121 SP1\uff09\n", Color.Gray);
            AppendColoredText(rtbResult, "=======================================\n\n", Color.FromArgb(100, 180, 255));

            AppendColoredText(rtbResult, "\u3010\u529f\u80fd\u5217\u8868\u3011\n", Color.FromArgb(255, 200, 100));
            AppendColoredText(rtbResult, "  >> \u7db2\u8def\u641c\u5c0b\uff08DuckDuckGo + Wikipedia\uff09\n", Color.White);
            AppendColoredText(rtbResult, "  >> \u81ea\u52d5\u7814\u7a76\uff08\u591a\u6e90\u641c\u5c0b + AI \u5206\u6790\uff09\n", Color.White);
            AppendColoredText(rtbResult, "  >> \u667a\u6167\u6458\u8981\uff08\u7db2\u9801/\u6587\u5b57\u6458\u8981\uff09\n", Color.White);
            AppendColoredText(rtbResult, "  >> \u4e3b\u984c\u6bd4\u8f03\uff08\u81ea\u52d5\u5c0d\u6bd4\u5206\u6790\uff09\n", Color.White);
            AppendColoredText(rtbResult, "  >> \u6578\u5b78\u8a08\u7b97\uff08\u56db\u5247\u904b\u7b97 + \u6b21\u65b9\uff09\n", Color.White);
            AppendColoredText(rtbResult, "  >> \u7a0b\u5f0f\u64cd\u63a7\uff08\u958b\u555f/\u95dc\u9589/\u5217\u51fa\uff09\n", Color.White);
            AppendColoredText(rtbResult, "  >> \u6a94\u6848\u7ba1\u7406\uff08\u700f\u89bd/\u641c\u5c0b/\u958b\u555f\uff09\n", Color.White);
            AppendColoredText(rtbResult, "  >> CMD \u547d\u4ee4\uff08\u5b89\u5168\u767d\u540d\u55ae\u57f7\u884c\uff09\n", Color.White);
            AppendColoredText(rtbResult, "  >> \u87a2\u5e55\u622a\u5716\n", Color.White);
            AppendColoredText(rtbResult, "  >> \u7a0b\u5f0f\u78bc\u751f\u6210\uff08AI + \u6a21\u677f + \u5373\u6642\u7de8\u8b6f\uff09\n", Color.White);
            AppendColoredText(rtbResult, "  >> \u6279\u6b21\u57f7\u884c\uff08\u591a\u6307\u4ee4\u4f9d\u5e8f\u8655\u7406\uff09\n", Color.White);
            AppendColoredText(rtbResult, "\n", Color.White);

            AppendColoredText(rtbResult, "\u3010\u4f7f\u7528\u65b9\u5f0f\u3011\n", Color.FromArgb(255, 200, 100));
            AppendColoredText(rtbResult, "  \u8f38\u5165\u81ea\u7136\u8a9e\u8a00\u6307\u4ee4\uff0c\u6309 Enter \u6216\u9ede\u64ca\u300c\u57f7\u884c\u300d\n", Color.White);
            AppendColoredText(rtbResult, "  \u53ef\u4f7f\u7528\u5feb\u6377\u6309\u9215\u5feb\u901f\u5207\u63db\u529f\u80fd\n", Color.White);
            AppendColoredText(rtbResult, "  \u6700\u5c0f\u5316\u5f8c\u6703\u99d0\u7559\u7cfb\u7d71\u6258\u76e4\u7e7c\u7e8c\u57f7\u884c\u4efb\u52d9\n\n", Color.White);

            AppendColoredText(rtbResult, "\u3010AI \u4f86\u6e90\u3011\n", Color.FromArgb(255, 200, 100));
            string aiStatus = string.IsNullOrEmpty(_settings.GeminiApiKey)
                ? "  \u76ee\u524d\uff1aDuckDuckGo AI + \u96e2\u7dda\u6a21\u677f\uff08\u672a\u8a2d\u5b9a Gemini Key\uff09\n"
                : "  \u76ee\u524d\uff1aGoogle Gemini + DuckDuckGo AI + \u96e2\u7dda\u6a21\u677f\n";
            AppendColoredText(rtbResult, aiStatus, Color.FromArgb(150, 255, 150));
            AppendColoredText(rtbResult, "  \u53ef\u65bc\u300c\u8a2d\u5b9a\u300d\u4e2d\u914d\u7f6e API Key\n", Color.Gray);
        }

        // ═══════════════════════════════════════════
        // 執行任務
        // ═══════════════════════════════════════════
        private void BtnExecute_Click(object sender, EventArgs e)
        {
            ExecuteCurrentQuery();
        }

        private void TxtQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ExecuteCurrentQuery();
            }
        }

        private void ExecuteCurrentQuery()
        {
            string query = txtQuery.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                SetStatus("\u8acb\u8f38\u5165\u6307\u4ee4");
                return;
            }

            if (_isExecuting)
            {
                _taskRunner.EnqueueTask(query, GetSelectedTaskType());
                SetStatus(string.Format("\u4efb\u52d9\u5df2\u52a0\u5165\u4f47\u5217\uff08\u4f47\u5217: {0}\uff09", _taskRunner.QueueCount));
                UpdateQueueLabel();
                return;
            }

            _isExecuting = true;
            SetUIExecuting(true);
            SetStatus("\u57f7\u884c\u4e2d\uff1a" + query);
            rtbSteps.Clear();

            TaskType? forceType = GetSelectedTaskType();
            _taskRunner.ExecuteAsync(query, forceType, null);
        }

        private TaskType? GetSelectedTaskType()
        {
            if (cboTaskType.SelectedIndex <= 0) return null;

            switch (cboTaskType.SelectedIndex)
            {
                case 1: return TaskType.Search;
                case 2: return TaskType.AutoResearch;
                case 3: return TaskType.Summarize;
                case 4: return TaskType.Compare;
                case 5: return TaskType.Calculate;
                case 6: return TaskType.SystemInfo;
                case 7: return TaskType.LaunchApp;
                case 8: return TaskType.CloseApp;
                case 9: return TaskType.ListProcesses;
                case 10: return TaskType.FileManagement;
                case 11: return TaskType.RunCommand;
                case 12: return TaskType.ScreenCapture;
                case 13: return TaskType.ClipboardOp;
                case 14: return TaskType.InstalledApps;
                case 15: return TaskType.GenerateCode;
                case 16: return TaskType.BatchOperation;
                default: return null;
            }
        }

        // ═══════════════════════════════════════════
        // 任務完成回呼
        // ═══════════════════════════════════════════
        private void TaskRunner_OnTaskCompleted(AgentTask task)
        {
            ThreadSafeUI.Run(this, delegate
            {
                _isExecuting = false;
                SetUIExecuting(false);

                // 顯示結果
                DisplayResult(task);

                // 加入歷史
                _taskHistory.Add(task);
                string historyItem = string.Format("[{0}] {1}",
                    task.Type.ToString().Substring(0, Math.Min(4, task.Type.ToString().Length)),
                    task.Query.Length > 30 ? task.Query.Substring(0, 30) + "..." : task.Query);
                lstHistory.Items.Insert(0, historyItem);

                // ★ 自動存檔
                SaveHistory();

                // 狀態更新
                string statusText = task.Status == TaskStatus.Completed
                    ? string.Format("\u5b8c\u6210\uff1a{0}\uff08\u8017\u6642 {1:F1}s\uff09", task.Type, (task.CompletedAt - task.CreatedAt).TotalSeconds)
                    : "\u5931\u6557\uff1a" + task.Type;
                SetStatus(statusText);
                UpdateQueueLabel();

                // 托盤通知
                if (this.WindowState == FormWindowState.Minimized && _settings.ShowBalloonNotify)
                {
                    trayIcon.ShowBalloonTip(3000, "\u4efb\u52d9\u5b8c\u6210", historyItem, ToolTipIcon.Info);
                }
            });
        }

        private void TaskRunner_OnStepUpdate(string step)
        {
            ThreadSafeUI.Run(this, delegate
            {
                rtbSteps.AppendText(string.Format("[{0:HH:mm:ss}] {1}\n", DateTime.Now, step));
                rtbSteps.ScrollToCaret();
            });
        }

        private void TaskRunner_OnProgressUpdate(int progress)
        {
            ThreadSafeUI.Run(this, delegate
            {
                progressBar.Value = Math.Min(progress, 100);
            });
        }

        private void TaskRunner_OnError(string error)
        {
            ThreadSafeUI.Run(this, delegate
            {
                _isExecuting = false;
                SetUIExecuting(false);
                SetStatus("\u932f\u8aa4\uff1a" + error);
                AppendColoredText(rtbResult, "\n[X] \u932f\u8aa4\uff1a" + error + "\n", Color.FromArgb(255, 100, 100));
            });
        }

        // ═══════════════════════════════════════════
        // 顯示結果
        // ═══════════════════════════════════════════
        private void DisplayResult(AgentTask task)
        {
            rtbResult.Clear();
            AppendColoredText(rtbResult,
                string.Format("=== {0} ===\n", task.Type), Color.FromArgb(100, 180, 255));
            AppendColoredText(rtbResult,
                string.Format("\u67e5\u8a62\uff1a{0}\n", task.Query), Color.FromArgb(200, 200, 200));
            AppendColoredText(rtbResult,
                string.Format("\u6642\u9593\uff1a{0:yyyy-MM-dd HH:mm:ss}\n\n", task.CompletedAt), Color.Gray);

            if (task.Status == TaskStatus.Completed)
            {
                AppendColoredText(rtbResult, task.Result ?? "(\u7121\u7d50\u679c)", Color.White);
            }
            else
            {
                AppendColoredText(rtbResult, "[X] " + (task.Result ?? "\u57f7\u884c\u5931\u6557"), Color.FromArgb(255, 120, 120));
            }

            // 搜尋結果表格
            dgvSearchResults.Rows.Clear();
            if (task.SearchResults != null)
            {
                foreach (SearchResult r in task.SearchResults)
                {
                    dgvSearchResults.Rows.Add(
                        r.Source ?? "",
                        r.Title ?? "",
                        r.Snippet != null && r.Snippet.Length > 100
                            ? r.Snippet.Substring(0, 100) + "..." : (r.Snippet ?? ""),
                        r.Url ?? "",
                        r.RelevanceScore.ToString("F2")
                    );
                }
            }

            // 程式碼分頁
            if (task.Type == TaskType.GenerateCode && !string.IsNullOrEmpty(task.Result))
            {
                string code = AIAgentTool.Utils.HtmlHelper.ExtractCodeBlock(task.Result);
                if (!string.IsNullOrEmpty(code))
                {
                    rtbCode.Text = code;
                    tabMain.SelectedTab = tabCode;
                }
                else
                {
                    tabMain.SelectedTab = tabResult;
                }
            }
            else
            {
                tabMain.SelectedTab = tabResult;
            }

            RefreshCacheView();
        }

        private void RefreshCacheView()
        {
            dgvCache.Rows.Clear();
            List<KnowledgeItem> items = _automationService.ReasoningEngine.GetAllCachedItems();
            foreach (KnowledgeItem item in items)
            {
                dgvCache.Rows.Add(
                    item.Topic,
                    item.Source,
                    item.CachedAt.ToString("yyyy-MM-dd HH:mm"),
                    item.UseCount
                );
            }
        }

        // ═══════════════════════════════════════════
        // 快捷按鈕
        // ═══════════════════════════════════════════
        private void QuickButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null || btn.Tag == null) return;

            int index = (int)btn.Tag;

            int[] quickToCombo = new int[] {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16
            };

            if (index < quickToCombo.Length && quickToCombo[index] < cboTaskType.Items.Count)
            {
                cboTaskType.SelectedIndex = quickToCombo[index];
            }

            switch (index)
            {
                case 5:
                    txtQuery.Text = "\u7cfb\u7d71\u8cc7\u8a0a";
                    ExecuteCurrentQuery();
                    break;
                case 8:
                    txtQuery.Text = "\u7a0b\u5e8f\u5217\u8868";
                    ExecuteCurrentQuery();
                    break;
                case 11:
                    txtQuery.Text = "\u622a\u5716";
                    ExecuteCurrentQuery();
                    break;
                case 12:
                    txtQuery.Text = "\u8b80\u53d6\u526a\u8cbc\u7c3f";
                    ExecuteCurrentQuery();
                    break;
                default:
                    txtQuery.Focus();
                    break;
            }
        }

        private void QuickButton_MouseEnter(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
                btn.BackColor = Color.FromArgb(70, 70, 85);
        }

        private void QuickButton_MouseLeave(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
                btn.BackColor = Color.FromArgb(50, 50, 58);
        }

        // ═══════════════════════════════════════════
        // 按鈕事件
        // ═══════════════════════════════════════════
        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtQuery.Clear();
            rtbResult.Clear();
            rtbSteps.Clear();
            rtbCode.Clear();
            dgvSearchResults.Rows.Clear();
            progressBar.Value = 0;
            SetStatus("\u5df2\u6e05\u9664");
            txtQuery.Focus();
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            using (SettingsForm sf = new SettingsForm(_settings))
            {
                if (sf.ShowDialog(this) == DialogResult.OK)
                {
                    _settings = sf.GetSettings();
                    _settings.Save();
                    SetStatus("\u8a2d\u5b9a\u5df2\u5132\u5b58");
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "\u6587\u5b57\u6a94\u6848 (*.txt)|*.txt|\u6240\u6709\u6a94\u6848 (*.*)|*.*";
            sfd.FileName = string.Format("AI_Result_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now);
            sfd.InitialDirectory = _settings.DefaultSavePath;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, rtbResult.Text, Encoding.UTF8);
                    SetStatus("\u5df2\u532f\u51fa\uff1a" + sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("\u532f\u51fa\u5931\u6557\uff1a" + ex.Message, "\u932f\u8aa4",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnCopyResult_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(rtbResult.Text))
            {
                try
                {
                    Clipboard.SetText(rtbResult.Text);
                    SetStatus("\u5df2\u8907\u88fd\u5230\u526a\u8cbc\u7c3f");
                }
                catch { SetStatus("\u8907\u88fd\u5931\u6557"); }
            }
        }

        private void BtnRunCode_Click(object sender, EventArgs e)
        {
            string code = rtbCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("\u7121\u7a0b\u5f0f\u78bc\u53ef\u57f7\u884c");
                return;
            }

            DialogResult dr = MessageBox.Show(
                "\u78ba\u5b9a\u8981\u7de8\u8b6f\u4e26\u57f7\u884c\u6b64\u7a0b\u5f0f\u78bc\u55ce\uff1f\n\n\u8acb\u78ba\u8a8d\u7a0b\u5f0f\u78bc\u5167\u5bb9\u5b89\u5168\u7121\u5bb3\u3002",
                "\u57f7\u884c\u78ba\u8a8d", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            SetStatus("\u7de8\u8b6f\u4e2d...");
            CompileResult result = _automationService.CodeCompiler.Compile(code);

            if (result.Success)
            {
                SetStatus("\u7de8\u8b6f\u6210\u529f\uff0c\u57f7\u884c\u4e2d...");
                string output = _automationService.CodeCompiler.ExecuteCompiled(result.OutputPath, 30000);
                AppendColoredText(rtbResult, "\n\n\u3010\u57f7\u884c\u8f38\u51fa\u3011\n" + output, Color.FromArgb(150, 255, 150));
                SetStatus("\u57f7\u884c\u5b8c\u6210");
            }
            else
            {
                AppendColoredText(rtbResult, "\n\n\u3010\u7de8\u8b6f\u932f\u8aa4\u3011\n", Color.FromArgb(255, 100, 100));
                foreach (string err in result.Errors)
                    AppendColoredText(rtbResult, "  " + err + "\n", Color.FromArgb(255, 150, 150));
                SetStatus("\u7de8\u8b6f\u5931\u6557");
            }
        }

        private void BtnSaveCode_Click(object sender, EventArgs e)
        {
            string code = rtbCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("\u7121\u7a0b\u5f0f\u78bc\u53ef\u5132\u5b58");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "C# \u6a94\u6848 (*.cs)|*.cs|\u6240\u6709\u6a94\u6848 (*.*)|*.*";
            sfd.FileName = string.Format("Generated_{0:yyyyMMdd_HHmmss}.cs", DateTime.Now);
            sfd.InitialDirectory = _settings.DefaultSavePath;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, code, Encoding.UTF8);
                    SetStatus("\u7a0b\u5f0f\u78bc\u5df2\u5132\u5b58\uff1a" + sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("\u5132\u5b58\u5931\u6557\uff1a" + ex.Message, "\u932f\u8aa4",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ═══════════════════════════════════════════
        // 歷史與表格事件
        // ═══════════════════════════════════════════
        private void LstHistory_DoubleClick(object sender, EventArgs e)
        {
            if (lstHistory.SelectedIndex < 0) return;
            int idx = lstHistory.SelectedIndex;
            int taskIdx = _taskHistory.Count - 1 - idx;
            if (taskIdx >= 0 && taskIdx < _taskHistory.Count)
            {
                DisplayResult(_taskHistory[taskIdx]);
            }
        }

        private void DgvSearchResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string url = dgvSearchResults.Rows[e.RowIndex].Cells["colUrl"].Value as string;
            if (!string.IsNullOrEmpty(url))
            {
                try { Process.Start(url); }
                catch { }
            }
        }

        // ═══════════════════════════════════════════
        // 系統托盤
        // ═══════════════════════════════════════════
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized && _settings.MinimizeToTray)
            {
                this.Hide();
                trayIcon.Visible = true;
                if (_settings.ShowBalloonNotify)
                {
                    trayIcon.ShowBalloonTip(2000, "AI \u667a\u6167\u4ee3\u7406\u5de5\u5177",
                        "\u7a0b\u5f0f\u5df2\u6700\u5c0f\u5316\u81f3\u7cfb\u7d71\u6258\u76e4\uff0c\u4efb\u52d9\u7e7c\u7e8c\u57f7\u884c\u4e2d\u3002", ToolTipIcon.Info);
                }
            }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void TrayMenu_Show(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void TrayMenu_Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            _taskRunner.Stop();
            SaveHistory();
            Application.Exit();
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            trayIcon.Visible = false;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isExecuting || _taskRunner.QueueCount > 0)
            {
                DialogResult dr = MessageBox.Show(
                    string.Format("\u76ee\u524d\u6709\u4efb\u52d9\u57f7\u884c\u4e2d\uff08\u4f47\u5217: {0}\uff09\uff0c\u78ba\u5b9a\u8981\u95dc\u9589\u55ce\uff1f", _taskRunner.QueueCount),
                    "\u78ba\u8a8d\u95dc\u9589", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dr != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _taskRunner.Stop();
            SaveHistory();
            trayIcon.Visible = false;
        }

        // ═══════════════════════════════════════════
        // UI 輔助方法
        // ═══════════════════════════════════════════
        private void SetStatus(string text)
        {
            lblStatus.Text = text;
        }

        private void UpdateQueueLabel()
        {
            lblQueue.Text = string.Format("\u4f47\u5217: {0}", _taskRunner.QueueCount);
        }

        private void SetUIExecuting(bool executing)
        {
            btnExecute.Enabled = !executing;
            btnExecute.Text = executing ? "... \u57f7\u884c\u4e2d" : "> \u57f7\u884c";
            if (!executing) progressBar.Value = 0;
        }

        private void AppendColoredText(RichTextBox rtb, string text, Color color)
        {
            int start = rtb.TextLength;
            rtb.AppendText(text);
            rtb.Select(start, text.Length);
            rtb.SelectionColor = color;
            rtb.SelectionLength = 0;
            rtb.ScrollToCaret();
        }
    }
}
