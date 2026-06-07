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

        private Label lblGroqKey;
        private TextBox txtGroqKey;
        private Label lblMistralKey;
        private TextBox txtMistralKey;
        private Label lblOpenRouterKey;
        private TextBox txtOpenRouterKey;
        private Label lblFreeNote;

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

            // ===================================
            // 視窗設定
            // ===================================
            this.Text = "Settings";
            this.Size = new Size(520, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(35, 35, 40);
            this.ForeColor = Color.FromArgb(220, 220, 220);
            this.Font = new Font("Microsoft JhengHei UI", 9F);

            // ===================================
            // 分頁控件
            // ===================================
            tabSettings = new TabControl();
            tabSettings.Location = new Point(10, 10);
            tabSettings.Size = new Size(490, 430);

            // --- AI 設定分頁 ---
            tabAI = new TabPage("AI Settings");
            tabAI.BackColor = Color.FromArgb(35, 35, 40);
            tabAI.AutoScroll = true;

            int y = 15;

            lblGeminiKey = new Label();
            lblGeminiKey.Text = "Google Gemini API Key:";
            lblGeminiKey.Location = new Point(20, y);
            lblGeminiKey.AutoSize = true;
            y += 22;

            txtGeminiKey = new TextBox();
            txtGeminiKey.Location = new Point(20, y);
            txtGeminiKey.Size = new Size(350, 23);
            txtGeminiKey.BackColor = Color.FromArgb(50, 50, 55);
            txtGeminiKey.ForeColor = Color.White;
            txtGeminiKey.UseSystemPasswordChar = true;

            btnTestAi = new Button();
            btnTestAi.Text = "Test";
            btnTestAi.Location = new Point(378, y - 1);
            btnTestAi.Size = new Size(60, 25);
            btnTestAi.FlatStyle = FlatStyle.Flat;
            btnTestAi.FlatAppearance.BorderSize = 0;
            btnTestAi.BackColor = Color.FromArgb(60, 120, 60);
            btnTestAi.ForeColor = Color.White;
            y += 32;

            lblGroqKey = new Label();
            lblGroqKey.Text = "Groq API Key (free: console.groq.com/keys):";
            lblGroqKey.Location = new Point(20, y);
            lblGroqKey.AutoSize = true;
            y += 22;

            txtGroqKey = new TextBox();
            txtGroqKey.Location = new Point(20, y);
            txtGroqKey.Size = new Size(420, 23);
            txtGroqKey.BackColor = Color.FromArgb(50, 50, 55);
            txtGroqKey.ForeColor = Color.White;
            txtGroqKey.UseSystemPasswordChar = true;
            y += 32;

            lblMistralKey = new Label();
            lblMistralKey.Text = "Mistral API Key (free: console.mistral.ai/api-keys):";
            lblMistralKey.Location = new Point(20, y);
            lblMistralKey.AutoSize = true;
            y += 22;

            txtMistralKey = new TextBox();
            txtMistralKey.Location = new Point(20, y);
            txtMistralKey.Size = new Size(420, 23);
            txtMistralKey.BackColor = Color.FromArgb(50, 50, 55);
            txtMistralKey.ForeColor = Color.White;
            txtMistralKey.UseSystemPasswordChar = true;
            y += 32;

            lblOpenRouterKey = new Label();
            lblOpenRouterKey.Text = "OpenRouter API Key (free: openrouter.ai/keys):";
            lblOpenRouterKey.Location = new Point(20, y);
            lblOpenRouterKey.AutoSize = true;
            y += 22;

            txtOpenRouterKey = new TextBox();
            txtOpenRouterKey.Location = new Point(20, y);
            txtOpenRouterKey.Size = new Size(420, 23);
            txtOpenRouterKey.BackColor = Color.FromArgb(50, 50, 55);
            txtOpenRouterKey.ForeColor = Color.White;
            txtOpenRouterKey.UseSystemPasswordChar = true;
            y += 32;

            lblFreeNote = new Label();
            lblFreeNote.Text = "* LLM7 (deepseek-v3) not need Key, auto fallback";
            lblFreeNote.Location = new Point(20, y);
            lblFreeNote.AutoSize = true;
            lblFreeNote.ForeColor = Color.FromArgb(150, 200, 255);
            y += 28;

            lblAiSource = new Label();
            lblAiSource.Text = "AI Source Priority:";
            lblAiSource.Location = new Point(20, y);
            lblAiSource.AutoSize = true;
            y += 22;

            cboAiSource = new ComboBox();
            cboAiSource.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAiSource.Location = new Point(20, y);
            cboAiSource.Size = new Size(300, 23);
            cboAiSource.BackColor = Color.FromArgb(50, 50, 55);
            cboAiSource.ForeColor = Color.White;
            cboAiSource.Items.AddRange(new object[] {
                "Auto (Gemini->DuckDuckGo->Free->Offline)",
                "Gemini Only",
                "DuckDuckGo AI Only",
                "Offline Only"
            });
            y += 30;

            lblAiStatus = new Label();
            lblAiStatus.Text = "Status: Not tested";
            lblAiStatus.Location = new Point(20, y);
            lblAiStatus.Size = new Size(420, 40);
            lblAiStatus.ForeColor = Color.Gray;

            tabAI.Controls.Add(lblGeminiKey);
            tabAI.Controls.Add(txtGeminiKey);
            tabAI.Controls.Add(btnTestAi);
            tabAI.Controls.Add(lblGroqKey);
            tabAI.Controls.Add(txtGroqKey);
            tabAI.Controls.Add(lblMistralKey);
            tabAI.Controls.Add(txtMistralKey);
            tabAI.Controls.Add(lblOpenRouterKey);
            tabAI.Controls.Add(txtOpenRouterKey);
            tabAI.Controls.Add(lblFreeNote);
            tabAI.Controls.Add(lblAiSource);
            tabAI.Controls.Add(cboAiSource);
            tabAI.Controls.Add(lblAiStatus);

            // --- 安全設定分頁 ---
            tabSafety = new TabPage("Security");
            tabSafety.BackColor = Color.FromArgb(35, 35, 40);

            lblSafetyLevel = new Label();
            lblSafetyLevel.Text = "Safety Level:";
            lblSafetyLevel.Location = new Point(20, 25);
            lblSafetyLevel.AutoSize = true;

            cboSafetyLevel = new ComboBox();
            cboSafetyLevel.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSafetyLevel.Location = new Point(20, 48);
            cboSafetyLevel.Size = new Size(200, 23);
            cboSafetyLevel.BackColor = Color.FromArgb(50, 50, 55);
            cboSafetyLevel.ForeColor = Color.White;
            cboSafetyLevel.Items.AddRange(new object[] {
                "Strict (confirm all)",
                "Medium (confirm dangerous)",
                "Relaxed (compile confirm only)"
            });

            chkConfirmRun = new CheckBox();
            chkConfirmRun.Text = "Confirm before running compiled code";
            chkConfirmRun.Location = new Point(20, 90);
            chkConfirmRun.AutoSize = true;
            chkConfirmRun.ForeColor = Color.FromArgb(220, 220, 220);
            chkConfirmRun.Checked = true;

            chkConfirmClose = new CheckBox();
            chkConfirmClose.Text = "Confirm before closing program";
            chkConfirmClose.Location = new Point(20, 118);
            chkConfirmClose.AutoSize = true;
            chkConfirmClose.ForeColor = Color.FromArgb(220, 220, 220);
            chkConfirmClose.Checked = true;

            chkConfirmCmd = new CheckBox();
            chkConfirmCmd.Text = "Confirm before CMD commands";
            chkConfirmCmd.Location = new Point(20, 146);
            chkConfirmCmd.AutoSize = true;
            chkConfirmCmd.ForeColor = Color.FromArgb(220, 220, 220);

            lblSafetyDesc = new Label();
            lblSafetyDesc.Text = "Note: Dangerous commands (format, del C:\\ etc)\nare always blocked regardless of safety level.";
            lblSafetyDesc.Location = new Point(20, 185);
            lblSafetyDesc.Size = new Size(400, 40);
            lblSafetyDesc.ForeColor = Color.FromArgb(255, 180, 100);

            tabSafety.Controls.Add(lblSafetyLevel);
            tabSafety.Controls.Add(cboSafetyLevel);
            tabSafety.Controls.Add(chkConfirmRun);
            tabSafety.Controls.Add(chkConfirmClose);
            tabSafety.Controls.Add(chkConfirmCmd);
            tabSafety.Controls.Add(lblSafetyDesc);

            // --- 一般設定分頁 ---
            tabGeneral = new TabPage("General");
            tabGeneral.BackColor = Color.FromArgb(35, 35, 40);

            lblSavePath = new Label();
            lblSavePath.Text = "Default save path:";
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
            chkMinimizeToTray.Text = "Minimize to system tray";
            chkMinimizeToTray.Location = new Point(20, 90);
            chkMinimizeToTray.AutoSize = true;
            chkMinimizeToTray.ForeColor = Color.FromArgb(220, 220, 220);

            chkBalloonNotify = new CheckBox();
            chkBalloonNotify.Text = "Show balloon notification on task complete";
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

            // ===================================
            // 底部按鈕
            // ===================================
            btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.Location = new Point(310, 450);
            btnOK.Size = new Size(80, 30);
            btnOK.FlatStyle = FlatStyle.Flat;
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.BackColor = Color.FromArgb(60, 130, 60);
            btnOK.ForeColor = Color.White;
            btnOK.DialogResult = DialogResult.OK;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(400, 450);
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
