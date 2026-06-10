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

            string contextQuery = BuildContextQuery(text);
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
            if (string.IsNullOrEmpty(code)) { SetStatus("\u7121\u7a0b\u5f0f\u78bc\u53ef\u57f7\u884c"); return; }

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

                var resultMsg = new ChatMessage("ai",
                    "\u2705 \u7de8\u8b6f\u57f7\u884c\u6210\u529f\uff01\n\n\u3010\u57f7\u884c\u8f38\u51fa\u3011\n" + output, DateTime.Now);
                _currentSession.Messages.Add(resultMsg);
                AddBubbleToUI(resultMsg);
                SaveSession(_currentSession);
                tabMain.SelectedTab = tabChat;
                SetStatus("\u57f7\u884c\u5b8c\u6210");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("\u274c \u7de8\u8b6f\u5931\u6557\uff1a");
                foreach (string err in result.Errors)
                    sb.AppendLine("  " + err);

                var errMsg = new ChatMessage("ai", sb.ToString(), DateTime.Now);
                _currentSession.Messages.Add(errMsg);
                AddBubbleToUI(errMsg);
                SaveSession(_currentSession);
                tabMain.SelectedTab = tabChat;
                SetStatus("\u7de8\u8b6f\u5931\u6557");
            }
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

            Label lbl = new Label();
            lbl.AutoSize = false;
            lbl.MaximumSize = new Size(maxWidth, 0);
            lbl.AutoSize = true;
            lbl.Font = new Font("Microsoft JhengHei UI", 10F);
            lbl.Padding = new Padding(10, 8, 10, 8);
            lbl.Text = msg.Content;
            lbl.Tag = msg;

            ContextMenuStrip bubbleMenu = new ContextMenuStrip();
            ToolStripMenuItem miCopy = new ToolStripMenuItem("\u8907\u88fd");
            miCopy.Click += delegate { try { Clipboard.SetText(msg.Content); } catch { } };
            ToolStripMenuItem miDel = new ToolStripMenuItem("\u522a\u9664\u9019\u689d\u8a0a\u606f");
            miDel.Click += delegate
            {
                _currentSession.Messages.Remove(msg);
                SaveSession(_currentSession);
                RenderChat();
            };
            bubbleMenu.Items.Add(miCopy);
            bubbleMenu.Items.Add(miDel);
            lbl.ContextMenuStrip = bubbleMenu;

            Size preferred = lbl.GetPreferredSize(new Size(maxWidth, 0));
            lbl.Size = new Size(preferred.Width + 20, preferred.Height + 16);

            if (msg.Role == "user")
            {
                lbl.BackColor = Color.FromArgb(0, 100, 180);
                lbl.ForeColor = Color.White;
                lbl.Location = new Point(row.Width - lbl.Width - 10, 5);
            }
            else
            {
                lbl.BackColor = Color.FromArgb(55, 55, 62);
                lbl.ForeColor = Color.FromArgb(225, 225, 225);
                lbl.Location = new Point(10, 5);
            }

            Label lblTime = new Label();
            lblTime.AutoSize = true;
            lblTime.Font = new Font("Microsoft JhengHei UI", 7.5F);
            lblTime.ForeColor = Color.FromArgb(120, 120, 130);
            lblTime.Text = msg.Time.ToString("HH:mm");
            if (msg.Role == "user")
                lblTime.Location = new Point(lbl.Right - 40, lbl.Bottom + 2);
            else
                lblTime.Location = new Point(lbl.Left, lbl.Bottom + 2);

            row.Height = lbl.Bottom + 25;
            row.Controls.Add(lbl);
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
            _sessions.Sort(delegate(ChatSession a, ChatSession b) { return b.CreatedAt.CompareTo(a.CreatedAt); });
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
