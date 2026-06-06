using System.Windows.Forms;
using System.Drawing;

namespace AIAgentTool
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel pnlTop;
        private Label lblTitle;
        private ComboBox cboTaskType;
        private TextBox txtQuery;
        private Button btnExecute;
        private Button btnClear;
        private Button btnSettings;

        private Panel pnlQuickButtons;
        private Panel pnlLeft;
        private ListBox lstHistory;
        private Label lblHistory;

        private StatusStrip statusBar;
        private ToolStripStatusLabel lblStatus;
        private ToolStripProgressBar progressBar;
        private ToolStripStatusLabel lblQueue;

        private TabControl tabMain;
        private TabPage tabResult;
        private TabPage tabSearchResults;
        private TabPage tabSteps;
        private TabPage tabCode;
        private TabPage tabCache;

        private RichTextBox rtbResult;
        private DataGridView dgvSearchResults;
        private RichTextBox rtbSteps;
        private RichTextBox rtbCode;
        private DataGridView dgvCache;

        private Button btnExport;
        private Button btnRunCode;
        private Button btnSaveCode;
        private Button btnCopyResult;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing && trayIcon != null)
            {
                trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();

            this.Text = "AI 智慧代理工具 v1.0";
            this.Size = new Size(1100, 720);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.FromArgb(220, 220, 220);
            this.Font = new Font("Microsoft JhengHei UI", 9F);
            this.KeyPreview = true;

            // ===================== 頂部面板 =====================
            pnlTop = new Panel();
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 95;
            pnlTop.BackColor = Color.FromArgb(40, 40, 45);
            pnlTop.Padding = new Padding(10, 8, 10, 5);

            lblTitle = new Label();
            lblTitle.Text = "[AI] 智慧代理工具";
            lblTitle.Font = new Font("Microsoft JhengHei UI", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(100, 180, 255);
            lblTitle.Location = new Point(12, 8);
            lblTitle.AutoSize = true;

            cboTaskType = new ComboBox();
            cboTaskType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTaskType.Location = new Point(12, 40);
            cboTaskType.Size = new Size(130, 25);
            cboTaskType.BackColor = Color.FromArgb(50, 50, 55);
            cboTaskType.ForeColor = Color.White;
            cboTaskType.Items.AddRange(new object[] {
                "自動判斷", "搜尋", "深入研究", "摘要", "比較",
                "計算", "系統資訊", "開啟程式", "關閉程式",
                "程序列表", "檔案管理", "CMD命令", "截圖",
                "剪貼簿", "已安裝程式", "產生程式碼", "批次執行"
            });
            cboTaskType.SelectedIndex = 0;

            txtQuery = new TextBox();
            txtQuery.Location = new Point(150, 40);
            txtQuery.Size = new Size(580, 25);
            txtQuery.BackColor = Color.FromArgb(50, 50, 55);
            txtQuery.ForeColor = Color.White;
            txtQuery.Font = new Font("Microsoft JhengHei UI", 10F);
            txtQuery.BorderStyle = BorderStyle.FixedSingle;

            btnExecute = new Button();
            btnExecute.Text = "> 執行";
            btnExecute.Location = new Point(740, 38);
            btnExecute.Size = new Size(80, 28);
            btnExecute.BackColor = Color.FromArgb(60, 140, 60);
            btnExecute.ForeColor = Color.White;
            btnExecute.FlatStyle = FlatStyle.Flat;
            btnExecute.FlatAppearance.BorderSize = 0;
            btnExecute.Cursor = Cursors.Hand;

            btnClear = new Button();
            btnClear.Text = "X 清除";
            btnClear.Location = new Point(825, 38);
            btnClear.Size = new Size(70, 28);
            btnClear.BackColor = Color.FromArgb(140, 60, 60);
            btnClear.ForeColor = Color.White;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Cursor = Cursors.Hand;

            btnSettings = new Button();
            btnSettings.Text = "| 設定";
            btnSettings.Location = new Point(900, 38);
            btnSettings.Size = new Size(70, 28);
            btnSettings.BackColor = Color.FromArgb(80, 80, 90);
            btnSettings.ForeColor = Color.White;
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.Cursor = Cursors.Hand;

            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(cboTaskType);
            pnlTop.Controls.Add(txtQuery);
            pnlTop.Controls.Add(btnExecute);
            pnlTop.Controls.Add(btnClear);
            pnlTop.Controls.Add(btnSettings);

            // ===================== 快捷按鈕面板 =====================
            pnlQuickButtons = new Panel();
            pnlQuickButtons.Dock = DockStyle.Top;
            pnlQuickButtons.Height = 38;
            pnlQuickButtons.BackColor = Color.FromArgb(35, 35, 40);
            pnlQuickButtons.Padding = new Padding(5, 5, 5, 5);

            string[] quickLabels = new string[] {
                "搜尋", "研究", "摘要", "比較", "計算",
                "系統", "檔案", "開啟", "關閉", "程序",
                "CMD", "截圖", "剪貼簿", "程式碼", "批次"
            };

            int qbX = 5;
            for (int i = 0; i < quickLabels.Length; i++)
            {
                Button qb = new Button();
                qb.Text = quickLabels[i];
                qb.Size = new Size(60, 26);
                qb.Location = new Point(qbX, 5);
                qb.FlatStyle = FlatStyle.Flat;
                qb.FlatAppearance.BorderSize = 1;
                qb.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 80);
                qb.BackColor = Color.FromArgb(50, 50, 58);
                qb.ForeColor = Color.FromArgb(200, 200, 210);
                qb.Font = new Font("Microsoft JhengHei UI", 8F);
                qb.Cursor = Cursors.Hand;
                qb.Tag = i;
                qb.Click += QuickButton_Click;
                qb.MouseEnter += QuickButton_MouseEnter;
                qb.MouseLeave += QuickButton_MouseLeave;
                pnlQuickButtons.Controls.Add(qb);
                qbX += 63;
            }

            // ===================== 左側歷史面板 =====================
            pnlLeft = new Panel();
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Width = 200;
            pnlLeft.BackColor = Color.FromArgb(35, 35, 40);
            pnlLeft.Padding = new Padding(5);

            lblHistory = new Label();
            lblHistory.Text = "# 任務歷史";
            lblHistory.Dock = DockStyle.Top;
            lblHistory.Height = 25;
            lblHistory.ForeColor = Color.FromArgb(180, 180, 190);
            lblHistory.Font = new Font("Microsoft JhengHei UI", 9F, FontStyle.Bold);
            lblHistory.TextAlign = ContentAlignment.MiddleLeft;

            lstHistory = new ListBox();
            lstHistory.Dock = DockStyle.Fill;
            lstHistory.BackColor = Color.FromArgb(40, 40, 48);
            lstHistory.ForeColor = Color.FromArgb(200, 200, 210);
            lstHistory.BorderStyle = BorderStyle.None;
            lstHistory.Font = new Font("Microsoft JhengHei UI", 8.5F);

            pnlLeft.Controls.Add(lstHistory);
            pnlLeft.Controls.Add(lblHistory);

            // ===================== 狀態列 =====================
            statusBar = new StatusStrip();
            statusBar.BackColor = Color.FromArgb(35, 35, 42);
            statusBar.ForeColor = Color.FromArgb(180, 180, 190);

            lblStatus = new ToolStripStatusLabel();
            lblStatus.Text = "就緒";
            lblStatus.Spring = true;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            progressBar = new ToolStripProgressBar();
            progressBar.Size = new Size(200, 16);
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;

            lblQueue = new ToolStripStatusLabel();
            lblQueue.Text = "佇列: 0";

            statusBar.Items.Add(lblStatus);
            statusBar.Items.Add(progressBar);
            statusBar.Items.Add(lblQueue);

            // ===================== 主分頁控件 =====================
            tabMain = new TabControl();
            tabMain.Dock = DockStyle.Fill;
            tabMain.BackColor = Color.FromArgb(35, 35, 40);
            tabMain.ForeColor = Color.FromArgb(220, 220, 220);
            tabMain.Font = new Font("Microsoft JhengHei UI", 9F);

            tabResult = new TabPage("結果");
            tabResult.BackColor = Color.FromArgb(30, 30, 35);

            rtbResult = new RichTextBox();
            rtbResult.Dock = DockStyle.Fill;
            rtbResult.BackColor = Color.FromArgb(25, 25, 30);
            rtbResult.ForeColor = Color.FromArgb(220, 220, 220);
            rtbResult.Font = new Font("Consolas", 10F);
            rtbResult.ReadOnly = true;
            rtbResult.BorderStyle = BorderStyle.None;
            rtbResult.WordWrap = true;

            Panel pnlResultButtons = new Panel();
            pnlResultButtons.Dock = DockStyle.Bottom;
            pnlResultButtons.Height = 35;
            pnlResultButtons.BackColor = Color.FromArgb(35, 35, 42);

            btnCopyResult = new Button();
            btnCopyResult.Text = "複製";
            btnCopyResult.Location = new Point(5, 5);
            btnCopyResult.Size = new Size(75, 26);
            btnCopyResult.FlatStyle = FlatStyle.Flat;
            btnCopyResult.FlatAppearance.BorderSize = 0;
            btnCopyResult.BackColor = Color.FromArgb(60, 60, 70);
            btnCopyResult.ForeColor = Color.White;
            btnCopyResult.Cursor = Cursors.Hand;

            btnExport = new Button();
            btnExport.Text = "匯出";
            btnExport.Location = new Point(85, 5);
            btnExport.Size = new Size(75, 26);
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.BackColor = Color.FromArgb(60, 60, 70);
            btnExport.ForeColor = Color.White;
            btnExport.Cursor = Cursors.Hand;

            pnlResultButtons.Controls.Add(btnCopyResult);
            pnlResultButtons.Controls.Add(btnExport);

            tabResult.Controls.Add(rtbResult);
            tabResult.Controls.Add(pnlResultButtons);

            tabSearchResults = new TabPage("搜尋結果");
            tabSearchResults.BackColor = Color.FromArgb(30, 30, 35);

            dgvSearchResults = new DataGridView();
            dgvSearchResults.Dock = DockStyle.Fill;
            dgvSearchResults.BackgroundColor = Color.FromArgb(30, 30, 35);
            dgvSearchResults.ForeColor = Color.FromArgb(220, 220, 220);
            dgvSearchResults.GridColor = Color.FromArgb(60, 60, 70);
            dgvSearchResults.BorderStyle = BorderStyle.None;
            dgvSearchResults.RowHeadersVisible = false;
            dgvSearchResults.AllowUserToAddRows = false;
            dgvSearchResults.AllowUserToDeleteRows = false;
            dgvSearchResults.ReadOnly = true;
            dgvSearchResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSearchResults.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvSearchResults.DefaultCellStyle.BackColor = Color.FromArgb(35, 35, 42);
            dgvSearchResults.DefaultCellStyle.ForeColor = Color.FromArgb(210, 210, 210);
            dgvSearchResults.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 100, 150);
            dgvSearchResults.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 60);
            dgvSearchResults.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvSearchResults.EnableHeadersVisualStyles = false;

            dgvSearchResults.Columns.Add("colSource", "來源");
            dgvSearchResults.Columns.Add("colTitle", "標題");
            dgvSearchResults.Columns.Add("colSnippet", "摘要");
            dgvSearchResults.Columns.Add("colUrl", "網址");
            dgvSearchResults.Columns.Add("colScore", "分數");
            dgvSearchResults.Columns["colSource"].Width = 70;
            dgvSearchResults.Columns["colTitle"].Width = 180;
            dgvSearchResults.Columns["colSnippet"].Width = 350;
            dgvSearchResults.Columns["colUrl"].Width = 200;
            dgvSearchResults.Columns["colScore"].Width = 50;

            tabSearchResults.Controls.Add(dgvSearchResults);

            tabSteps = new TabPage("執行步驟");
            tabSteps.BackColor = Color.FromArgb(30, 30, 35);

            rtbSteps = new RichTextBox();
            rtbSteps.Dock = DockStyle.Fill;
            rtbSteps.BackColor = Color.FromArgb(25, 25, 30);
            rtbSteps.ForeColor = Color.FromArgb(150, 220, 150);
            rtbSteps.Font = new Font("Consolas", 9F);
            rtbSteps.ReadOnly = true;
            rtbSteps.BorderStyle = BorderStyle.None;
            rtbSteps.WordWrap = true;

            tabSteps.Controls.Add(rtbSteps);

            tabCode = new TabPage("程式碼");
            tabCode.BackColor = Color.FromArgb(30, 30, 35);

            rtbCode = new RichTextBox();
            rtbCode.Dock = DockStyle.Fill;
            rtbCode.BackColor = Color.FromArgb(20, 20, 25);
            rtbCode.ForeColor = Color.FromArgb(200, 220, 255);
            rtbCode.Font = new Font("Consolas", 10F);
            rtbCode.BorderStyle = BorderStyle.None;
            rtbCode.WordWrap = false;
            rtbCode.AcceptsTab = true;

            Panel pnlCodeButtons = new Panel();
            pnlCodeButtons.Dock = DockStyle.Bottom;
            pnlCodeButtons.Height = 35;
            pnlCodeButtons.BackColor = Color.FromArgb(35, 35, 42);

            btnRunCode = new Button();
            btnRunCode.Text = "> 編譯執行";
            btnRunCode.Location = new Point(5, 5);
            btnRunCode.Size = new Size(90, 26);
            btnRunCode.FlatStyle = FlatStyle.Flat;
            btnRunCode.FlatAppearance.BorderSize = 0;
            btnRunCode.BackColor = Color.FromArgb(50, 120, 50);
            btnRunCode.ForeColor = Color.White;
            btnRunCode.Cursor = Cursors.Hand;

            btnSaveCode = new Button();
            btnSaveCode.Text = "儲存";
            btnSaveCode.Location = new Point(100, 5);
            btnSaveCode.Size = new Size(75, 26);
            btnSaveCode.FlatStyle = FlatStyle.Flat;
            btnSaveCode.FlatAppearance.BorderSize = 0;
            btnSaveCode.BackColor = Color.FromArgb(60, 60, 70);
            btnSaveCode.ForeColor = Color.White;
            btnSaveCode.Cursor = Cursors.Hand;

            pnlCodeButtons.Controls.Add(btnRunCode);
            pnlCodeButtons.Controls.Add(btnSaveCode);

            tabCode.Controls.Add(rtbCode);
            tabCode.Controls.Add(pnlCodeButtons);

            tabCache = new TabPage("知識快取");
            tabCache.BackColor = Color.FromArgb(30, 30, 35);

            dgvCache = new DataGridView();
            dgvCache.Dock = DockStyle.Fill;
            dgvCache.BackgroundColor = Color.FromArgb(30, 30, 35);
            dgvCache.ForeColor = Color.FromArgb(220, 220, 220);
            dgvCache.GridColor = Color.FromArgb(60, 60, 70);
            dgvCache.BorderStyle = BorderStyle.None;
            dgvCache.RowHeadersVisible = false;
            dgvCache.AllowUserToAddRows = false;
            dgvCache.ReadOnly = true;
            dgvCache.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCache.DefaultCellStyle.BackColor = Color.FromArgb(35, 35, 42);
            dgvCache.DefaultCellStyle.ForeColor = Color.FromArgb(210, 210, 210);
            dgvCache.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 100, 150);
            dgvCache.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 50, 60);
            dgvCache.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvCache.EnableHeadersVisualStyles = false;

            dgvCache.Columns.Add("colCacheTopic", "主題");
            dgvCache.Columns.Add("colCacheSource", "來源");
            dgvCache.Columns.Add("colCacheTime", "快取時間");
            dgvCache.Columns.Add("colCacheUse", "使用次數");
            dgvCache.Columns["colCacheTopic"].Width = 250;
            dgvCache.Columns["colCacheSource"].Width = 100;
            dgvCache.Columns["colCacheTime"].Width = 140;
            dgvCache.Columns["colCacheUse"].Width = 70;

            tabCache.Controls.Add(dgvCache);

            tabMain.TabPages.Add(tabResult);
            tabMain.TabPages.Add(tabSearchResults);
            tabMain.TabPages.Add(tabSteps);
            tabMain.TabPages.Add(tabCode);
            tabMain.TabPages.Add(tabCache);

            // ===================== 系統托盤 =====================
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("顯示主視窗", null, TrayMenu_Show);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("結束程式", null, TrayMenu_Exit);

            trayIcon = new NotifyIcon(this.components);
            trayIcon.Text = "AI 智慧代理工具";
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = false;
            trayIcon.DoubleClick += TrayIcon_DoubleClick;

            try
            {
                trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            catch { }

            // ===================== 加入主窗體 =====================
            this.Controls.Add(tabMain);
            this.Controls.Add(pnlLeft);
            this.Controls.Add(statusBar);
            this.Controls.Add(pnlQuickButtons);
            this.Controls.Add(pnlTop);

            this.ResumeLayout(false);
        }
    }
}
