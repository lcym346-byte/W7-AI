using System.Windows.Forms;
using System.Drawing;

namespace AIAgentTool
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // \u9802\u90e8
        private Panel pnlTop;
        private Label lblTitle;
        private Button btnSettings;
        private Button btnNewChat;

        // \u5de6\u5074\u5c0d\u8a71\u5217\u8868
        private Panel pnlSidebar;
        private ListBox lstSessions;
        private Label lblSessions;
        private ContextMenuStrip sessionMenu;

        // \u804a\u5929\u5340
        private Panel pnlChat;
        private Panel pnlChatInner;

        // \u8f38\u5165\u5340
        private Panel pnlInput;
        private TextBox txtInput;
        private Button btnSend;

        // \u72c0\u614b\u5217
        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;
        private ToolStripProgressBar progressBar;

        // \u7cfb\u7d71\u6258\u76e4
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

            this.Text = "AI \u667a\u6167\u4ee3\u7406\u5de5\u5177 v2.0";
            this.Size = new Size(950, 650);
            this.MinimumSize = new Size(700, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.FromArgb(220, 220, 220);
            this.Font = new Font("Microsoft JhengHei UI", 9.5F);
            this.KeyPreview = true;

            // ===================== \u9802\u90e8\u9762\u677f =====================
            pnlTop = new Panel();
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 48;
            pnlTop.BackColor = Color.FromArgb(38, 38, 42);
            pnlTop.Padding = new Padding(12, 8, 12, 8);

            lblTitle = new Label();
            lblTitle.Text = "AI Agent";
            lblTitle.Font = new Font("Microsoft JhengHei UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(100, 180, 255);
            lblTitle.Location = new Point(14, 10);
            lblTitle.AutoSize = true;

            btnNewChat = new Button();
            btnNewChat.Text = "+ \u65b0\u5c0d\u8a71";
            btnNewChat.FlatStyle = FlatStyle.Flat;
            btnNewChat.FlatAppearance.BorderSize = 1;
            btnNewChat.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 90);
            btnNewChat.BackColor = Color.FromArgb(50, 50, 58);
            btnNewChat.ForeColor = Color.White;
            btnNewChat.Font = new Font("Microsoft JhengHei UI", 9F);
            btnNewChat.Size = new Size(80, 30);
            btnNewChat.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNewChat.Location = new Point(750, 9);
            btnNewChat.Cursor = Cursors.Hand;

            btnSettings = new Button();
            btnSettings.Text = "\u2699 \u8a2d\u5b9a";
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.FlatAppearance.BorderSize = 1;
            btnSettings.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 90);
            btnSettings.BackColor = Color.FromArgb(50, 50, 58);
            btnSettings.ForeColor = Color.White;
            btnSettings.Font = new Font("Microsoft JhengHei UI", 9F);
            btnSettings.Size = new Size(70, 30);
            btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSettings.Location = new Point(840, 9);
            btnSettings.Cursor = Cursors.Hand;

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(btnNewChat);
            pnlTop.Controls.Add(btnSettings);

            // ===================== \u5de6\u5074\u5c0d\u8a71\u5217\u8868 =====================
            pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 200;
            pnlSidebar.BackColor = Color.FromArgb(28, 28, 32);
            pnlSidebar.Padding = new Padding(6);

            lblSessions = new Label();
            lblSessions.Text = "\u5c0d\u8a71\u7d00\u9304";
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
            sessionMenu.Items.Add("\u91cd\u65b0\u547d\u540d", null);
            sessionMenu.Items.Add("\u522a\u9664\u6b64\u5c0d\u8a71", null);
            sessionMenu.Items.Add("-");
            sessionMenu.Items.Add("\u6e05\u9664\u6240\u6709\u5c0d\u8a71", null);
            lstSessions.ContextMenuStrip = sessionMenu;

            pnlSidebar.Controls.Add(lstSessions);
            pnlSidebar.Controls.Add(lblSessions);

            // ===================== \u804a\u5929\u5340\u57df =====================
            pnlChat = new Panel();
            pnlChat.Dock = DockStyle.Fill;
            pnlChat.BackColor = Color.FromArgb(32, 32, 36);
            pnlChat.Padding = new Padding(0);

            pnlChatInner = new Panel();
            pnlChatInner.Dock = DockStyle.Fill;
            pnlChatInner.AutoScroll = true;
            pnlChatInner.BackColor = Color.FromArgb(32, 32, 36);

            pnlChat.Controls.Add(pnlChatInner);

            // ===================== \u8f38\u5165\u5340\u57df =====================
            pnlInput = new Panel();
            pnlInput.Dock = DockStyle.Bottom;
            pnlInput.Height = 65;
            pnlInput.BackColor = Color.FromArgb(40, 40, 45);
            pnlInput.Padding = new Padding(12, 10, 12, 10);

            txtInput = new TextBox();
            txtInput.Multiline = true;
            txtInput.Dock = DockStyle.Fill;
            txtInput.BackColor = Color.FromArgb(50, 50, 56);
            txtInput.ForeColor = Color.White;
            txtInput.Font = new Font("Microsoft JhengHei UI", 11F);
            txtInput.BorderStyle = BorderStyle.FixedSingle;
            txtInput.ScrollBars = ScrollBars.Vertical;

            btnSend = new Button();
            btnSend.Text = "\u50b3\u9001";
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

            // ===================== \u72c0\u614b\u5217 =====================
            statusBar = new StatusStrip();
            statusBar.BackColor = Color.FromArgb(35, 35, 42);
            statusBar.ForeColor = Color.FromArgb(160, 160, 170);

            lblStatus = new ToolStripStatusLabel();
            lblStatus.Text = "\u5c31\u7dd2";
            lblStatus.Spring = true;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            progressBar = new ToolStripProgressBar();
            progressBar.Size = new Size(180, 16);
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;

            statusBar.Items.Add(lblStatus);
            statusBar.Items.Add(progressBar);

            // ===================== \u7cfb\u7d71\u6258\u76e4 =====================
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("\u986f\u793a\u4e3b\u8996\u7a97", null);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("\u7d50\u675f\u7a0b\u5f0f", null);

            trayIcon = new NotifyIcon(this.components);
            trayIcon.Text = "AI \u667a\u6167\u4ee3\u7406\u5de5\u5177";
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = false;

            try { trayIcon.Icon = System.Drawing.SystemIcons.Application; }
            catch { }

            // ===================== \u52a0\u5165\u4e3b\u8996\u7a97 =====================
            this.Controls.Add(pnlChat);
            this.Controls.Add(pnlInput);
            this.Controls.Add(pnlSidebar);
            this.Controls.Add(statusBar);
            this.Controls.Add(pnlTop);

            this.ResumeLayout(false);
        }
    }
}
