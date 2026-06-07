using System;
using System.Windows.Forms;
using AIAgentTool.Models;

namespace AIAgentTool
{
    public partial class SettingsForm : Form
    {
        private AppSettings _settings;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            InitializeComponent();
            LoadSettings();
            WireEvents();
        }

        private void LoadSettings()
        {
            txtGeminiKey.Text = _settings.GeminiApiKey ?? "";
            txtGroqKey.Text = _settings.GroqApiKey ?? "";
            txtMistralKey.Text = _settings.MistralApiKey ?? "";
            txtOpenRouterKey.Text = _settings.OpenRouterApiKey ?? "";
            cboAiSource.SelectedIndex = (int)_settings.AiSource;

            cboSafetyLevel.SelectedIndex = (int)_settings.Safety;
            chkConfirmRun.Checked = true;
            chkConfirmClose.Checked = _settings.Safety != SafetyLevel.Relaxed;
            chkConfirmCmd.Checked = _settings.Safety == SafetyLevel.Strict;

            txtSavePath.Text = _settings.DefaultSavePath ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            chkMinimizeToTray.Checked = _settings.MinimizeToTray;
            chkBalloonNotify.Checked = _settings.ShowBalloonNotify;

            UpdateAiStatusLabel();
        }

        private void WireEvents()
        {
            btnTestAi.Click += BtnTestAi_Click;
            btnBrowsePath.Click += BtnBrowsePath_Click;
            cboAiSource.SelectedIndexChanged += CboAiSource_SelectedIndexChanged;
        }

        private void BtnTestAi_Click(object sender, EventArgs e)
        {
            lblAiStatus.ForeColor = System.Drawing.Color.Yellow;
            lblAiStatus.Text = "\u6e2c\u8a66\u4e2d...";
            Application.DoEvents();

            try
            {
                AppSettings testSettings = new AppSettings();
                testSettings.GeminiApiKey = txtGeminiKey.Text.Trim();
                testSettings.GroqApiKey = txtGroqKey.Text.Trim();
                testSettings.MistralApiKey = txtMistralKey.Text.Trim();
                testSettings.OpenRouterApiKey = txtOpenRouterKey.Text.Trim();
                testSettings.AiSource = AiSourceOption.Auto;

                Services.AI.AiRouter testRouter = new Services.AI.AiRouter(testSettings);
                string result = testRouter.TestAllConnections();

                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(150, 255, 150);
                lblAiStatus.Text = result;
            }
            catch (Exception ex)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(255, 100, 100);
                lblAiStatus.Text = "\u6e2c\u8a66\u5931\u6557: " + ex.Message;
            }
        }

        private void BtnBrowsePath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "\u9078\u64c7\u9810\u8a2d\u5132\u5b58\u8def\u5f91";
            fbd.SelectedPath = txtSavePath.Text;

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtSavePath.Text = fbd.SelectedPath;
            }
        }

        private void CboAiSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateAiStatusLabel();
        }

        private void UpdateAiStatusLabel()
        {
            int sourceIdx = cboAiSource.SelectedIndex;
            string key = txtGeminiKey.Text.Trim();

            if (sourceIdx == 0)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(150, 200, 255);
                lblAiStatus.Text = "\u81ea\u52d5: Gemini -> DuckDuckGo -> Groq/Mistral/OpenRouter/LLM7 -> \u96e2\u7dda";
            }
            else if (sourceIdx == 1 && string.IsNullOrEmpty(key))
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(255, 150, 100);
                lblAiStatus.Text = "\u8b66\u544a: \u5df2\u9078\u64c7\u50c5 Gemini \u4f46\u672a\u8a2d\u5b9a API Key";
            }
            else if (sourceIdx == 1)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(150, 255, 150);
                lblAiStatus.Text = "\u50c5 Gemini \u6a21\u5f0f";
            }
            else if (sourceIdx == 2)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(255, 180, 100);
                lblAiStatus.Text = "DuckDuckGo AI (\u53ef\u80fd\u88ab 418 \u6311\u6230\u5c01\u9396)";
            }
            else if (sourceIdx == 3)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.Gray;
                lblAiStatus.Text = "\u50c5\u96e2\u7dda\u6a21\u677f (\u4e0d\u9700\u8981\u7db2\u8def)";
            }
            else
            {
                lblAiStatus.ForeColor = System.Drawing.Color.Gray;
                lblAiStatus.Text = "\u72c0\u614b: \u672a\u6e2c\u8a66";
            }
        }

        public AppSettings GetSettings()
        {
            _settings.GeminiApiKey = txtGeminiKey.Text.Trim();
            _settings.GroqApiKey = txtGroqKey.Text.Trim();
            _settings.MistralApiKey = txtMistralKey.Text.Trim();
            _settings.OpenRouterApiKey = txtOpenRouterKey.Text.Trim();
            _settings.AiSource = (AiSourceOption)cboAiSource.SelectedIndex;
            _settings.Safety = (SafetyLevel)cboSafetyLevel.SelectedIndex;
            _settings.DefaultSavePath = txtSavePath.Text.Trim();
            _settings.MinimizeToTray = chkMinimizeToTray.Checked;
            _settings.ShowBalloonNotify = chkBalloonNotify.Checked;
            return _settings;
        }
    }
}
