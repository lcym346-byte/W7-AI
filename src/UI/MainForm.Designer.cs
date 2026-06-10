using System.Windows.Forms;
using System.Drawing;

namespace AIAgentTool
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // 頂部
        private Panel pnlTop;
        private Label lblTitle;
        private Button btnSettings;
        private Button btnNewChat;

        // 左側
        private Panel pnlSidebar;
        private ListBox lstSessions;
        private Label lblSessions;
        private ContextMenuStrip sessionMenu;

        // 中間主區 - 分頁
        private TabControl tabMain;
        private TabPage tabChat;
        private TabPage tabCode;

        // 聊天區
        private FlowLayoutPanel pnlChatInner;

        // 功能選單列
        private Panel pnlQuickMenu;
        private Button btnMenuImage;
        private Button btnMenuVideo;
        private Button btnMenuTTS;
        private Button btnMenuScreenshot;
        private Button btnMenuWindows;
        private Button btnMenuKnowledge;
        private Button btnMenuLaunch;
        private Button btnMenuCmd;

        // 輸入區
        private Panel pnlInput;
        private TextBox txtInput;
        private Button btnSend;

        // 程式碼區
        private RichTextBox rtbCode;
        private Panel pnlCodeButtons;
        private Button btnRunCode;
        private Button btnSaveCode;
        private Button btnCopyCode;

        // 狀態列
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;
        private ToolStripProgressBar progressBar;

        // 系統托盤
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            if (disposing && trayIcon != null)
                trayIcon.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();

            this.Text = "AI 智慧代理工具 v2.0";
            this.Size = new Size(950, 700);
            this.MinimumSize = new Size(700, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.FromArgb(220, 220, 220);
            this.Font = new Font("Microsoft JhengHei UI", 9.5F);
            this.KeyPreview = true;

            // ===================== 頂部 =====================
            pnlTop = new Panel();
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 48;
            pnlTop.BackColor = Color.FromArgb(38, 38, 42);

            lblTitle = new Label();
            lblTitle.Text = "AI Agent";
            lblTitle.Font = new Font("Microsoft JhengHei UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(100, 180, 255);
            lblTitle.Dock = DockStyle.Left;
            lblTitle.AutoSize = false;
            lblTitle.Width = 130;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.Padding = new Padding(12, 0, 0, 0);

            btnSettings = new Button();
            btnSettings.Text = "\u2699 設定";
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.FlatAppearance.BorderSize = 1;
            btnSettings.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 90);
            btnSettings.BackColor = Color.FromArgb(50, 50, 58);
            btnSettings.ForeColor = Color.White;
            btnSettings.Font = new Font("Microsoft JhengHei UI", 9F);
            btnSettings.Dock = DockStyle.Right;
            btnSettings.Width = 70;
            btnSettings.Cursor = Cursors.Hand;

            btnNewChat = new Button();
            btnNewChat.Text = "+ 新對話";
            btnNewChat.FlatStyle = FlatStyle.Flat;
            btnNewChat.FlatAppearance.BorderSize = 1;
            btnNewChat.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 90);
            btnNewChat.BackColor = Color.FromArgb(50, 50, 58);
            btnNewChat.ForeColor = Color.White;
            btnNewChat.Font = new Font("Microsoft JhengHei UI", 9F);
            btnNewChat.Dock = DockStyle.Right;
            btnNewChat.Width = 80;
            btnNewChat.Cursor = Cursors.Hand;

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(btnNewChat);
            pnlTop.Controls.Add(btnSettings);

            // ===================== 左側 =====================
            pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 200;
            pnlSidebar.BackColor = Color.FromArgb(28, 28, 32);
            pnlSidebar.Padding = new Padding(6);

            lblSessions = new Label();
            lblSessions.Text = "對話紀錄";
            lblSessions.Dock = DockStyle.Top;
            lblSessions.Height = 28;
            lblSessions.ForeColor = Color.FromArgb(160, 160, 170);
            lblSessions.Font = new Font("Microsoft JhengHei UI", 9.5F, FontStyle.Bold);
            lblSessions.TextAlign = ContentAlignment.MiddleLeft;
            lblSessions.Padding = new Padding(4, 0, 0, 0);

            lstSessions = new ListBox();
            lstSessions.Dock = DockStyle.Fill;
            lstSessions.BackColor = Color.FromArgb(32, 32, 38);
            lstSessions.ForeColor = Color.FromArgb(200, 200, 210);
            lstSessions.BorderStyle = BorderStyle.None;
            lstSessions.Font = new Font("Microsoft JhengHei UI", 9F);

            sessionMenu = new ContextMenuStrip();
            sessionMenu.Items.Add("重新命名", null);
            sessionMenu.Items.Add("刪除此對話", null);
            sessionMenu.Items.Add("-");
            sessionMenu.Items.Add("清除所有對話", null);
            lstSessions.ContextMenuStrip = sessionMenu;

            pnlSidebar.Controls.Add(lstSessions);
            pnlSidebar.Controls.Add(lblSessions);

            // ===================== 主區域分頁 =====================
            tabMain = new TabControl();
            tabMain.Dock = DockStyle.Fill;
            tabMain.Font = new Font("Microsoft JhengHei UI", 9.5F);
            tabMain.BackColor = Color.FromArgb(32, 32, 36);

            // --- 聊天分頁 ---
            tabChat = new TabPage("聊天");
            tabChat.BackColor = Color.FromArgb(32, 32, 36);

            pnlChatInner = new FlowLayoutPanel();
            pnlChatInner.Dock = DockStyle.Fill;
            pnlChatInner.AutoScroll = true;
            pnlChatInner.FlowDirection = FlowDirection.TopDown;
            pnlChatInner.WrapContents = false;
            pnlChatInner.BackColor = Color.FromArgb(32, 32, 36);

            // ===================== 功能快捷選單 =====================
            pnlQuickMenu = new Panel();
            pnlQuickMenu.Dock = DockStyle.Bottom;
            pnlQuickMenu.Height = 40;
            pnlQuickMenu.BackColor = Color.FromArgb(35, 35, 42);
            pnlQuickMenu.Padding = new Padding(6, 4, 6, 4);

            btnMenuImage = CreateMenuButton("\U0001F3A8 生成圖片", 0);
            btnMenuVideo = CreateMenuButton("\U0001F3AC 生成影片", 1);
            btnMenuTTS = CreateMenuButton("\U0001F50A 語音", 2);
            btnMenuScreenshot = CreateMenuButton("\U0001F4F7 截圖", 3);
            btnMenuWindows = CreateMenuButton("\U0001F5D4 視窗", 4);
            btnMenuKnowledge = CreateMenuButton("\U0001F4DA 知識庫", 5);
            btnMenuLaunch = CreateMenuButton("\U0001F680 開程式", 6);
            btnMenuCmd = CreateMenuButton("\U0001F4BB CMD", 7);

            pnlQuickMenu.Controls.Add(btnMenuImage);
            pnlQuickMenu.Controls.Add(btnMenuVideo);
            pnlQuickMenu.Controls.Add(btnMenuTTS);
            pnlQuickMenu.Controls.Add(btnMenuScreenshot);
            pnlQuickMenu.Controls.Add(btnMenuWindows);
            pnlQuickMenu.Controls.Add(btnMenuKnowledge);
            pnlQuickMenu.Controls.Add(btnMenuLaunch);
            pnlQuickMenu.Controls.Add(btnMenuCmd);

            // ===================== 輸入區 =====================
            pnlInput = new Panel();
            pnlInput.Dock = DockStyle.Bottom;
            pnlInput.Height = 65;
            pnlInput.BackColor = Color.FromArgb(40, 40, 45);
            pnlInput.Padding = new Padding(10, 8, 10, 8);

            txtInput = new TextBox();
            txtInput.Multiline = true;
            txtInput.Dock = DockStyle.Fill;
            txtInput.BackColor = Color.FromArgb(50, 50, 56);
            txtInput.ForeColor = Color.White;
            txtInput.Font = new Font("Microsoft JhengHei UI", 11F);
            txtInput.BorderStyle = BorderStyle.FixedSingle;
            txtInput.ScrollBars = ScrollBars.Vertical;

            btnSend = new Button();
            btnSend.Text = "傳送";
            btnSend.Dock = DockStyle.Right;
            btnSend.Width = 70;
            btnSend.FlatStyle = FlatStyle.Flat;
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.BackColor = Color.FromArgb(0, 120, 210);
            btnSend.ForeColor = Color.White;
            btnSend.Font = new Font("Microsoft JhengHei UI", 10.5F, FontStyle.Bold);
            btnSend.Cursor = Cursors.Hand;

            pnlInput.Controls.Add(txtInput);
            pnlInput.Controls.Add(btnSend);

            // 聊天分頁組裝順序（Bottom 先加）
            tabChat.Controls.Add(pnlChatInner);
            tabChat.Controls.Add(pnlQuickMenu);
            tabChat.Controls.Add(pnlInput);

            // --- 程式碼分頁 ---
            tabCode = new TabPage("程式碼");
            tabCode.BackColor = Color.FromArgb(25, 25, 30);

            rtbCode = new RichTextBox();
            rtbCode.Dock = DockStyle.Fill;
            rtbCode.BackColor = Color.FromArgb(20, 20, 25);
            rtbCode.ForeColor = Color.FromArgb(200, 220, 255);
            rtbCode.Font = new Font("Consolas", 10F);
            rtbCode.BorderStyle = BorderStyle.None;
            rtbCode.WordWrap = false;
            rtbCode.AcceptsTab = true;

            pnlCodeButtons = new Panel();
            pnlCodeButtons.Dock = DockStyle.Bottom;
            pnlCodeButtons.Height = 38;
            pnlCodeButtons.BackColor = Color.FromArgb(35, 35, 42);

            btnRunCode = new Button();
            btnRunCode.Text = "> 編譯執行";
            btnRunCode.Location = new Point(8, 5);
            btnRunCode.Size = new Size(90, 28);
            btnRunCode.FlatStyle = FlatStyle.Flat;
            btnRunCode.FlatAppearance.BorderSize = 0;
            btnRunCode.BackColor = Color.FromArgb(50, 120, 50);
            btnRunCode.ForeColor = Color.White;
            btnRunCode.Cursor = Cursors.Hand;

            btnSaveCode = new Button();
            btnSaveCode.Text = "儲存";
            btnSaveCode.Location = new Point(105, 5);
            btnSaveCode.Size = new Size(70, 28);
            btnSaveCode.FlatStyle = FlatStyle.Flat;
            btnSaveCode.FlatAppearance.BorderSize = 0;
            btnSaveCode.BackColor = Color.FromArgb(60, 60, 70);
            btnSaveCode.ForeColor = Color.White;
            btnSaveCode.Cursor = Cursors.Hand;

            btnCopyCode = new Button();
            btnCopyCode.Text = "複製";
            btnCopyCode.Location = new Point(182, 5);
            btnCopyCode.Size = new Size(70, 28);
            btnCopyCode.FlatStyle = FlatStyle.Flat;
            btnCopyCode.FlatAppearance.BorderSize = 0;
            btnCopyCode.BackColor = Color.FromArgb(60, 60, 70);
            btnCopyCode.ForeColor = Color.White;
            btnCopyCode.Cursor = Cursors.Hand;

            pnlCodeButtons.Controls.Add(btnRunCode);
            pnlCodeButtons.Controls.Add(btnSaveCode);
            pnlCodeButtons.Controls.Add(btnCopyCode);

            tabCode.Controls.Add(rtbCode);
            tabCode.Controls.Add(pnlCodeButtons);

            tabMain.TabPages.Add(tabChat);
            tabMain.TabPages.Add(tabCode);

            // ===================== 狀態列 =====================
            statusBar = new StatusStrip();
            statusBar.BackColor = Color.FromArgb(35, 35, 42);
            statusBar.ForeColor = Color.FromArgb(160, 160, 170);

            lblStatus = new ToolStripStatusLabel();
            lblStatus.Text = "就緒";
            lblStatus.Spring = true;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            progressBar = new ToolStripProgressBar();
            progressBar.Size = new Size(180, 16);
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;

            statusBar.Items.Add(lblStatus);
            statusBar.Items.Add(progressBar);

            // ===================== 托盤 =====================
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("顯示主視窗", null);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("結束程式", null);

            trayIcon = new NotifyIcon(this.components);
            trayIcon.Text = "AI 智慧代理工具";
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = false;
            try { trayIcon.Icon = System.Drawing.SystemIcons.Application; } catch { }

            // ===================== 加入主視窗 =====================
            this.Controls.Add(tabMain);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(statusBar);
            this.Controls.Add(pnlTop);

            this.ResumeLayout(false);
        }

        /// <summary>
        /// 建立功能選單按鈕
        /// </summary>
        private Button CreateMenuButton(string text, int index)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(82, 32);
            btn.Location = new Point(6 + index * 84, 4);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 70);
            btn.BackColor = Color.FromArgb(45, 45, 55);
            btn.ForeColor = Color.FromArgb(200, 200, 210);
            btn.Font = new Font("Microsoft JhengHei UI", 8F);
            btn.Cursor = Cursors.Hand;
            btn.Tag = index;
            return btn;
        }
    }
}
