using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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

        public MainForm()
        {
            InitializeComponent();
            InitializeServices();
            WireEvents();
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
        }

        private void ShowWelcome()
        {
            rtbResult.Clear();
            AppendColoredText(rtbResult, "=======================================\n", Color.FromArgb(100, 180, 255));
            AppendColoredText(rtbResult, "   [AI] 智慧代理工具 v1.0\n", Color.FromArgb(100, 255, 150));
            AppendColoredText(rtbResult, "   適用於 Windows 7（含無 SP1）\n", Color.Gray);
            AppendColoredText(rtbResult, "=======================================\n\n", Color.FromArgb(100, 180, 255));

            AppendColoredText(rtbResult, "【功能列表】\n", Color.FromArgb(255, 200, 100));
            AppendColoredText(rtbResult, "  >> 網路搜尋（DuckDuckGo + Wikipedia）\n", Color.White);
            AppendColoredText(rtbResult, "  >> 自動研究（多源搜尋 + AI 分析）\n", Color.White);
            AppendColoredText(rtbResult, "  >> 智慧摘要（網頁/文字摘要）\n", Color.White);
            AppendColoredText(rtbResult, "  >> 主題比較（自動對比分析）\n", Color.White);
            AppendColoredText(rtbResult, "  >> 數學計算（四則運算 + 次方）\n", Color.White);
            AppendColoredText(rtbResult, "  >> 程式操控（開啟/關閉/列出）\n", Color.White);
            AppendColoredText(rtbResult, "  >> 檔案管理（瀏覽/搜尋/開啟）\n", Color.White);
            AppendColoredText(rtbResult, "  >> CMD 命令（安全白名單執行）\n", Color.White);
            AppendColoredText(rtbResult, "  >> 螢幕截圖\n", Color.White);
            AppendColoredText(rtbResult, "  >> 程式碼生成（AI + 模板 + 即時編譯）\n", Color.White);
            AppendColoredText(rtbResult, "  >> 批次執行（多指令依序處理）\n", Color.White);
            AppendColoredText(rtbResult, "\n", Color.White);

            AppendColoredText(rtbResult, "【使用方式】\n", Color.FromArgb(255, 200, 100));
            AppendColoredText(rtbResult, "  輸入自然語言指令，按 Enter 或點擊「執行」\n", Color.White);
            AppendColoredText(rtbResult, "  可使用快捷按鈕快速切換功能\n", Color.White);
            AppendColoredText(rtbResult, "  最小化後會駐留系統托盤繼續執行任務\n\n", Color.White);

            AppendColoredText(rtbResult, "【AI 來源】\n", Color.FromArgb(255, 200, 100));
            string aiStatus = string.IsNullOrEmpty(_settings.GeminiApiKey)
                ? "  目前：DuckDuckGo AI + 離線模板（未設定 Gemini Key）\n"
                : "  目前：Google Gemini + DuckDuckGo AI + 離線模板\n";
            AppendColoredText(rtbResult, aiStatus, Color.FromArgb(150, 255, 150));
            AppendColoredText(rtbResult, "  可於「設定」中配置 API Key\n", Color.Gray);
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
                SetStatus("請輸入指令");
                return;
            }

            if (_isExecuting)
            {
                // 加入佇列
                _taskRunner.EnqueueTask(query, GetSelectedTaskType());
                SetStatus(string.Format("任務已加入佇列（佇列: {0}）", _taskRunner.QueueCount));
                UpdateQueueLabel();
                return;
            }

            _isExecuting = true;
            SetUIExecuting(true);
            SetStatus("執行中：" + query);
            rtbSteps.Clear();

            TaskType? forceType = GetSelectedTaskType();

            // 使用背景執行
            _taskRunner.ExecuteAsync(query, forceType, null);
        }

        private TaskType? GetSelectedTaskType()
{
    if (cboTaskType.SelectedIndex <= 0) return null; // 自動判斷 → null → 交給 AI 規劃

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

                // 狀態更新
                string statusText = task.Status == TaskStatus.Completed
                    ? string.Format("完成：{0}（耗時 {1:F1}s）", task.Type, (task.CompletedAt - task.CreatedAt).TotalSeconds)
                    : "失敗：" + task.Type;
                SetStatus(statusText);
                UpdateQueueLabel();

                // 托盤通知
                if (this.WindowState == FormWindowState.Minimized && _settings.ShowBalloonNotify)
                {
                    trayIcon.ShowBalloonTip(3000, "任務完成", historyItem, ToolTipIcon.Info);
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
                SetStatus("錯誤：" + error);
                AppendColoredText(rtbResult, "\n[X] 錯誤：" + error + "\n", Color.FromArgb(255, 100, 100));
            });
        }

        // ═══════════════════════════════════════════
        // 顯示結果
        // ═══════════════════════════════════════════
        private void DisplayResult(AgentTask task)
        {
            // 結果頁
            rtbResult.Clear();
            AppendColoredText(rtbResult,
                string.Format("=== {0} ===\n", task.Type), Color.FromArgb(100, 180, 255));
            AppendColoredText(rtbResult,
                string.Format("查詢：{0}\n", task.Query), Color.FromArgb(200, 200, 200));
            AppendColoredText(rtbResult,
                string.Format("時間：{0:yyyy-MM-dd HH:mm:ss}\n\n", task.CompletedAt), Color.Gray);

            if (task.Status == TaskStatus.Completed)
            {
                AppendColoredText(rtbResult, task.Result ?? "(無結果)", Color.White);
            }
            else
            {
                AppendColoredText(rtbResult, "[X] " + (task.Result ?? "執行失敗"), Color.FromArgb(255, 120, 120));
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

            // 程式碼分頁（如果結果中包含程式碼）
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

            // 更新快取顯示
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

    // 快捷按鈕 → 下拉選單正確索引
    int[] quickToCombo = new int[] {
        1,   // 0: 搜尋
        2,   // 1: 研究
        3,   // 2: 摘要
        4,   // 3: 比較
        5,   // 4: 計算
        6,   // 5: 系統
        7,   // 6: 開啟
        8,   // 7: 關閉
        9,   // 8: 程序
        10,  // 9: 檔案
        11,  // 10: CMD
        12,  // 11: 截圖
        13,  // 12: 剪貼簿
        14,  // 13: 已安裝
        15,  // 14: 程式碼
        16   // 15: 批次
    };

    if (index < quickToCombo.Length && quickToCombo[index] < cboTaskType.Items.Count)
    {
        cboTaskType.SelectedIndex = quickToCombo[index];
    }

    switch (index)
    {
        case 5: // 系統
            txtQuery.Text = "系統資訊";
            ExecuteCurrentQuery();
            break;
        case 8: // 程序
            txtQuery.Text = "程序列表";
            ExecuteCurrentQuery();
            break;
        case 11: // 截圖
            txtQuery.Text = "截圖";
            ExecuteCurrentQuery();
            break;
        case 12: // 剪貼簿
            txtQuery.Text = "讀取剪貼簿";
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
            SetStatus("已清除");
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
                    SetStatus("設定已儲存");
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "文字檔案 (*.txt)|*.txt|所有檔案 (*.*)|*.*";
            sfd.FileName = string.Format("AI_Result_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now);
            sfd.InitialDirectory = _settings.DefaultSavePath;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    System.IO.File.WriteAllText(sfd.FileName, rtbResult.Text, System.Text.Encoding.UTF8);
                    SetStatus("已匯出：" + sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("匯出失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    SetStatus("已複製到剪貼簿");
                }
                catch { SetStatus("複製失敗"); }
            }
        }

        private void BtnRunCode_Click(object sender, EventArgs e)
        {
            string code = rtbCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("無程式碼可執行");
                return;
            }

            // 確認對話框
            DialogResult dr = MessageBox.Show(
                "確定要編譯並執行此程式碼嗎？\n\n請確認程式碼內容安全無害。",
                "執行確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dr != DialogResult.Yes) return;

            SetStatus("編譯中...");
            CompileResult result = _automationService.CodeCompiler.Compile(code);

            if (result.Success)
            {
                SetStatus("編譯成功，執行中...");
                string output = _automationService.CodeCompiler.ExecuteCompiled(result.OutputPath, 30000);

                AppendColoredText(rtbResult, "\n\n【執行輸出】\n" + output, Color.FromArgb(150, 255, 150));
                SetStatus("執行完成");
            }
            else
            {
                AppendColoredText(rtbResult, "\n\n【編譯錯誤】\n", Color.FromArgb(255, 100, 100));
                foreach (string err in result.Errors)
                    AppendColoredText(rtbResult, "  " + err + "\n", Color.FromArgb(255, 150, 150));
                SetStatus("編譯失敗");
            }
        }

        private void BtnSaveCode_Click(object sender, EventArgs e)
        {
            string code = rtbCode.Text.Trim();
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("無程式碼可儲存");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "C# 檔案 (*.cs)|*.cs|所有檔案 (*.*)|*.*";
            sfd.FileName = string.Format("Generated_{0:yyyyMMdd_HHmmss}.cs", DateTime.Now);
            sfd.InitialDirectory = _settings.DefaultSavePath;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    System.IO.File.WriteAllText(sfd.FileName, code, System.Text.Encoding.UTF8);
                    SetStatus("程式碼已儲存：" + sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("儲存失敗：" + ex.Message, "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (idx < _taskHistory.Count)
            {
                // 最新在前，需反轉索引
                int taskIdx = _taskHistory.Count - 1 - idx;
                if (taskIdx >= 0 && taskIdx < _taskHistory.Count)
                {
                    DisplayResult(_taskHistory[taskIdx]);
                }
            }
        }

        private void DgvSearchResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string url = dgvSearchResults.Rows[e.RowIndex].Cells["colUrl"].Value as string;
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    Process.Start(url);
                }
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
                    trayIcon.ShowBalloonTip(2000, "AI 智慧代理工具",
                        "程式已最小化至系統托盤，任務繼續執行中。", ToolTipIcon.Info);
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
            // 如果有任務在執行，提示
            if (_isExecuting || _taskRunner.QueueCount > 0)
            {
                DialogResult dr = MessageBox.Show(
                    string.Format("目前有任務執行中（佇列: {0}），確定要關閉嗎？", _taskRunner.QueueCount),
                    "確認關閉", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dr != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _taskRunner.Stop();
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
            lblQueue.Text = string.Format("佇列: {0}", _taskRunner.QueueCount);
        }

        private void SetUIExecuting(bool executing)
        {
            btnExecute.Enabled = !executing;
            btnExecute.Text = executing ? "... 執行中" : "> 執行";
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
