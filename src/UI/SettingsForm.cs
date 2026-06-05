using System;
using System.Windows.Forms;
using AIAgentTool.Models;

namespace AIAgentTool
{
    /// <summary>
    /// 設定視窗 - API 金鑰、AI 來源、安全等級、一般設定
    /// </summary>
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
            cboAiSource.SelectedIndex = (int)_settings.AiSource;

            // 安全設定
            cboSafetyLevel.SelectedIndex = (int)_settings.Safety;
            chkConfirmRun.Checked = true; // 始終建議開啟
            chkConfirmClose.Checked = _settings.Safety != SafetyLevel.Relaxed;
            chkConfirmCmd.Checked = _settings.Safety == SafetyLevel.Strict;

            // 一般設定
            txtSavePath.Text = _settings.DefaultSavePath ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            chkMinimizeToTray.Checked = _settings.MinimizeToTray;
            chkBalloonNotify.Checked = _settings.ShowBalloonNotify;

            // 顯示 AI 狀態
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
            lblAiStatus.Text = "測試中...";
            Application.DoEvents();

            try
            {
                string key = txtGeminiKey.Text.Trim();
                if (string.IsNullOrEmpty(key))
                {
                    lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(255, 150, 100);
                    lblAiStatus.Text = "未填入 API Key。將使用 DuckDuckGo AI 或離線模板。";
                    return;
                }

                // 簡單測試：嘗試連線 Gemini API
                AppSettings testSettings = new AppSettings();
                testSettings.GeminiApiKey = key;
                testSettings.AiSource = AiSourceOption.GeminiOnly;

                Services.AI.AiRouter testRouter = new Services.AI.AiRouter(testSettings);
                string result = testRouter.Ask("Hello, respond with 'OK' only.");

                if (!string.IsNullOrEmpty(result))
                {
                    lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(100, 255, 100);
                    lblAiStatus.Text = "✓ Gemini API 連線成功！回應：" +
                        (result.Length > 50 ? result.Substring(0, 50) + "..." : result);
                }
                else
                {
                    lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(255, 150, 100);
                    lblAiStatus.Text = "連線成功但無回應，請確認 API Key 有效。";
                }
            }
            catch (Exception ex)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(255, 100, 100);
                lblAiStatus.Text = "✗ 測試失敗：" + ex.Message;
            }
        }

        private void BtnBrowsePath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "選擇預設儲存路徑";
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
            string key = txtGeminiKey.Text.Trim();
            int sourceIdx = cboAiSource.SelectedIndex;

            if (sourceIdx == 1 && string.IsNullOrEmpty(key))
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(255, 150, 100);
                lblAiStatus.Text = "⚠ 選擇「僅 Gemini」但未填入 API Key";
            }
            else if (sourceIdx == 0 && !string.IsNullOrEmpty(key))
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(150, 200, 255);
                lblAiStatus.Text = "自動模式：優先 Gemini → DuckDuckGo AI → 離線模板";
            }
            else if (sourceIdx == 2)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.FromArgb(150, 200, 255);
                lblAiStatus.Text = "使用 DuckDuckGo AI Chat（免費、無需 Key）";
            }
            else if (sourceIdx == 3)
            {
                lblAiStatus.ForeColor = System.Drawing.Color.Gray;
                lblAiStatus.Text = "僅使用離線模板（不需網路）";
            }
            else
            {
                lblAiStatus.ForeColor = System.Drawing.Color.Gray;
                lblAiStatus.Text = "狀態：未測試";
            }
        }

        /// <summary>
        /// 取得使用者修改後的設定
        /// </summary>
        public AppSettings GetSettings()
        {
            _settings.GeminiApiKey = txtGeminiKey.Text.Trim();
            _settings.AiSource = (AiSourceOption)cboAiSource.SelectedIndex;
            _settings.Safety = (SafetyLevel)cboSafetyLevel.SelectedIndex;
            _settings.DefaultSavePath = txtSavePath.Text.Trim();
            _settings.MinimizeToTray = chkMinimizeToTray.Checked;
            _settings.ShowBalloonNotify = chkBalloonNotify.Checked;
            return _settings;
        }
    }
}
