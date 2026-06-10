using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AIAgentTool.Services.Desktop
{
    /// <summary>
    /// 桌面自動化服務 - 參考 Windows-Use 概念
    /// 提供螢幕截圖、滑鼠點擊、鍵盤輸入、視窗管理等功能
    /// 使用 Windows API（支援 Windows 7+）
    /// </summary>
    public class DesktopAutomation
    {
        // ========== Windows API 宣告 ==========

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        // 滑鼠事件常數
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        // ShowWindow 常數
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;
        private const int SW_MAXIMIZE = 3;
        private const int SW_SHOW = 5;

        public string LastError { get; private set; }

        // ========== 螢幕截圖 ==========

        /// <summary>
        /// 擷取全螢幕截圖
        /// </summary>
        /// <param name="savePath">儲存路徑（資料夾）</param>
        /// <returns>截圖檔案路徑</returns>
        public string CaptureScreen(string savePath)
        {
            try
            {
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                string fileName = string.Format("screenshot_{0}.png",
                    DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                string filePath = Path.Combine(savePath, fileName);
                bmp.Save(filePath, ImageFormat.Png);
                bmp.Dispose();

                return filePath;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 擷取指定區域截圖
        /// </summary>
        public string CaptureRegion(string savePath, int x, int y, int width, int height)
        {
            try
            {
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);

                Bitmap bmp = new Bitmap(width, height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(x, y, 0, 0, new Size(width, height));
                }

                string fileName = string.Format("region_{0}.png",
                    DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                string filePath = Path.Combine(savePath, fileName);
                bmp.Save(filePath, ImageFormat.Png);
                bmp.Dispose();

                return filePath;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 擷取指定視窗截圖
        /// </summary>
        public string CaptureWindow(string savePath, string windowTitle)
        {
            try
            {
                IntPtr hWnd = FindWindowByTitle(windowTitle);
                if (hWnd == IntPtr.Zero)
                {
                    LastError = "Window not found: " + windowTitle;
                    return null;
                }

                RECT rect;
                GetWindowRect(hWnd, out rect);
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (width <= 0 || height <= 0)
                {
                    LastError = "Window has invalid size";
                    return null;
                }

                return CaptureRegion(savePath, rect.Left, rect.Top, width, height);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        // ========== 滑鼠操作 ==========

        /// <summary>
        /// 移動滑鼠到指定座標並左鍵點擊
        /// </summary>
        public bool Click(int x, int y)
        {
            try
            {
                SetCursorPos(x, y);
                System.Threading.Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 右鍵點擊
        /// </summary>
        public bool RightClick(int x, int y)
        {
            try
            {
                SetCursorPos(x, y);
                System.Threading.Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_RIGHTDOWN, x, y, 0, 0);
                mouse_event(MOUSEEVENTF_RIGHTUP, x, y, 0, 0);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 雙擊
        /// </summary>
        public bool DoubleClick(int x, int y)
        {
            Click(x, y);
            System.Threading.Thread.Sleep(80);
            Click(x, y);
            return true;
        }

        /// <summary>
        /// 移動滑鼠（不點擊）
        /// </summary>
        public bool MoveMouse(int x, int y)
        {
            try
            {
                SetCursorPos(x, y);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        // ========== 鍵盤操作 ==========

        /// <summary>
        /// 輸入文字（使用 SendKeys）
        /// </summary>
        public bool TypeText(string text)
        {
            try
            {
                SendKeys.SendWait(text);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 發送快捷鍵
        /// 例如: "^c" = Ctrl+C, "%{F4}" = Alt+F4, "{ENTER}" = Enter
        /// </summary>
        public bool SendShortcut(string keys)
        {
            try
            {
                SendKeys.SendWait(keys);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        // ========== 視窗管理 ==========

        /// <summary>
        /// 列出所有可見視窗
        /// </summary>
        public List<WindowInfo> ListWindows()
        {
            List<WindowInfo> windows = new List<WindowInfo>();

            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder title = new StringBuilder(256);
                GetWindowText(hWnd, title, 256);
                string windowTitle = title.ToString();

                if (string.IsNullOrEmpty(windowTitle)) return true;

                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);

                RECT rect;
                GetWindowRect(hWnd, out rect);

                windows.Add(new WindowInfo
                {
                    Handle = hWnd,
                    Title = windowTitle,
                    ProcessId = (int)processId,
                    X = rect.Left,
                    Y = rect.Top,
                    Width = rect.Right - rect.Left,
                    Height = rect.Bottom - rect.Top
                });

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary>
        /// 切換到指定視窗（帶到前景）
        /// </summary>
        public bool FocusWindow(string titleContains)
        {
            try
            {
                IntPtr hWnd = FindWindowByTitle(titleContains);
                if (hWnd == IntPtr.Zero)
                {
                    LastError = "Window not found: " + titleContains;
                    return false;
                }

                ShowWindow(hWnd, SW_RESTORE);
                SetForegroundWindow(hWnd);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 最小化視窗
        /// </summary>
        public bool MinimizeWindow(string titleContains)
        {
            IntPtr hWnd = FindWindowByTitle(titleContains);
            if (hWnd == IntPtr.Zero) return false;
            ShowWindow(hWnd, SW_MINIMIZE);
            return true;
        }

        /// <summary>
        /// 最大化視窗
        /// </summary>
        public bool MaximizeWindow(string titleContains)
        {
            IntPtr hWnd = FindWindowByTitle(titleContains);
            if (hWnd == IntPtr.Zero) return false;
            ShowWindow(hWnd, SW_MAXIMIZE);
            return true;
        }

        /// <summary>
        /// 移動/調整視窗大小
        /// </summary>
        public bool MoveResizeWindow(string titleContains, int x, int y, int width, int height)
        {
            IntPtr hWnd = FindWindowByTitle(titleContains);
            if (hWnd == IntPtr.Zero) return false;
            MoveWindow(hWnd, x, y, width, height, true);
            return true;
        }

        /// <summary>
        /// 取得目前前景視窗資訊
        /// </summary>
        public string GetActiveWindowTitle()
        {
            IntPtr hWnd = GetForegroundWindow();
            StringBuilder title = new StringBuilder(256);
            GetWindowText(hWnd, title, 256);
            return title.ToString();
        }

        // ========== 應用程式啟動 ==========

        /// <summary>
        /// 啟動應用程式
        /// </summary>
        public bool LaunchApp(string path, string arguments = "")
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = path;
                if (!string.IsNullOrEmpty(arguments))
                    psi.Arguments = arguments;
                psi.UseShellExecute = true;
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 關閉應用程式（依視窗標題）
        /// </summary>
        public bool CloseApp(string titleContains)
        {
            try
            {
                List<WindowInfo> windows = ListWindows();
                foreach (WindowInfo wi in windows)
                {
                    if (wi.Title.IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try
                        {
                            Process proc = Process.GetProcessById(wi.ProcessId);
                            proc.CloseMainWindow();
                            return true;
                        }
                        catch { }
                    }
                }
                LastError = "Process not found for: " + titleContains;
                return false;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        // ========== 執行命令 ==========

        /// <summary>
        /// 執行 CMD 命令並取得輸出
        /// </summary>
        public string RunCommand(string command, int timeoutMs = 30000)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c " + command;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.CreateNoWindow = true;
                psi.StandardOutputEncoding = Encoding.GetEncoding(950); // 繁體中文

                Process proc = Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit(timeoutMs);

                if (!string.IsNullOrEmpty(error) && string.IsNullOrEmpty(output))
                    return "ERROR: " + error;

                return output;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return "ERROR: " + ex.Message;
            }
        }

        // ========== 檔案操作 ==========

        /// <summary>
        /// 讀取檔案內容
        /// </summary>
        public string ReadFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LastError = "File not found: " + filePath;
                    return null;
                }
                return File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// 寫入檔案
        /// </summary>
        public bool WriteFile(string filePath, string content)
        {
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(filePath, content, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        // ========== 輔助方法 ==========

        private IntPtr FindWindowByTitle(string titleContains)
        {
            IntPtr found = IntPtr.Zero;

            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder title = new StringBuilder(256);
                GetWindowText(hWnd, title, 256);
                string windowTitle = title.ToString();

                if (!string.IsNullOrEmpty(windowTitle) &&
                    windowTitle.IndexOf(titleContains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = hWnd;
                    return false; // 停止列舉
                }
                return true;
            }, IntPtr.Zero);

            return found;
        }
    }

    /// <summary>
    /// 視窗資訊
    /// </summary>
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public int ProcessId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}] {1} ({2}x{3} at {4},{5})",
                ProcessId, Title, Width, Height, X, Y);
        }
    }
}
