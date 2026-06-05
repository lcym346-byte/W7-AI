using System;
using System.Net;
using System.Windows.Forms;

namespace AIAgentTool
{
    static class Program
    {
        /// <summary>
        /// 應用程式進入點
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Windows 7 無 SP1 的 TLS 支援設定
            // .NET 4.0 預設只有 SSL3 和 TLS 1.0
            // 需要手動啟用 TLS 1.1 和 TLS 1.2
            try
            {
                // TLS 1.1 = 768, TLS 1.2 = 3072
                ServicePointManager.SecurityProtocol =
                    (SecurityProtocolType)768 |
                    (SecurityProtocolType)3072 |
                    SecurityProtocolType.Tls |
                    SecurityProtocolType.Ssl3;
            }
            catch
            {
                // 如果系統不支援 TLS 1.2 (極舊版 Win7)，至少保留 TLS 1.0
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            }

            // 忽略 SSL 憑證錯誤 (某些舊系統根憑證過期)
            ServicePointManager.ServerCertificateValidationCallback =
                delegate { return true; };

            // 增加同時連線數 (預設只有 2)
            ServicePointManager.DefaultConnectionLimit = 10;

            // 啟動 WinForms
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
