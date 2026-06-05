using System.Windows.Forms;
using System.Drawing;

namespace AIAgentTool
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        private TabControl tabSettings;
        private TabPage tabAI;
        private TabPage tabSafety;
        private TabPage tabGeneral;

        private Label lblGeminiKey;
        private TextBox txtGeminiKey;
        private Label lblAiSource;
        private ComboBox cboAiSource;
        private Label lblAiStatus;
        private Button btnTestAi;

        private Label lblSafetyLevel;
        private ComboBox cboSafetyLevel;
        private CheckBox chkConfirmRun;
        private CheckBox chkConfirmClose;
        private CheckBox chkConfirmCmd;
        private Label lblSafetyDesc;

        private Label lblSavePath;
        private TextBox txtSavePath;
        private Button btnBrowsePath;
        private CheckBox chkMinimizeToTray;
        private CheckBox chkBalloonNotify;

        private Button btnOK;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // ═══════════════════════════════════════════
            // 視窗設定
            // ═══════════════════════════════════════════
            this.Text = "⚙ 設定";
            this.Size = new Size(500, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(35, 35, 40);
            this.ForeColor = Color.FromArgb(220, 220, 220);
            this.Font = new Font("Microsoft JhengHei UI", 9F);

            // ═══════════════════════════════════════════
            // 分頁控件
            // ═══════════════════════════════════════════
            tabSettings = new TabControl();
            tabSettings.Location = new Point(10, 10);
            tabSettings.Size = new Size(470, 330);

            // — AI 設定分頁 —
            tabAI = new TabPage("🤖 AI 設定");
            tabAI.BackColor = Color.FromArgb(35, 35, 40);

            lblGeminiKey = new Label();
            lblGeminiKey.Text = "Google Gemini API Key：";
            lblGeminiKey.Location = new Point(20, 25);
            lblGeminiKey.AutoSize = true;

            txtGeminiKey = new TextBox();
            txtGeminiKey.Location = new Point(20, 48);
            txtGeminiKey.Size = new Size(350, 23);
            txtGeminiKey.BackColor = Color.FromArgb(50, 50, 55);
            txtGeminiKey.ForeColor = Color.White;
            txtGeminiKey.UseSystemPasswordChar = true;

            btnTestAi = new Button();
            btnTestAi.Text = "測試";
            btnTestAi.Location = new Point(378, 47);
            btnTestAi.Size = new Size(60, 25);
            btnTestAi.FlatStyle = FlatStyle.Flat;
            btnTestAi.FlatAppearance.BorderSize = 0;
            btnTestAi.BackColor = Color.FromArgb(60, 120, 60);
            btnTestAi.ForeColor = Color.White;

            lblAiSource = new Label();
            lblAiSource.Text = "AI 來源優先順序：";
            lblAiSource.Location = new Point(20, 90);
            lblAiSource.AutoSize = true;

            cboAiSource = new ComboBox();
            cboAiSource.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAiSource.Location = new Point(20, 113);
            cboAiSource.Size = new Size(200, 23);
            cboAiSource.BackColor = Color.FromArgb(50, 50, 55);
            cboAiSource.ForeColor = Color.White;
            cboAiSource.Items.AddRange(new object[] {
                "自動（Gemini → DuckDuckGo → 離線）",
                "僅 Gemini",
                "僅 DuckDuckGo AI",
                "僅離線模板"
            });

            lblAiStatus = new Label();
            lblAiStatus.Text = "狀態：未測試";
            lblAiStatus.Location = new Point(20, 150);
            lblAiStatus.Size = new Size(400, 40);
            lblAiStatus.ForeColor = Color.Gray;

            tabAI.Controls.Add(lblGeminiKey);
            tabAI.Controls.Add(txtGeminiKey);
            tabAI.Controls.Add(btnTestAi);
            tabAI.Controls.Add(lblAiSource);
            tabAI.Controls.Add(cboAiSource);
            tabAI.Controls.Add(lblAiStatus);

            // — 安全設定分頁 —
            tabSafety = new TabPage("🔒 安全設定");
            tabSafety.BackColor = Color.FromArgb(35, 35, 40);

            lblSafetyLevel = new Label();
            lblSafetyLevel.Text = "安全等級：";
            lblSafetyLevel.Location = new Point(20, 25);
            lblSafetyLevel.AutoSize = true;

            cboSafetyLevel = new ComboBox();
            cboSafetyLevel.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSafetyLevel.Location = new Point(20, 48);
            cboSafetyLevel.Size = new Size(200, 23);
            cboSafetyLevel.BackColor = Color.FromArgb(50, 50, 55);
            cboSafetyLevel.ForeColor = Color.White;
            cboSafetyLevel.Items.AddRange(new object[] {
                "嚴格（所有操作需確認）",
                "中等（危險操作需確認）",
                "寬鬆（僅編譯執行需確認）"
            });

            chkConfirmRun = new CheckBox();
            chkConfirmRun.Text = "執行編譯後的程式前需要確認";
            chkConfirmRun.Location = new Point(20, 90);
            chkConfirmRun.AutoSize = true;
            chkConfirmRun.ForeColor = Color.FromArgb(220, 220, 220);
            chkConfirmRun.Checked = true;

            chkConfirmClose = new CheckBox();
            chkConfirmClose.Text = "關閉程式前需要確認";
            chkConfirmClose.Location = new Point(20, 118);
            chkConfirmClose.AutoSize = true;
            chkConfirmClose.ForeColor = Color.FromArgb(220, 220, 220);
            chkConfirmClose.Checked = true;

            chkConfirmCmd = new CheckBox();
            chkConfirmCmd.Text = "執行 CMD 命令前需要確認";
            chkConfirmCmd.Location = new Point(20, 146);
            chkConfirmCmd.AutoSize = true;
            chkConfirmCmd.ForeColor = Color.FromArgb(220, 220, 220);

            lblSafetyDesc = new Label();
            lblSafetyDesc.Text = "註：無論安全等級如何，危險命令（format、del C:\\等）\n永遠被禁止執行。";
            lblSafetyDesc.Location = new Point(20, 185);
            lblSafetyDesc.Size = new Size(400, 40);
            lblSafetyDesc.ForeColor = Color.FromArgb(255, 180, 100);

            tabSafety.Controls.Add(lblSafetyLevel);
            tabSafety.Controls.Add(cboSafetyLevel);
            tabSafety.Controls.Add(chkConfirmRun);
            tabSafety.Controls.Add(chkConfirmClose);
            tabSafety.Controls.Add(chkConfirmCmd);
            tabSafety.Controls.Add(lblSafetyDesc);

            // — 一般設定分頁 —
            tabGeneral = new TabPage("📋 一般設定");
            tabGeneral.BackColor = Color.FromArgb(35, 35, 40);

            lblSavePath = new Label();
            lblSavePath.Text = "預設儲存路徑：";
            lblSavePath.Location = new Point(20, 25);
            lblSavePath.AutoSize = true;

            txtSavePath = new TextBox();
            txtSavePath.Location = new Point(20, 48);
            txtSavePath.Size = new Size(330, 23);
            txtSavePath.BackColor = Color.FromArgb(50, 50, 55);
            txtSavePath.ForeColor = Color.White;

            btnBrowsePath = new Button();
            btnBrowsePath.Text = "...";
            btnBrowsePath.Location = new Point(358, 47);
            btnBrowsePath.Size = new Size(35, 25);
            btnBrowsePath.FlatStyle = FlatStyle.Flat;
            btnBrowsePath.FlatAppearance.BorderSize = 0;
            btnBrowsePath.BackColor = Color.FromArgb(60, 60, 70);
            btnBrowsePath.ForeColor = Color.White;

            chkMinimizeToTray = new CheckBox();
            chkMinimizeToTray.Text = "最小化時縮到系統托盤";
            chkMinimizeToTray.Location = new Point(20, 90);
            chkMinimizeToTray.AutoSize = true;
            chkMinimizeToTray.ForeColor = Color.FromArgb(220, 220, 220);

            chkBalloonNotify = new CheckBox();
            chkBalloonNotify.Text = "顯示氣泡通知（任務完成時）";
            chkBalloonNotify.Location = new Point(20, 118);
            chkBalloonNotify.AutoSize = true;
            chkBalloonNotify.ForeColor = Color.FromArgb(220, 220, 220);

            tabGeneral.Controls.Add(lblSavePath);
            tabGeneral.Controls.Add(txtSavePath);
            tabGeneral.Controls.Add(btnBrowsePath);
            tabGeneral.Controls.Add(chkMinimizeToTray);
            tabGeneral.Controls.Add(chkBalloonNotify);

            // 加入分頁
            tabSettings.TabPages.Add(tabAI);
            tabSettings.TabPages.Add(tabSafety);
            tabSettings.TabPages.Add(tabGeneral);

            // ═══════════════════════════════════════════
            // 底部按鈕
            // ═══════════════════════════════════════════
            btnOK = new Button();
            btnOK.Text = "確定";
            btnOK.Location = new Point(300, 350);
            btnOK.Size = new Size(80, 30);
            btnOK.FlatStyle = FlatStyle.Flat;
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.BackColor = Color.FromArgb(60, 130, 60);
            btnOK.ForeColor = Color.White;
            btnOK.DialogResult = DialogResult.OK;

            btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Location = new Point(390, 350);
            btnCancel.Size = new Size(80, 30);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.BackColor = Color.FromArgb(100, 60, 60);
            btnCancel.ForeColor = Color.White;
            btnCancel.DialogResult = DialogResult.Cancel;

            // 加入主窗體
            this.Controls.Add(tabSettings);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            this.ResumeLayout(false);
        }
    }
}
