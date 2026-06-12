using System;
using System.Collections.Generic;
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
    public partial class MainForm : Form
    {
        private AppSettings _settings;
        private TaskAutomationService _automationService;
        private BackgroundTaskRunner _taskRunner;
        private bool _isExecuting;

        private List<ChatSession> _sessions;
        private ChatSession _currentSession;
        private string _sessionsDir;
        private bool _isRendering;

        public MainForm()
        {
            InitializeComponent();
            InitializeServices();
            WireEvents();
            LoadAllSessions();

            if (_sessions.Count == 0)
                StartNewSession();
            else
            {
                _currentSession = _sessions[0];
                RefreshSessionList();
                lstSessions.SelectedIndex = 0;
                RenderChat();
            }

            // 啟動時顯示已學習的技能數量
            try
            {
                string skillInfo = _automationService.CodeGenerator.GetSkillsSummary();
                if (!string.IsNullOrEmpty(skillInfo))
                    SetStatus(skillInfo);
            }
            catch { }
        }

        private void InitializeServices()
        {
            _settings = AppSettings.Load();
            _automationService = new TaskAutomationService(_settings);
            _taskRunner = new BackgroundTaskRunner(_automationService);
            _sessions = new List<ChatSession>();

            _taskRunner.OnTaskCompleted += TaskRunner_OnTaskCompleted;
            _taskRunner.OnStepUpdate += TaskRunner_OnStepUpdate;
            _taskRunner.OnProgressUpdate += TaskRunner_OnProgressUpdate;
            _taskRunner.OnError += TaskRunner_OnError;
            _taskRunner.Start();

            _sessionsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chat_sessions");
            if (!Directory.Exists(_sessionsDir))
                Directory.CreateDirectory(_sessionsDir);
        }

        private void WireEvents()
        {
            btnSend.Click += BtnSend_Click;
            btnNewChat.Click += BtnNewChat_Click;
            btnSettings.Click += BtnSettings_Click;
            txtInput.KeyDown += TxtInput_KeyDown;
            lstSessions.SelectedIndexChanged += LstSessions_SelectedIndexChanged;

            btnRunCode.Click += BtnRunCode_Click;
            btnSaveCode.Click += BtnSaveCode_Click;
            btnCopyCode.Click += BtnCopyCode_Click;

            sessionMenu.Items[0].Click += SessionMenu_Rename;
            sessionMenu.Items[1].Click += SessionMenu_Delete;
            sessionMenu.Items[3].Click += SessionMenu_ClearAll;

            trayMenu.Items[0].Click += TrayMenu_Show;
            trayMenu.Items[2].Click += TrayMenu_Exit;
            trayIcon.DoubleClick += TrayIcon_DoubleClick;

            this.FormClosing += MainForm_FormClosing;
            this.Resize += MainForm_Resize;
            pnlChatInner.Resize += delegate { if (!_isRendering) RenderChat(); };

            // 功能快捷選單事件
            btnMenuImage.Click += delegate { QuickAction("生成圖片："); };
            btnMenuVideo.Click += delegate { QuickAction("生成影片："); };
            btnMenuTTS.Click += delegate { QuickAction("朗讀："); };
            btnMenuScreenshot.Click += delegate { txtInput.Text = "截圖"; SendMessage(); };
            btnMenuWindows.Click += delegate { txtInput.Text = "列出視窗"; SendMessage(); };
            btnMenuKnowledge.Click += delegate { QuickAction("知識庫搜尋："); };
            btnMenuLaunch.Click += delegate { QuickAction("開啟 "); };
            btnMenuCmd.Click += delegate { QuickAction("cmd "); };
            // 修正聊天面板滾輪問題
pnlChatInner.MouseEnter += delegate
{
    pnlChatInner.Focus();
};

        }

        // =======================================================
        // 傳送
        // =======================================================
        private void BtnSend_Click(object sender, EventArgs e) { SendMessage(); }

        private void TxtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
        }

        private void SendMessage()
        {
            string text = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text) || _isExecuting) return;

            var userMsg = new ChatMessage("user", text, DateTime.Now);
            _currentSession.Messages.Add(userMsg);
            AddBubbleToUI(userMsg);
            txtInput.Clear();

            if (_currentSession.Title.StartsWith("\u65b0\u5c0d\u8a71"))
            {
                _currentSession.Title = Truncate(text, 25);
                RefreshSessionList();
            }
            SaveSession(_currentSession);

            _isExecuting = true;
            btnSend.Enabled = false;
            btnSend.Text = "...";
            SetStatus("\u57f7\u884c\u4e2d: " + Truncate(text, 40));
            progressBar.Value = 10;
            tabMain.SelectedTab = tabChat;

            // 如果使用者輸入包含「修復」「修正」「bug」「錯誤」等關鍵字，自動附帶目前的程式碼
            string contextQuery = BuildContextQuery(text);
            string lowerText = text.ToLower();
            if ((lowerText.Contains("修復") || lowerText.Contains("修正") ||
                 lowerText.Contains("bug") || lowerText.Contains("錯誤") ||
                 lowerText.Contains("問題") || lowerText.Contains("fix"))
                && !string.IsNullOrEmpty(rtbCode.Text.Trim()))
            {
                contextQuery = BuildContextQuery(
                    text + "\n\n【請修改以下現有程式碼，不要重新生成新程式】\n```csharp\n" + rtbCode.Text + "\n```");
            }
            _taskRunner.ExecuteAsync(contextQuery, null, null);
        }

        // =======================================================
        // 任務完成
        // =======================================================
        private void TaskRunner_OnTaskCompleted(AgentTask task)
        {
            ThreadSafeUI.Run(this, delegate
            {
                _isExecuting = false;
                btnSend.Enabled = true;
                btnSend.Text = "\u50b3\u9001";
                progressBar.Value = 0;

                string reply = task.Result ?? "(\u7121\u56de\u61c9)";

                string extractedCode = HtmlHelper.ExtractCodeBlock(reply);
                if (!string.IsNullOrEmpty(extractedCode))
                {
                    rtbCode.Text = extractedCode;
                    tabMain.SelectedTab = tabCode;
                }

                var aiMsg = new ChatMessage("ai", reply, DateTime.Now);
                _currentSession.Messages.Add(aiMsg);
                AddBubbleToUI(aiMsg);
                SaveSession(_currentSession);

                double sec = (task.CompletedAt - task.CreatedAt).TotalSeconds;
                SetStatus(string.Format("\u5b8c\u6210 ({0:F1}s)", sec));

                if (this.WindowState == FormWindowState.Minimized && _settings.ShowBalloonNotify)
                    trayIcon.ShowBalloonTip(3000, "\u4efb\u52d9\u5b8c\u6210", Truncate(task.Query, 30), ToolTipIcon.Info);
            });
        }

        private void TaskRunner_OnStepUpdate(string step)
        {
            ThreadSafeUI.Run(this, delegate { SetStatus(step); });
        }

        private void TaskRunner_OnProgressUpdate(int progress)
        {
            ThreadSafeUI.Run(this, delegate { progressBar.Value = Math.Min(progress, 100); });
        }

        private void TaskRunner_OnError(string error)
        {
            ThreadSafeUI.Run(this, delegate
            {
                _isExecuting = false;
                btnSend.Enabled = true;
                btnSend.Text = "\u50b3\u9001";
                progressBar.Value = 0;

                var errMsg = new ChatMessage("ai", "\u274c \u932f\u8aa4: " + error, DateTime.Now);
                _currentSession.Messages.Add(errMsg);
                AddBubbleToUI(errMsg);
                SaveSession(_currentSession);
                SetStatus("\u932f\u8aa4: " + error);
            });
        }

        // =======================================================
        // 程式碼按鈕
        // =======================================================
        private void BtnRunCode_Click(object sender, EventArgs e)
        {
            string code = rtbCode.Text.Trim();
            if (string.IsNullOrEmpty(code)) { SetStatus("無程式碼可執行"); return; }

            DialogResult dr = MessageBox.Show(
                "確定要編譯並執行此程式碼嗎？\n\n請確認程式碼內容安全無害。",
                "執行確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) return;

            // 開始編譯（支援自動修復）
            _isExecuting = true;
            btnSend.Enabled = false;
            btnRunCode.Enabled = false;
            btnRunCode.Text = "修復中...";

            Thread compileThread = new Thread(delegate()
            {
                AutoCompileAndFix(code);
            });
            compileThread.IsBackground = true;
            compileThread.Start();
        }

        /// <summary>
        /// 自動編譯 + 功能測試 + AI 除錯修復 + 經驗學習（最多嘗試 3 次）
        /// </summary>
        private void AutoCompileAndFix(string originalCode)
        {
            const int MAX_FIX_ATTEMPTS = 3;
            string currentCode = originalCode;
            CompileResult result = null;
            string userRequirement = "";

            // 取得使用者原始需求（從最近的 user 訊息中找）
            if (_currentSession != null)
            {
                for (int i = _currentSession.Messages.Count - 1; i >= 0; i--)
                {
                    if (_currentSession.Messages[i].Role == "user")
                    {
                        userRequirement = _currentSession.Messages[i].Content;
                        break;
                    }
                }
            }

            // 記錄原始碼（用於學習比較）
            string originalSourceForLesson = originalCode;
            List<string> allErrorsEncountered = new List<string>();

            for (int attempt = 0; attempt <= MAX_FIX_ATTEMPTS; attempt++)
            {
                int attemptNum = attempt;
                ThreadSafeUI.Run(this, delegate
                {
                    if (attemptNum == 0)
                        SetStatus("編譯中...");
                    else
                        SetStatus(string.Format("AI 修復第 {0} 次，重新編譯...", attemptNum));
                    progressBar.Value = attemptNum * 20;
                });

                // === 階段一：編譯 ===
                result = _automationService.CodeCompiler.Compile(currentCode);

                if (!result.Success)
                {
                    // 記錄錯誤（用於學習）
                    foreach (string err in result.Errors)
                    {
                        if (!allErrorsEncountered.Contains(err))
                            allErrorsEncountered.Add(err);
                    }

                    // 編譯失敗 → AI 修復
                    if (attempt >= MAX_FIX_ATTEMPTS) break;

                    int currentAttempt = attempt + 1;
                    List<string> errors = result.Errors;
                    ThreadSafeUI.Run(this, delegate
                    {
                        SetStatus(string.Format("編譯失敗（{0} 個錯誤），AI 修復中... ({1}/{2})",
                            errors.Count, currentAttempt, MAX_FIX_ATTEMPTS));
                        progressBar.Value = currentAttempt * 20;
                    });

                    string fixedCode = _automationService.CodeGenerator.FixCompileErrors(currentCode, errors);
                    if (string.IsNullOrEmpty(fixedCode) || fixedCode == currentCode) break;

                    currentCode = fixedCode;
                    Thread.Sleep(1000);
                    continue;
                }

                // === 階段二：功能測試 ===
                ThreadSafeUI.Run(this, delegate
                {
                    SetStatus("編譯成功，正在進行功能測試...");
                    progressBar.Value = 60;
                });

                CodeTestService testService = new CodeTestService(
                    _automationService.AiRouter, _automationService.CodeCompiler);
                List<TestCase> testResults = testService.AnalyzeAndGenerateTests(currentCode, userRequirement);

                // 統計結果
                List<TestCase> failedTests = new List<TestCase>();
                int passCount = 0;
                foreach (TestCase t in testResults)
                {
                    if (t.Result == TestResult.Fail)
                        failedTests.Add(t);
                    else if (t.Result == TestResult.Pass)
                        passCount++;
                }

                if (failedTests.Count == 0)
                {
                    // === 全部通過 → 交付 + 學習 ===
                    string finalCode = currentCode;
                    int fixCount = attemptNum;
                    int passed = passCount;
                    int totalTests = testResults.Count;

                    // ★★★ 經驗學習：記錄修復經驗 ★★★
                    if (fixCount > 0 && allErrorsEncountered.Count > 0)
                    {
                        try
                        {
                            // 記錄每個遇到的錯誤
                            foreach (string errLine in allErrorsEncountered)
                            {
                                // 提取錯誤碼（如 CS1525, CS1056 等）
                                Match errMatch = Regex.Match(errLine, @"(CS\d{4})");
                                string errCode = errMatch.Success ? errMatch.Groups[1].Value : "UNKNOWN";
                                _automationService.CodeGenerator.RecordFixLesson(
                                    errCode, errLine, originalSourceForLesson, finalCode);
                            }
                        }
                        catch { }
                    }

                    // ★★★ 技能學習：記錄成功技能 ★★★
                    try
                    {
                        List<string> fixedErrors = new List<string>();
                        foreach (string err in allErrorsEncountered)
                            fixedErrors.Add(err);

                        _automationService.CodeGenerator.RecordSkill(
                            userRequirement, finalCode, fixedErrors, fixCount);
                    }
                    catch { }

                    ThreadSafeUI.Run(this, delegate
                    {
                        _isExecuting = false;
                        btnSend.Enabled = true;
                        btnRunCode.Enabled = true;
                        btnRunCode.Text = "> 編譯執行";
                        progressBar.Value = 100;

                        if (fixCount > 0)
                            rtbCode.Text = finalCode;

                        StringBuilder msg = new StringBuilder();
                        if (fixCount > 0)
                            msg.AppendLine(string.Format("✅ 程式碼經過 AI 修復 {0} 次後編譯成功！", fixCount));
                        else
                            msg.AppendLine("✅ 編譯成功！");

                        msg.AppendLine(string.Format("🧪 功能測試：{0}/{1} 項通過", passed, totalTests));
                        msg.AppendLine("輸出: " + result.OutputPath);

                        // 顯示學習狀態
                        int lessonCount = _automationService.CodeGenerator.GetLessonCount();
                        string skillSummary = _automationService.CodeGenerator.GetSkillsSummary();
                        msg.AppendLine(string.Format("🧠 已累積 {0} 條修復經驗", lessonCount));
                        if (!string.IsNullOrEmpty(skillSummary))
                            msg.AppendLine("📚 " + skillSummary);

                        var okMsg = new ChatMessage("ai", msg.ToString(), DateTime.Now);
                        _currentSession.Messages.Add(okMsg);
                        AddBubbleToUI(okMsg);
                        SaveSession(_currentSession);
                        SetStatus("編譯成功，測試通過");
                        tabMain.SelectedTab = tabChat;
                    });

                    // 執行
                    string output = _automationService.CodeCompiler.ExecuteCompiled(result.OutputPath, 30000);
                    ThreadSafeUI.Run(this, delegate
                    {
                        var runMsg = new ChatMessage("ai",
                            "🔹 執行結果：\n" + output, DateTime.Now);
                        _currentSession.Messages.Add(runMsg);
                        AddBubbleToUI(runMsg);
                        SaveSession(_currentSession);
                        SetStatus("執行完成");
                        progressBar.Value = 0;
                    });
                    return;
                }
                else
                {
                    // === 有測試失敗 → AI 修復功能問題 ===
                    if (attempt >= MAX_FIX_ATTEMPTS) break;

                    int failCount = failedTests.Count;
                    int currentAttempt = attempt + 1;

                    ThreadSafeUI.Run(this, delegate
                    {
                        SetStatus(string.Format("功能測試發現 {0} 個問題，AI 修正中... ({1}/{2})",
                            failCount, currentAttempt, MAX_FIX_ATTEMPTS));
                        progressBar.Value = 70;

                        // 顯示測試結果
                        StringBuilder testMsg = new StringBuilder();
                        testMsg.AppendLine(string.Format("🧪 功能測試結果：{0} 通過，{1} 失敗",
                            passCount, failCount));
                        foreach (TestCase t in testResults)
                        {
                            if (t.Result == TestResult.Pass)
                                testMsg.AppendLine("  ✅ " + t.Name);
                            else
                                testMsg.AppendLine("  ❌ " + t.Name + "：" + t.FailReason);
                        }
                        var testInfoMsg = new ChatMessage("ai", testMsg.ToString(), DateTime.Now);
                        _currentSession.Messages.Add(testInfoMsg);
                        AddBubbleToUI(testInfoMsg);
                        SaveSession(_currentSession);
                    });

                    // 生成修復提示並讓 AI 修正
                    string fixPrompt = testService.GenerateFixPrompt(currentCode, failedTests);
                    List<string> fixErrors = new List<string>();
                    fixErrors.Add(fixPrompt);
                    string fixedCode = _automationService.CodeGenerator.FixCompileErrors(currentCode, fixErrors);
                    if (string.IsNullOrEmpty(fixedCode) || fixedCode == currentCode) break;

                    currentCode = fixedCode;
                    Thread.Sleep(1000);
                }
            }

            // === 最終失敗 ===
            CompileResult finalResult = result;
            ThreadSafeUI.Run(this, delegate
            {
                _isExecuting = false;
                btnSend.Enabled = true;
                btnRunCode.Enabled = true;
                btnRunCode.Text = "> 編譯執行";
                progressBar.Value = 0;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("❌ 修復失敗（已嘗試 " + MAX_FIX_ATTEMPTS + " 次）：");
                sb.AppendLine();
                if (finalResult != null && !finalResult.Success)
                {
                    foreach (string err in finalResult.Errors)
                        sb.AppendLine("  " + err);
                }
                else
                {
                    sb.AppendLine("  功能測試未通過，AI 無法自動修正");
                }
                sb.AppendLine();
                sb.AppendLine("💡 建議：在聊天中描述具體問題，例如「修正：數字鍵盤按了沒反應」");

                var errMsg = new ChatMessage("ai", sb.ToString(), DateTime.Now);
                _currentSession.Messages.Add(errMsg);
                AddBubbleToUI(errMsg);
                SaveSession(_currentSession);
                tabMain.SelectedTab = tabChat;
                SetStatus("修復失敗");
            });
        }

        private void BtnSaveCode_Click(object sender, EventArgs e)
        {
            string code = rtbCode.Text.Trim();
            if (string.IsNullOrEmpty(code)) { SetStatus("\u7121\u7a0b\u5f0f\u78bc\u53ef\u5132\u5b58"); return; }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "C# \u6a94\u6848 (*.cs)|*.cs|\u6240\u6709\u6a94\u6848 (*.*)|*.*";
            sfd.FileName = string.Format("Generated_{0:yyyyMMdd_HHmmss}.cs", DateTime.Now);
            sfd.InitialDirectory = _settings.DefaultSavePath;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, code, Encoding.UTF8);
                    SetStatus("\u5df2\u5132\u5b58: " + sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("\u5132\u5b58\u5931\u6557: " + ex.Message, "\u932f\u8aa4",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnCopyCode_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(rtbCode.Text))
            {
                try { Clipboard.SetText(rtbCode.Text); SetStatus("\u7a0b\u5f0f\u78bc\u5df2\u8907\u88fd"); }
                catch { SetStatus("\u8907\u88fd\u5931\u6557"); }
            }
        }

        // =======================================================
        // 聊天氣泡框
        // =======================================================
        private void RenderChat()
        {
            _isRendering = true;
            pnlChatInner.SuspendLayout();
            pnlChatInner.Controls.Clear();

            if (_currentSession != null)
            {
                foreach (var msg in _currentSession.Messages)
                    CreateBubbleControl(msg);
            }

            pnlChatInner.ResumeLayout(true);
            _isRendering = false;

            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = 60;
            t.Tick += delegate(object s, EventArgs ev)
            {
                t.Stop(); t.Dispose();
                pnlChatInner.AutoScrollPosition = new Point(0, pnlChatInner.DisplayRectangle.Height);
            };
            t.Start();
        }

        private void AddBubbleToUI(ChatMessage msg)
        {
            CreateBubbleControl(msg);

            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = 60;
            t.Tick += delegate(object s, EventArgs ev)
            {
                t.Stop(); t.Dispose();
                pnlChatInner.AutoScrollPosition = new Point(0, pnlChatInner.DisplayRectangle.Height);
            };
            t.Start();
        }

        private void CreateBubbleControl(ChatMessage msg)
        {
            int panelWidth = pnlChatInner.ClientSize.Width - 30;
            if (panelWidth < 300) panelWidth = 300;
            int maxWidth = panelWidth - 80;

            Panel row = new Panel();
            row.Width = panelWidth;
            row.BackColor = pnlChatInner.BackColor;
            row.Margin = new Padding(3, 4, 3, 4);

            RichTextBox rtb = new RichTextBox();
            rtb.ReadOnly = true;
            rtb.BorderStyle = BorderStyle.None;
            rtb.ScrollBars = RichTextBoxScrollBars.None;
            rtb.Font = new Font("Microsoft JhengHei UI", 10F);
            rtb.Text = msg.Content;
            rtb.Tag = msg;
            rtb.DetectUrls = false;
            rtb.WordWrap = true;
            rtb.Cursor = Cursors.IBeam;

            // 計算高度
            rtb.Width = Math.Min(maxWidth, panelWidth - 80);
            Size sz = TextRenderer.MeasureText(msg.Content, rtb.Font,
                new Size(rtb.Width - 20, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            rtb.Height = sz.Height + 24;

            // 右鍵選單
            ContextMenuStrip bubbleMenu = new ContextMenuStrip();
            ToolStripMenuItem miCopy = new ToolStripMenuItem("\u8907\u88fd\u5168\u90e8");
            miCopy.Click += delegate { try { Clipboard.SetText(msg.Content); } catch { } };
            ToolStripMenuItem miCopySelected = new ToolStripMenuItem("\u8907\u88fd\u9078\u53d6");
            miCopySelected.Click += delegate
            {
                if (!string.IsNullOrEmpty(rtb.SelectedText))
                    try { Clipboard.SetText(rtb.SelectedText); } catch { }
            };
            ToolStripMenuItem miDel = new ToolStripMenuItem("\u522a\u9664\u9019\u689d\u8a0a\u606f");
            miDel.Click += delegate
            {
                _currentSession.Messages.Remove(msg);
                SaveSession(_currentSession);
                RenderChat();
            };
            bubbleMenu.Items.Add(miCopySelected);
            bubbleMenu.Items.Add(miCopy);
            bubbleMenu.Items.Add(new ToolStripSeparator());
            bubbleMenu.Items.Add(miDel);
            rtb.ContextMenuStrip = bubbleMenu;

            if (msg.Role == "user")
            {
                rtb.BackColor = Color.FromArgb(0, 100, 180);
                rtb.ForeColor = Color.White;
                rtb.Location = new Point(row.Width - rtb.Width - 10, 5);
            }
            else
            {
                rtb.BackColor = Color.FromArgb(55, 55, 62);
                rtb.ForeColor = Color.FromArgb(225, 225, 225);
                rtb.Location = new Point(10, 5);
            }

            Label lblTime = new Label();
            lblTime.AutoSize = true;
            lblTime.Font = new Font("Microsoft JhengHei UI", 7.5F);
            lblTime.ForeColor = Color.FromArgb(120, 120, 130);
            lblTime.Text = msg.Time.ToString("HH:mm");
            if (msg.Role == "user")
                lblTime.Location = new Point(rtb.Right - 40, rtb.Bottom + 2);
            else
                lblTime.Location = new Point(rtb.Left, rtb.Bottom + 2);

            row.Height = rtb.Bottom + 25;
            row.Controls.Add(rtb);
            row.Controls.Add(lblTime);

            pnlChatInner.Controls.Add(row);
        }

        // =======================================================
        // Session
        // =======================================================
        private void StartNewSession()
        {
            _currentSession = new ChatSession();
            _currentSession.Id = Guid.NewGuid().ToString("N");
            _currentSession.Title = "\u65b0\u5c0d\u8a71 " + DateTime.Now.ToString("MM/dd HH:mm");
            _currentSession.CreatedAt = DateTime.Now;
            _currentSession.Messages = new List<ChatMessage>();
            _sessions.Insert(0, _currentSession);
            RefreshSessionList();
            lstSessions.SelectedIndex = 0;
            RenderChat();
            rtbCode.Clear();
            txtInput.Focus();
        }

        private void BtnNewChat_Click(object sender, EventArgs e) { StartNewSession(); }

        private void LstSessions_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstSessions.SelectedIndex < 0 || lstSessions.SelectedIndex >= _sessions.Count) return;
            _currentSession = _sessions[lstSessions.SelectedIndex];
            RenderChat();
        }

        private void RefreshSessionList()
        {
            lstSessions.BeginUpdate();
            int sel = lstSessions.SelectedIndex;
            lstSessions.Items.Clear();
            foreach (var s in _sessions)
                lstSessions.Items.Add(s.Title ?? "(\u7121\u6a19\u984c)");
            if (sel >= 0 && sel < lstSessions.Items.Count)
                lstSessions.SelectedIndex = sel;
            else if (_sessions.Count > 0)
                lstSessions.SelectedIndex = 0;
            lstSessions.EndUpdate();
        }

        // =======================================================
        // 存讀 JSON
        // =======================================================
        private void SaveSession(ChatSession session)
        {
            if (session == null) return;
            try
            {
                string filePath = Path.Combine(_sessionsDir, session.Id + ".json");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"id\": \"" + EscapeJson(session.Id) + "\",");
                sb.AppendLine("  \"title\": \"" + EscapeJson(session.Title) + "\",");
                sb.AppendLine("  \"createdAt\": \"" + session.CreatedAt.ToString("o") + "\",");
                sb.AppendLine("  \"messages\": [");
                for (int i = 0; i < session.Messages.Count; i++)
                {
                    var msg = session.Messages[i];
                    sb.Append("    {");
                    sb.Append("\"role\": \"" + EscapeJson(msg.Role) + "\", ");
                    sb.Append("\"content\": \"" + EscapeJson(msg.Content) + "\", ");
                    sb.Append("\"time\": \"" + msg.Time.ToString("o") + "\"");
                    sb.Append("}");
                    if (i < session.Messages.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  ]");
                sb.AppendLine("}");
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void LoadAllSessions()
        {
            _sessions.Clear();
            if (!Directory.Exists(_sessionsDir)) return;
            string[] files = Directory.GetFiles(_sessionsDir, "*.json");
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file, Encoding.UTF8);
                    ChatSession session = ParseSessionJson(json);
                    if (session != null) _sessions.Add(session);
                }
                catch { }
            }
            _sessions.Sort(delegate(ChatSession a, ChatSession b)
            {
                return b.CreatedAt.CompareTo(a.CreatedAt);
            });
        }

        private ChatSession ParseSessionJson(string json)
        {
            var session = new ChatSession();
            session.Id = ExtractJsonValue(json, "id");
            session.Title = ExtractJsonValue(json, "title");
            string created = ExtractJsonValue(json, "createdAt");
            DateTime dt;
            if (DateTime.TryParse(created, out dt)) session.CreatedAt = dt;
            else session.CreatedAt = DateTime.Now;
            session.Messages = new List<ChatMessage>();

            int arrStart = json.IndexOf("\"messages\"");
            if (arrStart < 0) return session;
            arrStart = json.IndexOf("[", arrStart);
            if (arrStart < 0) return session;
            int arrEnd = json.LastIndexOf("]");
            if (arrEnd <= arrStart) return session;
            string arrContent = json.Substring(arrStart + 1, arrEnd - arrStart - 1);

            int pos = 0;
            while (pos < arrContent.Length)
            {
                int objStart = arrContent.IndexOf("{", pos);
                if (objStart < 0) break;
                int objEnd = FindMatchingBrace(arrContent, objStart);
                if (objEnd < 0) break;
                string block = arrContent.Substring(objStart, objEnd - objStart + 1);
                string role = ExtractJsonValue(block, "role");
                string content = UnescapeJson(ExtractJsonValue(block, "content"));
                string timeStr = ExtractJsonValue(block, "time");
                DateTime msgTime;
                if (!DateTime.TryParse(timeStr, out msgTime)) msgTime = DateTime.Now;
                if (!string.IsNullOrEmpty(role))
                    session.Messages.Add(new ChatMessage(role, content, msgTime));
                pos = objEnd + 1;
            }
            return session;
        }

        private int FindMatchingBrace(string s, int openPos)
        {
            int depth = 0; bool inStr = false;
            for (int i = openPos; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && inStr) { i++; continue; }
                if (c == '"') { inStr = !inStr; continue; }
                if (inStr) continue;
                if (c == '{') depth++;
                if (c == '}') { depth--; if (depth == 0) return i; }
            }
            return -1;
        }

        // =======================================================
        // 右鍵選單
        // =======================================================
        private void SessionMenu_Rename(object sender, EventArgs e)
        {
            if (lstSessions.SelectedIndex < 0) return;
            var session = _sessions[lstSessions.SelectedIndex];
            Form inputForm = new Form();
            inputForm.Text = "\u91cd\u65b0\u547d\u540d";
            inputForm.Size = new Size(350, 130);
            inputForm.StartPosition = FormStartPosition.CenterParent;
            inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputForm.MaximizeBox = false; inputForm.MinimizeBox = false;
            TextBox tb = new TextBox(); tb.Text = session.Title;
            tb.Location = new Point(12, 15); tb.Size = new Size(310, 25);
            inputForm.Controls.Add(tb);
            Button btnOK = new Button(); btnOK.Text = "\u78ba\u5b9a";
            btnOK.DialogResult = DialogResult.OK; btnOK.Location = new Point(165, 55); btnOK.Size = new Size(75, 28);
            inputForm.Controls.Add(btnOK);
            Button btnCancel = new Button(); btnCancel.Text = "\u53d6\u6d88";
            btnCancel.DialogResult = DialogResult.Cancel; btnCancel.Location = new Point(247, 55); btnCancel.Size = new Size(75, 28);
            inputForm.Controls.Add(btnCancel);
            inputForm.AcceptButton = btnOK; inputForm.CancelButton = btnCancel;
            if (inputForm.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(tb.Text.Trim()))
            {
                session.Title = tb.Text.Trim();
                SaveSession(session); RefreshSessionList();
            }
        }

        private void SessionMenu_Delete(object sender, EventArgs e)
        {
            if (lstSessions.SelectedIndex < 0) return;
            var session = _sessions[lstSessions.SelectedIndex];
            if (MessageBox.Show("\u78ba\u5b9a\u522a\u9664\u5c0d\u8a71\u300c" + session.Title + "\u300d\uff1f",
                "\u78ba\u8a8d", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            string fp = Path.Combine(_sessionsDir, session.Id + ".json");
            if (File.Exists(fp)) File.Delete(fp);
            _sessions.Remove(session);
            if (_sessions.Count == 0) StartNewSession();
            else { _currentSession = _sessions[0]; RefreshSessionList(); lstSessions.SelectedIndex = 0; RenderChat(); }
        }

        private void SessionMenu_ClearAll(object sender, EventArgs e)
        {
            if (MessageBox.Show("\u78ba\u5b9a\u6e05\u9664\u6240\u6709\u5c0d\u8a71\uff1f\u7121\u6cd5\u5fa9\u539f\u3002",
                "\u78ba\u8a8d", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            foreach (var s in _sessions)
            { string fp = Path.Combine(_sessionsDir, s.Id + ".json"); if (File.Exists(fp)) File.Delete(fp); }
            _sessions.Clear(); StartNewSession();
        }

        // =======================================================
        // 設定 / 托盤 / 關閉
        // =======================================================
        private void BtnSettings_Click(object sender, EventArgs e)
        {
            using (SettingsForm sf = new SettingsForm(_settings))
            {
                if (sf.ShowDialog(this) == DialogResult.OK)
                {
                    _settings = sf.GetSettings(); _settings.Save();
                    _automationService = new TaskAutomationService(_settings);
                    SetStatus("\u8a2d\u5b9a\u5df2\u5132\u5b58");
                }
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized && _settings.MinimizeToTray)
            { this.Hide(); trayIcon.Visible = true; }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e) { RestoreFromTray(); }
        private void TrayMenu_Show(object sender, EventArgs e) { RestoreFromTray(); }
        private void TrayMenu_Exit(object sender, EventArgs e)
        { trayIcon.Visible = false; _taskRunner.Stop(); Application.Exit(); }

        private void RestoreFromTray()
        { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); trayIcon.Visible = false; }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isExecuting)
            {
                if (MessageBox.Show("\u4efb\u52d9\u57f7\u884c\u4e2d\uff0c\u78ba\u5b9a\u95dc\u9589\uff1f",
                    "\u78ba\u8a8d", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                { e.Cancel = true; return; }
            }
            _taskRunner.Stop(); trayIcon.Visible = false;
        }

        // =======================================================
        // 工具
        // =======================================================
        private void SetStatus(string text) { lblStatus.Text = text; }

        private string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", "").Replace("\n", " ");
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }

        private string ExtractJsonValue(string json, string key)
        {
            string pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"";
            Match m = Regex.Match(json, pattern);
            return m.Success ? m.Groups[1].Value : "";
        }

        private string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private string UnescapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\n", "\n").Replace("\\r", "\r")
                    .Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private void QuickAction(string prefix)
        {
            txtInput.Text = prefix;
            txtInput.SelectionStart = txtInput.Text.Length;
            txtInput.Focus();
        }

        private string BuildContextQuery(string currentInput)
        {
            if (_currentSession == null || _currentSession.Messages.Count == 0)
                return currentInput;

            List<ChatMessage> recent = _currentSession.Messages;
            int startIndex = 0;
            if (recent.Count > 20)
                startIndex = recent.Count - 20;

            StringBuilder context = new StringBuilder();
            context.AppendLine("[CONTEXT]");

            for (int i = startIndex; i < recent.Count; i++)
            {
                ChatMessage msg = recent[i];
                if (msg.Role == "user")
                    context.AppendLine("USER: " + Truncate(msg.Content, 300));
                else
                    context.AppendLine("AI: " + Truncate(msg.Content, 300));
            }

            context.AppendLine("[/CONTEXT]");
            context.AppendLine("[CURRENT] " + currentInput);

            return context.ToString();
        }
    }

    public class ChatSession
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }

    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public DateTime Time { get; set; }
        public ChatMessage(string role, string content, DateTime time)
        { Role = role; Content = content; Time = time; }
    }
}
