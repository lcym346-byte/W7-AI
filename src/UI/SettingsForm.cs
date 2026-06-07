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
            // AI 設定
            txtGeminiKey.Text = _settings.GeminiApiKey ?? "";
            txtGroqKey.Text = _settings.GroqApiKey ?? "";
            txtMistralKey.Text = _settings.MistralApiKey ?? "";
            txtOpenRouterKey.Text = _settings.OpenRouterApiKey ?? "";
            cboAiSource.SelectedIndex = (int)_settings.AiSource;

            // 安全設定
            cboSafetyLevel.SelectedIndex = (int)_settings.Safety;
            chkConfirmRun.Checked = true;
            chkConfirmClose.Checked = _settings.Safety != SafetyLevel.Relaxed;
            chkConfirmCmd.Checked = _settings.Safety == SafetyLevel.Strict;

            // 一般設定
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
            lblAiStatus.Text = "Testing...";
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
                lblAiStatus.Text = "Test failed: " + ex.Message;
            }
        }

        private void BtnBrowsePath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Select default save path";
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
                lblAiStatus.Text = "Auto: Gemini -> DuckDuckGo -> Groq/Mistral/OpenRouter/LLM7 -> Offline";
            }
            else if (sourceIdx == 1 && string.IsNullOrEmpty(key))
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(255, 150, 100);
                lblAiStatus.Text = "Warning: Gemini Only selected but no API Key";
            }
            else if (sourceIdx == 1)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(150, 255, 150);
                lblAiStatus.Text = "Gemini Only mode";
            }
            else if (sourceIdx == 2)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(255, 180, 100);
                lblAiStatus.Text = "DuckDuckGo AI (may be blocked by 418 challenge)";
            }
            else if (sourceIdx == 3)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.Gray;
                lblAiStatus.Text = "Offline templates only (no network needed)";
            }
            else
            {
                lblAiStatus.ForeColor = System.Drawing.Color.Gray;
                lblAiStatus.Text = "Status: Not tested";
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
