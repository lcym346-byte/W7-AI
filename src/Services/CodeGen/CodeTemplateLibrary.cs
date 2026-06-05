using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AIAgentTool.Services.CodeGen
{
    /// <summary>
    /// 程式碼模板庫
    /// 離線可用的預建模板，涵蓋常見需求
    /// </summary>
    public class CodeTemplateLibrary
    {
        // 模板索引：關鍵字 → 模板方法
        private readonly List<TemplateEntry> _templates;

        public CodeTemplateLibrary()
        {
            _templates = new List<TemplateEntry>();
            RegisterTemplates();
        }

        /// <summary>
        /// 根據描述找出最佳匹配模板
        /// </summary>
        public string FindBestTemplate(string description)
        {
            if (string.IsNullOrEmpty(description)) return null;

            string lower = description.ToLower();
            int bestScore = 0;
            TemplateEntry bestMatch = null;

            foreach (TemplateEntry entry in _templates)
            {
                int score = 0;
                foreach (string keyword in entry.Keywords)
                {
                    if (lower.Contains(keyword.ToLower()))
                        score++;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = entry;
                }
            }

            if (bestMatch != null && bestScore >= 1)
                return bestMatch.GenerateCode(description);

            return null;
        }

        /// <summary>
        /// 列出所有可用模板
        /// </summary>
        public string ListTemplates()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("═══ 可用的程式碼模板 ═══\n");

            for (int i = 0; i < _templates.Count; i++)
            {
                sb.AppendLine(string.Format("{0}. {1}",
                    i + 1, _templates[i].Description));
                sb.AppendLine(string.Format("   關鍵字: {0}",
                    string.Join(", ", _templates[i].Keywords)));
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ============================================================
        // 註冊所有模板
        // ============================================================

        private void RegisterTemplates()
        {
            // 1. 檔案批次重命名
            _templates.Add(new TemplateEntry(
                "批次重命名檔案",
                new string[] { "重命名", "rename", "批次", "檔名", "前綴", "後綴" },
                delegate(string desc) { return TemplateBatchRename(); }));

            // 2. 計時器/倒數
            _templates.Add(new TemplateEntry(
                "倒數計時器 (WinForms)",
                new string[] { "計時", "倒數", "timer", "countdown", "碼表" },
                delegate(string desc) { return TemplateCountdownTimer(); }));

            // 3. 檔案搜尋
            _templates.Add(new TemplateEntry(
                "檔案搜尋工具",
                new string[] { "搜尋", "search", "找", "file", "檔案" },
                delegate(string desc) { return TemplateFileSearch(); }));

            // 4. CSV 處理
            _templates.Add(new TemplateEntry(
                "CSV 讀取與統計",
                new string[] { "csv", "excel", "統計", "平均", "資料" },
                delegate(string desc) { return TemplateCsvProcessor(); }));

            // 5. 簡易 HTTP 下載
            _templates.Add(new TemplateEntry(
                "檔案下載器",
                new string[] { "下載", "download", "http", "url", "網路" },
                delegate(string desc) { return TemplateFileDownloader(); }));

            // 6. 文字取代工具
            _templates.Add(new TemplateEntry(
                "文字批次取代",
                new string[] { "取代", "replace", "替換", "文字", "字串" },
                delegate(string desc) { return TemplateTextReplace(); }));

            // 7. 系統監控
            _templates.Add(new TemplateEntry(
                "系統資源監控",
                new string[] { "監控", "monitor", "cpu", "記憶體", "效能" },
                delegate(string desc) { return TemplateSystemMonitor(); }));

            // 8. 簡易記事本
            _templates.Add(new TemplateEntry(
                "簡易記事本 (WinForms)",
                new string[] { "記事本", "notepad", "編輯器", "editor", "文字編輯" },
                delegate(string desc) { return TemplateSimpleNotepad(); }));
        }

        // ============================================================
        // 各模板的程式碼生成
        // ============================================================

        private string TemplateBatchRename()
        {
            return @"using System;
using System.IO;

namespace BatchRename
{
    /// <summary>
    /// 批次重命名工具 — 為資料夾中的檔案加上日期前綴
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""=== 批次重命名工具 ==="");
            Console.Write(""請輸入資料夾路徑 (直接 Enter 用目前目錄): "");
            string folder = Console.ReadLine();

            if (string.IsNullOrEmpty(folder))
                folder = Directory.GetCurrentDirectory();

            if (!Directory.Exists(folder))
            {
                Console.WriteLine(""資料夾不存在: "" + folder);
                Console.ReadKey();
                return;
            }

            Console.Write(""請輸入篩選條件 (如 *.jpg，直接 Enter 用 *.*): "");
            string filter = Console.ReadLine();
            if (string.IsNullOrEmpty(filter)) filter = ""*.*"";

            Console.Write(""請輸入前綴文字 (直接 Enter 用日期): "");
            string prefix = Console.ReadLine();
            if (string.IsNullOrEmpty(prefix))
                prefix = DateTime.Now.ToString(""yyyyMMdd"") + ""_"";

            string[] files = Directory.GetFiles(folder, filter);
            Console.WriteLine(string.Format(""\n找到 {0} 個檔案，開始重命名..."", files.Length));

            int count = 0;
            foreach (string file in files)
            {
                string dir = Path.GetDirectoryName(file);
                string name = Path.GetFileName(file);
                string newName = prefix + name;
                string newPath = Path.Combine(dir, newName);

                if (!File.Exists(newPath))
                {
                    File.Move(file, newPath);
                    Console.WriteLine(string.Format(""  {0} -> {1}"", name, newName));
                    count++;
                }
            }

            Console.WriteLine(string.Format(""\n完成！共重命名 {0} 個檔案"", count));
            Console.ReadKey();
        }
    }
}";
        }

        private string TemplateCountdownTimer()
        {
            return @"using System;
using System.Drawing;
using System.Windows.Forms;

namespace CountdownTimer
{
    /// <summary>
    /// 倒數計時器 — WinForms 視窗應用
    /// </summary>
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new TimerForm());
        }
    }

    class TimerForm : Form
    {
        private Label lblTime;
        private Button btnStart;
        private Button btnReset;
        private NumericUpDown nudMinutes;
        private Timer timer;
        private int remainingSeconds;

        public TimerForm()
        {
            this.Text = ""倒數計時器"";
            this.Size = new Size(350, 200);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            lblTime = new Label();
            lblTime.Text = ""00:00"";
            lblTime.Font = new Font(""Consolas"", 48, FontStyle.Bold);
            lblTime.TextAlign = ContentAlignment.MiddleCenter;
            lblTime.Dock = DockStyle.Top;
            lblTime.Height = 100;

            nudMinutes = new NumericUpDown();
            nudMinutes.Minimum = 1;
            nudMinutes.Maximum = 999;
            nudMinutes.Value = 5;
            nudMinutes.Location = new Point(20, 110);
            nudMinutes.Size = new Size(80, 25);

            Label lblMin = new Label();
            lblMin.Text = ""分鐘"";
            lblMin.Location = new Point(105, 113);
            lblMin.Size = new Size(40, 20);

            btnStart = new Button();
            btnStart.Text = ""開始"";
            btnStart.Location = new Point(160, 108);
            btnStart.Size = new Size(70, 30);
            btnStart.Click += delegate { StartTimer(); };

            btnReset = new Button();
            btnReset.Text = ""重設"";
            btnReset.Location = new Point(240, 108);
            btnReset.Size = new Size(70, 30);
            btnReset.Click += delegate { ResetTimer(); };

            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += delegate { TimerTick(); };

            this.Controls.Add(lblTime);
            this.Controls.Add(nudMinutes);
            this.Controls.Add(lblMin);
            this.Controls.Add(btnStart);
            this.Controls.Add(btnReset);
        }

        private void StartTimer()
        {
            if (!timer.Enabled)
            {
                if (remainingSeconds <= 0)
                    remainingSeconds = (int)nudMinutes.Value * 60;
                timer.Start();
                btnStart.Text = ""暫停"";
            }
            else
            {
                timer.Stop();
                btnStart.Text = ""繼續"";
            }
        }

        private void ResetTimer()
        {
            timer.Stop();
            remainingSeconds = 0;
            lblTime.Text = ""00:00"";
            btnStart.Text = ""開始"";
        }

        private void TimerTick()
        {
            remainingSeconds--;
            int min = remainingSeconds / 60;
            int sec = remainingSeconds % 60;
            lblTime.Text = string.Format(""{0:D2}:{1:D2}"", min, sec);

            if (remainingSeconds <= 0)
            {
                timer.Stop();
                btnStart.Text = ""開始"";
                MessageBox.Show(""時間到！"", ""倒數計時器"",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}";
        }

        private string TemplateFileSearch()
        {
            return @"using System;
using System.Collections.Generic;
using System.IO;

namespace FileSearchTool
{
    /// <summary>
    /// 檔案搜尋工具 — 遞迴搜尋符合條件的檔案
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""=== 檔案搜尋工具 ==="");
            Console.Write(""搜尋目錄 (直接 Enter 用使用者目錄): "");
            string baseDir = Console.ReadLine();
            if (string.IsNullOrEmpty(baseDir))
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            Console.Write(""搜尋關鍵字: "");
            string keyword = Console.ReadLine();

            Console.Write(""最大搜尋深度 (1-10，預設 3): "");
            string depthStr = Console.ReadLine();
            int maxDepth = 3;
            int.TryParse(depthStr, out maxDepth);
            if (maxDepth < 1) maxDepth = 3;

            Console.WriteLine(string.Format(""\n搜尋中... (目錄: {0}, 關鍵字: {1})"", baseDir, keyword));

            List<string> results = new List<string>();
            Search(baseDir, keyword, results, maxDepth);

            Console.WriteLine(string.Format(""\n找到 {0} 個結果:"", results.Count));
            foreach (string file in results)
            {
                try
                {
                    FileInfo fi = new FileInfo(file);
                    Console.WriteLine(string.Format(""  {0} ({1:F1} KB)"",
                        file, fi.Length / 1024.0));
                }
                catch
                {
                    Console.WriteLine(""  "" + file);
                }
            }

            Console.WriteLine(""\n按任意鍵結束..."");
            Console.ReadKey();
        }

        static void Search(string path, string keyword, List<string> results, int depth)
        {
            if (depth <= 0 || results.Count >= 200) return;
            try
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    if (Path.GetFileName(file).ToLower().Contains(keyword.ToLower()))
                        results.Add(file);
                }
                foreach (string dir in Directory.GetDirectories(path))
                {
                    try { Search(dir, keyword, results, depth - 1); }
                    catch { }
                }
            }
            catch { }
        }
    }
}";
        }

        private string TemplateCsvProcessor()
        {
            return @"using System;
using System.Collections.Generic;
using System.IO;

namespace CsvProcessor
{
    /// <summary>
    /// CSV 讀取與統計工具
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""=== CSV 統計工具 ==="");
            Console.Write(""請輸入 CSV 檔案路徑: "");
            string filePath = Console.ReadLine();

            if (!File.Exists(filePath))
            {
                Console.WriteLine(""檔案不存在: "" + filePath);
                Console.ReadKey();
                return;
            }

            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
            {
                Console.WriteLine(""檔案為空"");
                Console.ReadKey();
                return;
            }

            // 顯示標頭
            string[] headers = lines[0].Split(',');
            Console.WriteLine(string.Format(""\n欄位數: {0}, 資料列數: {1}"",
                headers.Length, lines.Length - 1));
            Console.Write(""欄位: "");
            for (int i = 0; i < headers.Length; i++)
                Console.Write(string.Format(""[{0}]{1} "", i, headers[i].Trim()));
            Console.WriteLine();

            // 嘗試對數值欄位做統計
            Console.Write(""\n要統計哪個欄位 (輸入編號): "");
            string colStr = Console.ReadLine();
            int col = 0;
            int.TryParse(colStr, out col);

            if (col < 0 || col >= headers.Length)
            {
                Console.WriteLine(""欄位編號無效"");
                Console.ReadKey();
                return;
            }

            List<double> values = new List<double>();
            for (int i = 1; i < lines.Length; i++)
            {
                string[] cells = lines[i].Split(',');
                if (col < cells.Length)
                {
                    double v;
                    if (double.TryParse(cells[col].Trim(), out v))
                        values.Add(v);
                }
            }

            if (values.Count == 0)
            {
                Console.WriteLine(""該欄位無有效數值"");
            }
            else
            {
                double sum = 0;
                double min = values[0], max = values[0];
                foreach (double v in values)
                {
                    sum += v;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
                double avg = sum / values.Count;

                Console.WriteLine(string.Format(""\n欄位 [{0}] 統計:"", headers[col].Trim()));
                Console.WriteLine(string.Format(""  有效數值: {0} 個"", values.Count));
                Console.WriteLine(string.Format(""  總和: {0:F2}"", sum));
                Console.WriteLine(string.Format(""  平均: {0:F2}"", avg));
                Console.WriteLine(string.Format(""  最小: {0:F2}"", min));
                Console.WriteLine(string.Format(""  最大: {0:F2}"", max));
            }

            Console.WriteLine(""\n按任意鍵結束..."");
            Console.ReadKey();
        }
    }
}";
        }

        private string TemplateFileDownloader()
        {
            return @"using System;
using System.IO;
using System.Net;

namespace FileDownloader
{
    /// <summary>
    /// 檔案下載器
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""=== 檔案下載器 ==="");
            Console.Write(""請輸入下載 URL: "");
            string url = Console.ReadLine();

            if (string.IsNullOrEmpty(url))
            {
                Console.WriteLine(""URL 不可為空"");
                Console.ReadKey();
                return;
            }

            string fileName = Path.GetFileName(new Uri(url).LocalPath);
            if (string.IsNullOrEmpty(fileName)) fileName = ""download.bin"";

            Console.Write(string.Format(""儲存檔名 (預設 {0}): "", fileName));
            string inputName = Console.ReadLine();
            if (!string.IsNullOrEmpty(inputName)) fileName = inputName;

            string savePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

            Console.WriteLine(string.Format(""\n下載中: {0}"", url));
            Console.WriteLine(string.Format(""儲存到: {0}"", savePath));

            try
            {
                WebClient client = new WebClient();
                client.DownloadFile(url, savePath);

                FileInfo fi = new FileInfo(savePath);
                Console.WriteLine(string.Format(""\n✓ 下載完成！大小: {0:F2} MB"",
                    fi.Length / 1048576.0));
            }
            catch (Exception ex)
            {
                Console.WriteLine(""\n✗ 下載失敗: "" + ex.Message);
            }

            Console.WriteLine(""\n按任意鍵結束..."");
            Console.ReadKey();
        }
    }
}";
        }

        private string TemplateTextReplace()
        {
            return @"using System;
using System.IO;
using System.Text;

namespace TextReplace
{
    /// <summary>
    /// 文字批次取代工具
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""=== 文字批次取代工具 ==="");
            Console.Write(""請輸入檔案路徑: "");
            string filePath = Console.ReadLine();

            if (!File.Exists(filePath))
            {
                Console.WriteLine(""檔案不存在"");
                Console.ReadKey();
                return;
            }

            Console.Write(""要搜尋的文字: "");
            string search = Console.ReadLine();
            Console.Write(""取代為: "");
            string replace = Console.ReadLine();

            string content = File.ReadAllText(filePath, Encoding.UTF8);
            int count = 0;
            int index = 0;
            while ((index = content.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += search.Length;
            }

            if (count == 0)
            {
                Console.WriteLine(""\n找不到指定文字"");
            }
            else
            {
                Console.WriteLine(string.Format(""\n找到 {0} 處，確認取代？(Y/N): "", count));
                if (Console.ReadKey().Key == ConsoleKey.Y)
                {
                    string newContent = content.Replace(search, replace);
                    File.WriteAllText(filePath, newContent, Encoding.UTF8);
                    Console.WriteLine(string.Format(""\n✓ 已取代 {0} 處"", count));
                }
                else
                {
                    Console.WriteLine(""\n已取消"");
                }
            }

            Console.WriteLine(""\n按任意鍵結束..."");
            Console.ReadKey();
        }
    }
}";
        }

        private string TemplateSystemMonitor()
        {
            return @"using System;
using System.Diagnostics;
using System.Threading;

namespace SystemMonitor
{
    /// <summary>
    /// 系統資源監控 — 即時顯示 CPU 和記憶體使用狀況
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""=== 系統資源監控 ==="");
            Console.WriteLine(""按 Ctrl+C 結束\n"");

            PerformanceCounter cpuCounter = new PerformanceCounter(
                ""Processor"", ""% Processor Time"", ""_Total"");
            cpuCounter.NextValue(); // 第一次讀取會是 0

            Thread.Sleep(1000);

            while (true)
            {
                float cpu = cpuCounter.NextValue();
                long memUsed = GC.GetTotalMemory(false);
                Process current = Process.GetCurrentProcess();

                Console.Write(string.Format(
                    ""\rCPU: {0,5:F1}% | 系統程序數: {1} | 本程序記憶體: {2:F1} MB    "",
                    cpu,
                    Process.GetProcesses().Length,
                    current.WorkingSet64 / 1048576.0));

                Thread.Sleep(1000);
            }
        }
    }
}";
        }

        private string TemplateSimpleNotepad()
        {
            return @"using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SimpleNotepad
{
    /// <summary>
    /// 簡易記事本
    /// </summary>
    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new NotepadForm());
        }
    }

    class NotepadForm : Form
    {
        private TextBox txtContent;
        private MenuStrip menu;
        private string currentFile = """";

        public NotepadForm()
        {
            this.Text = ""簡易記事本"";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            menu = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem(""檔案"");
            fileMenu.DropDownItems.Add(""開啟"", null, delegate { OpenFile(); });
            fileMenu.DropDownItems.Add(""儲存"", null, delegate { SaveFile(); });
            fileMenu.DropDownItems.Add(""另存為"", null, delegate { SaveFileAs(); });
            fileMenu.DropDownItems.Add(""-"");
            fileMenu.DropDownItems.Add(""離開"", null, delegate { this.Close(); });
            menu.Items.Add(fileMenu);

            txtContent = new TextBox();
            txtContent.Multiline = true;
            txtContent.ScrollBars = ScrollBars.Both;
            txtContent.Dock = DockStyle.Fill;
            txtContent.Font = new Font(""Consolas"", 11);
            txtContent.AcceptsTab = true;

            this.MainMenuStrip = menu;
            this.Controls.Add(txtContent);
            this.Controls.Add(menu);
        }

        private void OpenFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = ""文字檔|*.txt|所有檔案|*.*"";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                currentFile = dlg.FileName;
                txtContent.Text = File.ReadAllText(currentFile);
                this.Text = ""簡易記事本 - "" + Path.GetFileName(currentFile);
            }
        }

        private void SaveFile()
        {
            if (string.IsNullOrEmpty(currentFile))
                SaveFileAs();
            else
                File.WriteAllText(currentFile, txtContent.Text);
        }

        private void SaveFileAs()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = ""文字檔|*.txt|所有檔案|*.*"";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                currentFile = dlg.FileName;
                File.WriteAllText(currentFile, txtContent.Text);
                this.Text = ""簡易記事本 - "" + Path.GetFileName(currentFile);
            }
        }
    }
}";
        }
    }

    // ============================================================
    // 模板項目類別
    // ============================================================

    /// <summary>
    /// 模板項目
    /// </summary>
    public class TemplateEntry
    {
        public string Description { get; private set; }
        public string[] Keywords { get; private set; }

        private readonly Func<string, string> _generator;

        public TemplateEntry(string description, string[] keywords,
            Func<string, string> generator)
        {
            Description = description;
            Keywords = keywords;
            _generator = generator;
        }

        public string GenerateCode(string description)
        {
            return _generator(description);
        }
    }
}
