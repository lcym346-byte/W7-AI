using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace AIAgentTool.Services.System
{
    public class SystemAutomationService
    {
        private static readonly HashSet<string> AllowedCommands = CreateAllowed();
        private static readonly HashSet<string> BlockedCommands = CreateBlocked();

        private static HashSet<string> CreateAllowed()
        {
            HashSet<string> s = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] cmds = new string[] {
                "ipconfig", "hostname", "whoami", "systeminfo", "ver",
                "date", "time", "set", "path", "echo",
                "dir", "tree", "type", "find", "findstr",
                "tasklist", "wmic", "netstat", "ping",
                "tracert", "nslookup", "arp",
                "chcp", "help", "vol",
                "driverquery", "getmac", "gpresult",
                "where", "assoc", "ftype"
            };
            foreach (string c in cmds) s.Add(c);
            return s;
        }

        private static HashSet<string> CreateBlocked()
        {
            HashSet<string> s = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] cmds = new string[] {
                "del", "erase", "rmdir", "rd", "format", "fdisk",
                "reg", "regedit", "shutdown", "restart",
                "net", "netsh", "sc", "bcdedit", "diskpart",
                "cipher", "takeown", "icacls", "cacls",
                "powershell", "wscript", "cscript", "mshta",
                "attrib", "sfc", "chkdsk"
            };
            foreach (string c in cmds) s.Add(c);
            return s;
        }

        private readonly Models.SafetyLevel _safetyLevel;

        public SystemAutomationService(Models.SafetyLevel safetyLevel)
        {
            _safetyLevel = safetyLevel;
        }

        public SystemAutomationService(Models.AppSettings settings)
        {
            _safetyLevel = settings.Safety;
        }

        public string ExecuteCommand(string command)
        {
            StringBuilder sb = new StringBuilder();
            command = command.Trim();

            string safetyResult = CheckCommandSafety(command);
            if (safetyResult != null)
            {
                sb.AppendLine(safetyResult);
                return sb.ToString();
            }

            try
            {
                sb.AppendLine(string.Format("執行: {0}", command));
                sb.AppendLine(new string('-', 50));

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = "/C " + command;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;

                try
                {
                    startInfo.StandardOutputEncoding = Encoding.GetEncoding(950);
                    startInfo.StandardErrorEncoding = Encoding.GetEncoding(950);
                }
                catch
                {
                    startInfo.StandardOutputEncoding = Encoding.UTF8;
                    startInfo.StandardErrorEncoding = Encoding.UTF8;
                }

                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit(30000);

                if (!string.IsNullOrEmpty(output))
                    sb.AppendLine(output);

                if (!string.IsNullOrEmpty(error))
                {
                    sb.AppendLine("[錯誤輸出]");
                    sb.AppendLine(error);
                }

                sb.AppendLine(new string('-', 50));
                sb.AppendLine(string.Format("結束代碼: {0}", process.ExitCode));
            }
            catch (Exception ex)
            {
                sb.AppendLine(string.Format("✗ 命令執行失敗: {0}", ex.Message));
            }

            return sb.ToString();
        }

        public string QuickCommand(string alias)
        {
            alias = alias.Trim().ToLower();

            Dictionary<string, string> commandMap = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            commandMap["ip"] = "ipconfig /all";
            commandMap["ip位址"] = "ipconfig /all";
            commandMap["網路"] = "ipconfig /all";
            commandMap["network"] = "ipconfig /all";
            commandMap["dns"] = "ipconfig /displaydns";
            commandMap["連線"] = "netstat -an";
            commandMap["connections"] = "netstat -an";
            commandMap["ping"] = "ping 8.8.8.8 -n 4";
            commandMap["任務"] = "tasklist /fo table";
            commandMap["tasks"] = "tasklist /fo table";
            commandMap["驅動"] = "driverquery";
            commandMap["drivers"] = "driverquery";
            commandMap["mac"] = "getmac";
            commandMap["環境變數"] = "set";
            commandMap["使用者"] = "whoami /all";
            commandMap["系統版本"] = "ver";
            commandMap["磁碟"] = "wmic logicaldisk get caption,freespace,size";
            commandMap["啟動項"] = "wmic startup list brief";
            commandMap["cpu"] = "wmic cpu get name,numberofcores,maxclockspeed";
            commandMap["記憶體"] = "wmic memorychip get capacity,speed,manufacturer";
            commandMap["主機板"] = "wmic baseboard get manufacturer,product";
            commandMap["路由"] = "tracert 8.8.8.8";

            if (commandMap.ContainsKey(alias))
                return ExecuteCommand(commandMap[alias]);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("未知的快速命令: {0}\n", alias));
            sb.AppendLine("可用的快速命令:");
            foreach (string key in commandMap.Keys)
            {
                sb.AppendLine(string.Format("  • {0} → {1}", key, commandMap[key]));
            }
            return sb.ToString();
        }

        public string CaptureScreen(string savePath)
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                if (string.IsNullOrEmpty(savePath))
                {
                    savePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        string.Format("Screenshot_{0}.png",
                            DateTime.Now.ToString("yyyyMMdd_HHmmss")));
                }

                bitmap.Save(savePath, ImageFormat.Png);
                bitmap.Dispose();

                return string.Format("✓ 螢幕截圖已儲存: {0}\n  解析度: {1}x{2}",
                    savePath, bounds.Width, bounds.Height);
            }
            catch (Exception ex)
            {
                return string.Format("✗ 截圖失敗: {0}", ex.Message);
            }
        }

        public string CaptureScreen()
        {
            return CaptureScreen(null);
        }

        public string GetClipboardText()
        {
            try
            {
                string text = "";
                Thread thread = new Thread(delegate()
                {
                    try
                    {
                        if (Clipboard.ContainsText())
                            text = Clipboard.GetText();
                        else if (Clipboard.ContainsImage())
                            text = "[剪貼簿包含圖片]";
                        else if (Clipboard.ContainsFileDropList())
                        {
                            StringCollection files = Clipboard.GetFileDropList();
                            StringBuilder sb2 = new StringBuilder("[剪貼簿包含檔案]\n");
                            foreach (string f in files)
                                sb2.AppendLine("  " + f);
                            text = sb2.ToString();
                        }
                        else
                            text = "[剪貼簿為空]";
                    }
                    catch (Exception ex)
                    {
                        text = "讀取失敗: " + ex.Message;
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(3000);

                return string.Format("═══ 剪貼簿內容 ═══\n{0}", text);
            }
            catch (Exception ex)
            {
                return string.Format("✗ 讀取剪貼簿失敗: {0}", ex.Message);
            }
        }

        public string SetClipboardText(string text)
        {
            try
            {
                Thread thread = new Thread(delegate()
                {
                    Clipboard.SetText(text);
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(3000);

                return string.Format("✓ 已複製到剪貼簿 ({0} 字元)", text.Length);
            }
            catch (Exception ex)
            {
                return string.Format("✗ 複製失敗: {0}", ex.Message);
            }
        }

        public string GetClipboard()
        {
            return GetClipboardText();
        }

        public string SetClipboard(string text)
        {
            return SetClipboardText(text);
        }

        public string GetSystemInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("═══ 系統資訊 ═══");
            sb.AppendLine(string.Format("作業系統: {0}", Environment.OSVersion));
            sb.AppendLine(string.Format("電腦名稱: {0}", Environment.MachineName));
            sb.AppendLine(string.Format("使用者: {0}", Environment.UserName));
            sb.AppendLine(string.Format("處理器核心數: {0}", Environment.ProcessorCount));
            sb.AppendLine(string.Format(".NET 版本: {0}", Environment.Version));
            sb.AppendLine(string.Format("64位元系統: {0}", Environment.Is64BitOperatingSystem));
            sb.AppendLine(string.Format("系統目錄: {0}",
                Environment.GetFolderPath(Environment.SpecialFolder.System)));
            sb.AppendLine(string.Format("系統執行時間: {0}",
                TimeSpan.FromMilliseconds(Environment.TickCount)));

            sb.AppendLine("\n═══ 磁碟資訊 ═══");
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.IsReady)
                    {
                        sb.AppendLine(string.Format("{0} [{1}] {2:F1} GB 可用 / {3:F1} GB 總計",
                            drive.Name, drive.DriveFormat,
                            drive.AvailableFreeSpace / 1073741824.0,
                            drive.TotalSize / 1073741824.0));
                    }
                }
                catch { }
            }

            return sb.ToString();
        }

        private string CheckCommandSafety(string command)
        {
            string[] parts = command.Split(new char[] { ' ', '/' },
                StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "✗ 空命令";

            string baseCommand = parts[0].ToLower();

            if (BlockedCommands.Contains(baseCommand))
            {
                return string.Format(
                    "✗ 安全限制：命令 \"{0}\" 被禁止執行\n" +
                    "此命令可能對系統造成更改。\n" +
                    "允許的命令：資訊查詢、網路診斷類", baseCommand);
            }

            if (_safetyLevel == Models.SafetyLevel.Strict)
            {
                if (!AllowedCommands.Contains(baseCommand))
                {
                    return string.Format(
                        "✗ 嚴格模式：命令 \"{0}\" 不在白名單中\n" +
                        "請在設定中調整安全等級，或使用已知的安全命令。", baseCommand);
                }
            }

            string lowerCmd = command.ToLower();
            if ((lowerCmd.Contains("|") && ContainsAny(lowerCmd, "del", "format", "rd")) ||
                (lowerCmd.Contains(">") && !lowerCmd.Contains("find")) ||
                (lowerCmd.Contains("&") && ContainsAny(lowerCmd, "del", "format", "shutdown")))
            {
                return "✗ 安全限制：包含潛在危險的管道/重導向操作";
            }

            return null;
        }

        private bool ContainsAny(string text, params string[] keywords)
        {
            foreach (string kw in keywords)
            {
                if (text.Contains(kw)) return true;
            }
            return false;
        }
    }
}
