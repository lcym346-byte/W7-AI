using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;


namespace AIAgentTool.Services.System
{
    /// <summary>
    /// 程序管理服務 — 啟動/關閉/列舉程序 + 視窗管理
    /// Windows 7 相容 (P/Invoke + .NET 4.0)
    /// </summary>
    public class ProcessManagerService
    {
        // ============================================================
        // Win32 API P/Invoke
        // ============================================================

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(
            IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(
            EnumWindowsDelegate enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd, out uint processId);

        private delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);

        private const int SW_HIDE = 0;
        private const int SW_NORMAL = 1;
        private const int SW_MINIMIZE = 6;
        private const int SW_MAXIMIZE = 3;
        private const int SW_RESTORE = 9;

        // ============================================================
        // 程式中文別名對照表
        // ============================================================

        private static readonly Dictionary<string, string> AppAliases =
            CreateAliases();

        private static Dictionary<string, string> CreateAliases()
        {
            Dictionary<string, string> d = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            // 系統工具
            d["記事本"] = "notepad.exe";
            d["notepad"] = "notepad.exe";
            d["計算機"] = "calc.exe";
            d["calculator"] = "calc.exe";
            d["calc"] = "calc.exe";
            d["小畫家"] = "mspaint.exe";
            d["paint"] = "mspaint.exe";
            d["命令提示字元"] = "cmd.exe";
            d["cmd"] = "cmd.exe";
            d["檔案總管"] = "explorer.exe";
            d["explorer"] = "explorer.exe";
            d["工作管理員"] = "taskmgr.exe";
            d["taskmgr"] = "taskmgr.exe";
            d["控制台"] = "control.exe";
            d["control"] = "control.exe";
            d["遠端桌面"] = "mstsc.exe";
            d["mstsc"] = "mstsc.exe";
            d["截圖工具"] = "SnippingTool.exe";
            d["snip"] = "SnippingTool.exe";
            d["寫字板"] = "wordpad.exe";
            d["wordpad"] = "wordpad.exe";
            d["放大鏡"] = "magnify.exe";
            d["螢幕小鍵盤"] = "osk.exe";
            d["osk"] = "osk.exe";
            d["登錄編輯程式"] = "regedit.exe";
            d["regedit"] = "regedit.exe";

            // 瀏覽器
            d["chrome"] = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            d["google"] = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            d["firefox"] = @"C:\Program Files\Mozilla Firefox\firefox.exe";
            d["ie"] = "iexplore.exe";
            d["edge"] = "msedge.exe";

            // Office
            d["word"] = "WINWORD.EXE";
            d["excel"] = "EXCEL.EXE";
            d["powerpoint"] = "POWERPNT.EXE";
            d["ppt"] = "POWERPNT.EXE";
            d["outlook"] = "OUTLOOK.EXE";

            // 多媒體
            d["媒體播放器"] = "wmplayer.exe";
            d["wmplayer"] = "wmplayer.exe";

            return d;
        }

        // ============================================================
        // 啟動程式
        // ============================================================

        /// <summary>
        /// 智慧啟動程式 — 支援中文別名、路徑、URL
        /// </summary>
                /// <summary>
        /// 啟動程式（無參數版本）
        /// </summary>
        public string LaunchApplication(string appName)
        {
            return LaunchApplication(appName, null);
        }


        public string LaunchApplication(string appName, string arguments)
        {
            StringBuilder sb = new StringBuilder();
            appName = appName.Trim();

            try
            {
                string resolvedPath = ResolveAppPath(appName);

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = resolvedPath;
                startInfo.Arguments = arguments ?? "";
                startInfo.UseShellExecute = true;

                sb.AppendLine(string.Format("正在啟動: {0}", resolvedPath));
                if (!string.IsNullOrEmpty(arguments))
                    sb.AppendLine(string.Format("參數: {0}", arguments));

                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    sb.AppendLine(string.Format("✓ 成功啟動！PID: {0}", process.Id));
                    sb.AppendLine(string.Format("  程序名稱: {0}", process.ProcessName));

                    try
                    {
                        process.WaitForInputIdle(3000);
                        if (!string.IsNullOrEmpty(process.MainWindowTitle))
                            sb.AppendLine(string.Format("  視窗標題: {0}",
                                process.MainWindowTitle));
                    }
                    catch { }
                }
                else
                {
                    sb.AppendLine("✓ 已使用系統 Shell 啟動");
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                sb.AppendLine(string.Format("✗ 啟動失敗: {0}", ex.Message));
                sb.AppendLine(SuggestAlternatives(appName));
            }
            catch (Exception ex)
            {
                sb.AppendLine(string.Format("✗ 錯誤: {0}", ex.Message));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 開啟檔案 (用預設程式)
        /// </summary>
        public string OpenFile(string filePath)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                filePath = Environment.ExpandEnvironmentVariables(filePath.Trim());

                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                {
                    sb.AppendLine(string.Format("✗ 找不到: {0}", filePath));
                    return sb.ToString();
                }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = filePath;
                psi.UseShellExecute = true;
                Process.Start(psi);

                sb.AppendLine(string.Format("✓ 已用預設程式開啟: {0}", filePath));
            }
            catch (Exception ex)
            {
                sb.AppendLine(string.Format("✗ 開啟失敗: {0}", ex.Message));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 開啟網址
        /// </summary>
        public string OpenUrl(string url)
        {
            try
            {
                if (!url.StartsWith("http"))
                    url = "https://" + url;

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = url;
                psi.UseShellExecute = true;
                Process.Start(psi);

                return string.Format("✓ 已在瀏覽器開啟: {0}", url);
            }
            catch (Exception ex)
            {
                return string.Format("✗ 開啟失敗: {0}", ex.Message);
            }
        }

        // ============================================================
        // 關閉程式
        // ============================================================

        /// <summary>
        /// 關閉程式 (優先正常關閉，逾時才強制終止)
        /// </summary>
        public string CloseApplication(string appName)
        {
            StringBuilder sb = new StringBuilder();
            appName = appName.Trim().ToLower()
                .Replace(".exe", "").Replace("關閉", "")
                .Replace("結束", "").Replace("停止", "").Trim();

            string processName = appName;
            if (AppAliases.ContainsKey(appName))
            {
                processName = Path.GetFileNameWithoutExtension(AppAliases[appName]);
            }

            try
            {
                Process[] processes = Process.GetProcessesByName(processName);

                // 模糊搜尋
                if (processes.Length == 0)
                {
                    List<Process> found = new List<Process>();
                    foreach (Process p in Process.GetProcesses())
                    {
                        try
                        {
                            if (p.ProcessName.ToLower().Contains(processName) ||
                                (!string.IsNullOrEmpty(p.MainWindowTitle) &&
                                 p.MainWindowTitle.ToLower().Contains(processName)))
                            {
                                found.Add(p);
                            }
                        }
                        catch { }
                    }
                    processes = found.ToArray();
                }

                if (processes.Length == 0)
                {
                    sb.AppendLine(string.Format(
                        "找不到名為 \"{0}\" 的執行中程序", appName));
                    return sb.ToString();
                }

                sb.AppendLine(string.Format("找到 {0} 個相關程序：", processes.Length));
                int closed = 0;

                foreach (Process proc in processes)
                {
                    try
                    {
                        string info = string.Format("  PID {0}: {1}",
                            proc.Id, proc.ProcessName);

                        if (proc.MainWindowHandle != IntPtr.Zero)
                        {
                            proc.CloseMainWindow();
                            bool exited = proc.WaitForExit(3000);
                            if (exited)
                            {
                                sb.AppendLine(info + " → ✓ 已正常關閉");
                                closed++;
                                continue;
                            }
                        }

                        proc.Kill();
                        proc.WaitForExit(2000);
                        sb.AppendLine(info + " → ✓ 已強制終止");
                        closed++;
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine(string.Format("  PID {0} → ✗ 失敗: {1}",
                            proc.Id, ex.Message));
                    }
                }

                sb.AppendLine(string.Format("\n共關閉 {0}/{1} 個程序",
                    closed, processes.Length));
            }
            catch (Exception ex)
            {
                sb.AppendLine(string.Format("✗ 操作失敗: {0}", ex.Message));
            }

            return sb.ToString();
        }

        // ============================================================
        // 列舉程序
        // ============================================================

        /// <summary>
        /// 列出所有執行中程序
        /// </summary>
                /// <summary>
        /// 列出程序（預設不顯示詳細）
        /// </summary>
        public string ListRunningProcesses()
        {
            return ListRunningProcesses(false);
        }


        public string ListRunningProcesses(bool detailed)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("═══ 執行中的程序 ═══");
            sb.AppendLine(string.Format("時間: {0}\n",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

            Process[] allProcesses = Process.GetProcesses();

            // 有視窗的程序
            List<Process> windowProcs = new List<Process>();
            foreach (Process p in allProcesses)
            {
                try
                {
                    if (p.MainWindowHandle != IntPtr.Zero &&
                        !string.IsNullOrEmpty(p.MainWindowTitle))
                    {
                        windowProcs.Add(p);
                    }
                }
                catch { }
            }

            sb.AppendLine(string.Format("【有視窗的程式】 ({0} 個)", windowProcs.Count));
            sb.AppendLine(string.Format("{0,-8} {1,-20} {2,-10} {3}",
                "PID", "名稱", "記憶體MB", "視窗標題"));
            sb.AppendLine(new string('-', 75));

            foreach (Process proc in windowProcs)
            {
                try
                {
                    sb.AppendLine(string.Format("{0,-8} {1,-20} {2,-10:F1} {3}",
                        proc.Id,
                        Truncate(proc.ProcessName, 18),
                        proc.WorkingSet64 / 1048576.0,
                        Truncate(proc.MainWindowTitle, 30)));
                }
                catch { }
            }

            if (detailed)
            {
                int bgCount = 0;
                sb.AppendLine("\n【背景程序】 (前 30 個)");
                sb.AppendLine(string.Format("{0,-8} {1,-25} {2,-10}",
                    "PID", "名稱", "記憶體MB"));
                sb.AppendLine(new string('-', 50));

                foreach (Process proc in allProcesses)
                {
                    if (windowProcs.Contains(proc)) continue;
                    if (bgCount >= 30) break;
                    try
                    {
                        sb.AppendLine(string.Format("{0,-8} {1,-25} {2,-10:F1}",
                            proc.Id, proc.ProcessName,
                            proc.WorkingSet64 / 1048576.0));
                        bgCount++;
                    }
                    catch { }
                }
            }

            sb.AppendLine(string.Format("\n程序總數: {0} | 有視窗: {1}",
                allProcesses.Length, windowProcs.Count));

            return sb.ToString();
        }

        /// <summary>
        /// 搜尋特定程序
        /// </summary>
        public string FindProcess(string keyword)
        {
            StringBuilder sb = new StringBuilder();
            keyword = keyword.ToLower().Trim();

            List<Process> found = new List<Process>();
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (p.ProcessName.ToLower().Contains(keyword) ||
                        (!string.IsNullOrEmpty(p.MainWindowTitle) &&
                         p.MainWindowTitle.ToLower().Contains(keyword)))
                    {
                        found.Add(p);
                    }
                }
                catch { }
            }

            if (found.Count == 0)
            {
                sb.AppendLine(string.Format("找不到包含 \"{0}\" 的程序", keyword));
            }
            else
            {
                sb.AppendLine(string.Format("找到 {0} 個匹配程序：\n", found.Count));
                foreach (Process proc in found)
                {
                    try
                    {
                        sb.AppendLine(string.Format(
                            "  PID: {0} | 名稱: {1} | 記憶體: {2:F1} MB | 視窗: {3}",
                            proc.Id, proc.ProcessName,
                            proc.WorkingSet64 / 1048576.0,
                            string.IsNullOrEmpty(proc.MainWindowTitle)
                                ? "(無)" : proc.MainWindowTitle));
                    }
                    catch { }
                }
            }
            return sb.ToString();
        }

        // ============================================================
        // 視窗管理
        // ============================================================

        /// <summary>
        /// 視窗操作
        /// </summary>
        public string ManageWindow(string processName, string action)
        {
            StringBuilder sb = new StringBuilder();
            processName = processName.Trim().ToLower();
            action = action.Trim().ToLower();

            IntPtr targetWindow = FindTargetWindow(processName);

            if (targetWindow == IntPtr.Zero)
            {
                sb.AppendLine(string.Format("找不到 \"{0}\" 的視窗", processName));
                return sb.ToString();
            }

            StringBuilder titleBuf = new StringBuilder(256);
            GetWindowText(targetWindow, titleBuf, 256);
            sb.AppendLine(string.Format("目標視窗: {0}", titleBuf.ToString()));

            try
            {
                if (action.Contains("最大化") || action == "maximize" || action == "max")
                {
                    ShowWindow(targetWindow, SW_MAXIMIZE);
                    sb.AppendLine("✓ 已最大化");
                }
                else if (action.Contains("最小化") || action == "minimize" || action == "min")
                {
                    ShowWindow(targetWindow, SW_MINIMIZE);
                    sb.AppendLine("✓ 已最小化");
                }
                else if (action.Contains("還原") || action == "restore")
                {
                    ShowWindow(targetWindow, SW_RESTORE);
                    sb.AppendLine("✓ 已還原");
                }
                else if (action.Contains("置前") || action.Contains("前景") ||
                         action == "focus" || action == "activate")
                {
                    if (IsIconic(targetWindow))
                        ShowWindow(targetWindow, SW_RESTORE);
                    SetForegroundWindow(targetWindow);
                    sb.AppendLine("✓ 已設為前景視窗");
                }
                else if (action.Contains("隱藏") || action == "hide")
                {
                    ShowWindow(targetWindow, SW_HIDE);
                    sb.AppendLine("✓ 已隱藏");
                }
                else
                {
                    sb.AppendLine("支援的動作: 最大化, 最小化, 還原, 置前, 隱藏");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine(string.Format("✗ 操作失敗: {0}", ex.Message));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 列出所有可見視窗
        /// </summary>
        public string ListAllWindows()
        {
            StringBuilder sb = new StringBuilder();
            List<string> windowList = new List<string>();

            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder titleBuf = new StringBuilder(256);
                    GetWindowText(hWnd, titleBuf, 256);
                    string title = titleBuf.ToString();
                    if (!string.IsNullOrEmpty(title) && title.Trim().Length > 0)
                    {
                        uint pid;
                        GetWindowThreadProcessId(hWnd, out pid);
                        windowList.Add(string.Format("  PID {0,-6} | {1}",
                            pid, title));
                    }
                }
                return true;
            }, IntPtr.Zero);

            sb.AppendLine(string.Format("═══ 所有可見視窗 ({0} 個) ═══\n",
                windowList.Count));
            foreach (string w in windowList)
            {
                sb.AppendLine(w);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 列出已安裝程式
        /// </summary>
                /// <summary>
        /// 列出已安裝程式（無篩選）
        /// </summary>
        public string ListInstalledPrograms()
        {
            return ListInstalledPrograms(null);
        }


        public string ListInstalledPrograms(string filter)
        {
            StringBuilder sb = new StringBuilder();
            List<string> programs = new List<string>();

            string[] registryPaths = new string[]
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (string path in registryPaths)
            {
                try
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(path);
                    if (key == null) continue;

                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            RegistryKey subKey = key.OpenSubKey(subKeyName);
                            if (subKey == null) continue;

                            object nameObj = subKey.GetValue("DisplayName");
                            if (nameObj == null) continue;
                            string name = nameObj.ToString();
                            if (string.IsNullOrEmpty(name)) continue;

                            // 篩選
                            if (!string.IsNullOrEmpty(filter) &&
                                !name.ToLower().Contains(filter.ToLower()))
                                continue;

                            // 避免重複
                            if (programs.Contains(name)) continue;

                            string version = "";
                            object verObj = subKey.GetValue("DisplayVersion");
                            if (verObj != null) version = verObj.ToString();

                            programs.Add(name);
                            // 不在這裡格式化，只收集名稱
                        }
                        catch { }
                    }
                }
                catch { }
            }

            programs.Sort();

            sb.AppendLine(string.Format("═══ 已安裝程式 ({0} 個) ═══",
                programs.Count));
            if (!string.IsNullOrEmpty(filter))
                sb.AppendLine(string.Format("篩選: \"{0}\"", filter));
            sb.AppendLine();

            foreach (string prog in programs)
            {
                sb.AppendLine(string.Format("  • {0}", prog));
            }

            return sb.ToString();
        }

        // ============================================================
        // 輔助方法
        // ============================================================

        private string ResolveAppPath(string appName)
        {
            // 別名
            if (AppAliases.ContainsKey(appName))
                return AppAliases[appName];

            // 完整路徑
            if (File.Exists(appName))
                return appName;

            // 補 .exe
            string withExe = appName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? appName : appName + ".exe";

            // 在 PATH 中搜尋
            string pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string dir in pathVar.Split(';'))
            {
                try
                {
                    string fullPath = Path.Combine(dir.Trim(), withExe);
                    if (File.Exists(fullPath)) return fullPath;
                }
                catch { }
            }

            return appName;
        }

        private IntPtr FindTargetWindow(string processName)
        {
            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    if ((proc.ProcessName.ToLower().Contains(processName) ||
                        (!string.IsNullOrEmpty(proc.MainWindowTitle) &&
                         proc.MainWindowTitle.ToLower().Contains(processName)))
                        && proc.MainWindowHandle != IntPtr.Zero)
                    {
                        return proc.MainWindowHandle;
                    }
                }
                catch { }
            }
            return IntPtr.Zero;
        }

        private string SuggestAlternatives(string appName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n可用的程式別名：");
            int count = 0;
            foreach (string key in AppAliases.Keys)
            {
                if (count >= 5) break;
                if (key.Length >= 2 && appName.Length >= 2 &&
                    (key.ToLower().Contains(appName.ToLower().Substring(0, 2)) ||
                     appName.ToLower().Contains(key.ToLower().Substring(0, 2))))
                {
                    sb.AppendLine(string.Format("  • {0} → {1}", key, AppAliases[key]));
                    count++;
                }
            }
            if (count == 0)
                sb.AppendLine("  輸入「已安裝程式」查看所有可用程式");
            return sb.ToString();
        }

        private string Truncate(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLen) return text;
            return text.Substring(0, maxLen - 2) + "..";
        }
    }
}
