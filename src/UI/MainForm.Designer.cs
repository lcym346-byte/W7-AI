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

            this.Text = "\u8a2d\u5b9a";
            this.Size = new Size(520, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(35, 35, 40);
            this.ForeColor = Color.FromArgb(220, 220, 220);
            this.Font = new Font("Microsoft JhengHei UI", 9F);

            tabSettings = new TabControl();
            tabSettings.Location = new Point(10, 10);
            tabSettings.Size = new Size(490, 430);

            // --- AI \u8a2d\u5b9a\u5206\u9801 ---
            tabAI = new TabPage("AI \u8a2d\u5b9a");
            tabAI.BackColor = Color.FromArgb(35, 35, 40);
            tabAI.AutoScroll = true;

            int y = 15;

            lblGeminiKey = new Label();
            lblGeminiKey.Text = "Google Gemini API \u91d1\u9470:";
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
            btnTestAi.Text = "\u6e2c\u8a66";
            btnTestAi.Location = new Point(378, y - 1);
            btnTestAi.Size = new Size(60, 25);
            btnTestAi.FlatStyle = FlatStyle.Flat;
            btnTestAi.FlatAppearance.BorderSize = 0;
            btnTestAi.BackColor = Color.FromArgb(60, 120, 60);
            btnTestAi.ForeColor = Color.White;
            y += 32;

            lblGroqKey = new Label();
            lblGroqKey.Text = "Groq API \u91d1\u9470 (\u514d\u8cbb: console.groq.com/keys):";
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
            lblMistralKey.Text = "Mistral API \u91d1\u9470 (\u514d\u8cbb: console.mistral.ai/api-keys):";
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
            lblOpenRouterKey.Text = "OpenRouter API \u91d1\u9470 (\u514d\u8cbb: openrouter.ai/keys):";
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
            lblFreeNote.Text = "* LLM7 (deepseek-v3) \u4e0d\u9700\u8981 Key\uff0c\u81ea\u52d5\u5099\u63f4";
            lblFreeNote.Location = new Point(20, y);
            lblFreeNote.AutoSize = true;
            lblFreeNote.ForeColor = Color.FromArgb(150, 200, 255);
            y += 28;

            lblAiSource = new Label();
            lblAiSource.Text = "AI \u4f86\u6e90\u512a\u5148\u9806\u5e8f:";
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
                "\u81ea\u52d5 (Gemini->DuckDuckGo->\u514d\u8cbb->\u96e2\u7dda)",
                "\u50c5 Gemini",
                "\u50c5 DuckDuckGo AI",
                "\u50c5\u96e2\u7dda\u6a21\u677f"
            });
            y += 30;

            lblAiStatus = new Label();
            lblAiStatus.Text = "\u72c0\u614b: \u672a\u6e2c\u8a66";
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

            // --- \u5b89\u5168\u8a2d\u5b9a\u5206\u9801 ---
            tabSafety = new TabPage("\u5b89\u5168\u8a2d\u5b9a");
            tabSafety.BackColor = Color.FromArgb(35, 35, 40);

            lblSafetyLevel = new Label();
            lblSafetyLevel.Text = "\u5b89\u5168\u7b49\u7d1a:";
            lblSafetyLevel.Location = new Point(20, 25);
            lblSafetyLevel.AutoSize = true;

            cboSafetyLevel = new ComboBox();
            cboSafetyLevel.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSafetyLevel.Location = new Point(20, 48);
            cboSafetyLevel.Size = new Size(200, 23);
            cboSafetyLevel.BackColor = Color.FromArgb(50, 50, 55);
            cboSafetyLevel.ForeColor = Color.White;
            cboSafetyLevel.Items.AddRange(new object[] {
                "\u56b4\u683c (\u5168\u90e8\u78ba\u8a8d)",
                "\u4e2d\u7b49 (\u5371\u96aa\u64cd\u4f5c\u78ba\u8a8d)",
                "\u5bec\u9b06 (\u50c5\u7de8\u8b6f\u57f7\u884c\u78ba\u8a8d)"
            });

            chkConfirmRun = new CheckBox();
            chkConfirmRun.Text = "\u57f7\u884c\u7de8\u8b6f\u7a0b\u5f0f\u524d\u78ba\u8a8d";
            chkConfirmRun.Location = new Point(20, 90);
            chkConfirmRun.AutoSize = true;
            chkConfirmRun.ForeColor = Color.FromArgb(220, 220, 220);
            chkConfirmRun.Checked = true;

            chkConfirmClose = new CheckBox();
            chkConfirmClose.Text = "\u95dc\u9589\u7a0b\u5f0f\u524d\u78ba\u8a8d";
            chkConfirmClose.Location = new Point(20, 118);
            chkConfirmClose.AutoSize = true;
            chkConfirmClose.ForeColor = Color.FromArgb(220, 220, 220);
            chkConfirmClose.Checked = true;

            chkConfirmCmd = new CheckBox();
            chkConfirmCmd.Text = "\u57f7\u884c CMD \u547d\u4ee4\u524d\u78ba\u8a8d";
            chkConfirmCmd.Location = new Point(20, 146);
            chkConfirmCmd.AutoSize = true;
            chkConfirmCmd.ForeColor = Color.FromArgb(220, 220, 220);

            lblSafetyDesc = new Label();
            lblSafetyDesc.Text = "\u8a3b\u610f\uff1a\u5371\u96aa\u547d\u4ee4 (format, del C:\\ \u7b49)\n\u7121\u8ad6\u5b89\u5168\u7b49\u7d1a\u5982\u4f55\u90fd\u6703\u88ab\u5c01\u9396\u3002";
            lblSafetyDesc.Location = new Point(20, 185);
            lblSafetyDesc.Size = new Size(400, 40);
            lblSafetyDesc.ForeColor = Color.FromArgb(255, 180, 100);

            tabSafety.Controls.Add(lblSafetyLevel);
            tabSafety.Controls.Add(cboSafetyLevel);
            tabSafety.Controls.Add(chkConfirmRun);
            tabSafety.Controls.Add(chkConfirmClose);
            tabSafety.Controls.Add(chkConfirmCmd);
            tabSafety.Controls.Add(lblSafetyDesc);

            // --- \u4e00\u822c\u8a2d\u5b9a\u5206\u9801 ---
            tabGeneral = new TabPage("\u4e00\u822c\u8a2d\u5b9a");
            tabGeneral.BackColor = Color.FromArgb(35, 35, 40);

            lblSavePath = new Label();
            lblSavePath.Text = "\u9810\u8a2d\u5132\u5b58\u8def\u5f91:";
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
            chkMinimizeToTray.Text = "\u6700\u5c0f\u5316\u81f3\u7cfb\u7d71\u6258\u76e4";
            chkMinimizeToTray.Location = new Point(20, 90);
            chkMinimizeToTray.AutoSize = true;
            chkMinimizeToTray.ForeColor = Color.FromArgb(220, 220, 220);

            chkBalloonNotify = new CheckBox();
            chkBalloonNotify.Text = "\u4efb\u52d9\u5b8c\u6210\u6642\u986f\u793a\u901a\u77e5";
            chkBalloonNotify.Location = new Point(20, 118);
            chkBalloonNotify.AutoSize = true;
            chkBalloonNotify.ForeColor = Color.FromArgb(220, 220, 220);

            tabGeneral.Controls.Add(lblSavePath);
            tabGeneral.Controls.Add(txtSavePath);
            tabGeneral.Controls.Add(btnBrowsePath);
            tabGeneral.Controls.Add(chkMinimizeToTray);
            tabGeneral.Controls.Add(chkBalloonNotify);

            tabSettings.TabPages.Add(tabAI);
            tabSettings.TabPages.Add(tabSafety);
            tabSettings.TabPages.Add(tabGeneral);

            // \u5e95\u90e8\u6309\u9215
            btnOK = new Button();
            btnOK.Text = "\u78ba\u5b9a";
            btnOK.Location = new Point(310, 450);
            btnOK.Size = new Size(80, 30);
            btnOK.FlatStyle = FlatStyle.Flat;
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.BackColor = Color.FromArgb(60, 130, 60);
            btnOK.ForeColor = Color.White;
            btnOK.DialogResult = DialogResult.OK;

            btnCancel = new Button();
            btnCancel.Text = "\u53d6\u6d88";
            btnCancel.Location = new Point(400, 450);
            btnCancel.Size = new Size(80, 30);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.BackColor = Color.FromArgb(100, 60, 60);
            btnCancel.ForeColor = Color.White;
            btnCancel.DialogResult = DialogResult.Cancel;

            this.Controls.Add(tabSettings);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            this.ResumeLayout(false);
        }
    }
}
