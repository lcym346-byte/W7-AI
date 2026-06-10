using System;
using System.Collections.Generic;
using System.IO;
using AIAgentTool.Services.Media;
using AIAgentTool.Services.Desktop;

namespace AIAgentTool.Services.Core
{
    /// <summary>
    /// 媒體任務執行器 - 處理圖片生成、語音、桌面操作等任務
    /// </summary>
    public class MediaTaskExecutor
    {
        private readonly LocalAiMediaService _media;
        private readonly DesktopAutomation _desktop;
        private readonly string _outputPath;

        public bool LocalAiAvailable { get { return _media.IsAvailable; } }
        public string LastError { get; private set; }

        public MediaTaskExecutor(string localAiUrl, string outputPath)
        {
            _media = new LocalAiMediaService(localAiUrl);
            _desktop = new DesktopAutomation();
            _outputPath = outputPath ?? Environment.GetFolderPath(
                Environment.SpecialFolder.Desktop);

            // 嘗試連線 LocalAI
            _media.TestConnection();
        }

        /// <summary>
        /// 執行媒體/桌面任務步驟
        /// </summary>
        /// <param name="stepType">步驟類型</param>
        /// <param name="parameters">參數字典</param>
        /// <returns>執行結果描述</returns>
        public string ExecuteStep(string stepType, Dictionary<string, string> parameters)
        {
            try
            {
                switch (stepType.ToLower())
                {
                    // ===== 媒體生成 =====
                    case "generate_image":
                        return ExecuteGenerateImage(parameters);

                    case "text_to_speech":
                    case "tts":
                        return ExecuteTextToSpeech(parameters);

                    case "speech_to_text":
                    case "stt":
                        return ExecuteSpeechToText(parameters);

                    // ===== 桌面操作 =====
                    case "screenshot":
                        return ExecuteScreenshot(parameters);

                    case "click":
                        return ExecuteClick(parameters);

                    case "type_text":
                    case "type":
                        return ExecuteTypeText(parameters);

                    case "shortcut":
                    case "hotkey":
                        return ExecuteShortcut(parameters);

                    case "focus_window":
                    case "switch_window":
                        return ExecuteFocusWindow(parameters);

                    case "list_windows":
                        return ExecuteListWindows();

                    case "launch_app":
                    case "open_app":
                        return ExecuteLaunchApp(parameters);

                    case "close_app":
                        return ExecuteCloseApp(parameters);

                    case "run_command":
                    case "cmd":
                        return ExecuteRunCommand(parameters);

                    case "read_file":
                        return ExecuteReadFile(parameters);

                    case "write_file":
                        return ExecuteWriteFile(parameters);

                    case "move_window":
                    case "resize_window":
                        return ExecuteMoveWindow(parameters);

                    default:
                        LastError = "Unknown step type: " + stepType;
                        return null;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return "Error: " + ex.Message;
            }
        }

        /// <summary>
        /// 檢查 LocalAI 連線狀態
        /// </summary>
        public string CheckStatus()
        {
            bool connected = _media.TestConnection();
            if (connected)
                return "LocalAI: Connected (" + _media.GetAvailableModels() + ")";
            else
                return "LocalAI: Not connected - " + _media.LastError +
                       "\nDesktop Automation: Ready";
        }

        // ===== 媒體生成實作 =====

        private string ExecuteGenerateImage(Dictionary<string, string> p)
        {
            if (!_media.IsAvailable)
                return "Error: LocalAI is not running. Start it with: docker run -ti -p 8080:8080 localai/localai:latest";

            string prompt = GetParam(p, "prompt", "desc", "text");
            string size = GetParam(p, "size") ?? "512x512";

            if (string.IsNullOrEmpty(prompt))
                return "Error: No prompt provided for image generation";

            string imagePath = Path.Combine(_outputPath, "generated_images");
            string result = _media.GenerateImage(prompt, imagePath, size);

            if (result != null)
                return "Image generated: " + result;
            else
                return "Image generation failed: " + _media.LastError;
        }

        private string ExecuteTextToSpeech(Dictionary<string, string> p)
        {
            if (!_media.IsAvailable)
                return "Error: LocalAI is not running";

            string text = GetParam(p, "text", "input", "content");
            string voice = GetParam(p, "voice") ?? "alloy";

            if (string.IsNullOrEmpty(text))
                return "Error: No text provided for TTS";

            string audioPath = Path.Combine(_outputPath, "generated_audio");
            string result = _media.TextToSpeech(text, audioPath, voice);

            if (result != null)
                return "Audio generated: " + result;
            else
                return "TTS failed: " + _media.LastError;
        }

        private string ExecuteSpeechToText(Dictionary<string, string> p)
        {
            if (!_media.IsAvailable)
                return "Error: LocalAI is not running";

            string filePath = GetParam(p, "file", "path", "audio");
            if (string.IsNullOrEmpty(filePath))
                return "Error: No audio file specified";

            string result = _media.SpeechToText(filePath);
            if (result != null)
                return "Transcription: " + result;
            else
                return "STT failed: " + _media.LastError;
        }

        // ===== 桌面操作實作 =====

        private string ExecuteScreenshot(Dictionary<string, string> p)
        {
            string window = GetParam(p, "window", "title");
            string screenshotPath = Path.Combine(_outputPath, "screenshots");

            string result;
            if (!string.IsNullOrEmpty(window))
                result = _desktop.CaptureWindow(screenshotPath, window);
            else
                result = _desktop.CaptureScreen(screenshotPath);

            if (result != null)
                return "Screenshot saved: " + result;
            else
                return "Screenshot failed: " + _desktop.LastError;
        }

        private string ExecuteClick(Dictionary<string, string> p)
        {
            string xStr = GetParam(p, "x");
            string yStr = GetParam(p, "y");
            string button = GetParam(p, "button") ?? "left";

            if (string.IsNullOrEmpty(xStr) || string.IsNullOrEmpty(yStr))
                return "Error: x and y coordinates required";

            int x = int.Parse(xStr);
            int y = int.Parse(yStr);

            bool ok;
            if (button == "right")
                ok = _desktop.RightClick(x, y);
            else if (button == "double")
                ok = _desktop.DoubleClick(x, y);
            else
                ok = _desktop.Click(x, y);

            return ok ? string.Format("Clicked at ({0}, {1})", x, y) : "Click failed";
        }

        private string ExecuteTypeText(Dictionary<string, string> p)
        {
            string text = GetParam(p, "text", "input", "content");
            if (string.IsNullOrEmpty(text))
                return "Error: No text to type";

            bool ok = _desktop.TypeText(text);
            return ok ? "Typed: " + text : "Type failed";
        }

        private string ExecuteShortcut(Dictionary<string, string> p)
        {
            string keys = GetParam(p, "keys", "shortcut", "hotkey");
            if (string.IsNullOrEmpty(keys))
                return "Error: No shortcut keys specified";

            bool ok = _desktop.SendShortcut(keys);
            return ok ? "Shortcut sent: " + keys : "Shortcut failed";
        }

        private string ExecuteFocusWindow(Dictionary<string, string> p)
        {
            string title = GetParam(p, "title", "window", "name");
            if (string.IsNullOrEmpty(title))
                return "Error: No window title specified";

            bool ok = _desktop.FocusWindow(title);
            return ok ? "Focused: " + title : "Focus failed: " + _desktop.LastError;
        }

        private string ExecuteListWindows()
        {
            List<WindowInfo> windows = _desktop.ListWindows();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Open windows:");
            foreach (WindowInfo wi in windows)
            {
                sb.AppendLine("  " + wi.ToString());
            }
            return sb.ToString();
        }

        private string ExecuteLaunchApp(Dictionary<string, string> p)
        {
            string path = GetParam(p, "path", "app", "target");
            string args = GetParam(p, "arguments", "args") ?? "";

            if (string.IsNullOrEmpty(path))
                return "Error: No app path specified";

            bool ok = _desktop.LaunchApp(path, args);
            return ok ? "Launched: " + path : "Launch failed: " + _desktop.LastError;
        }

        private string ExecuteCloseApp(Dictionary<string, string> p)
        {
            string title = GetParam(p, "title", "window", "name", "app");
            if (string.IsNullOrEmpty(title))
                return "Error: No app/window specified";

            bool ok = _desktop.CloseApp(title);
            return ok ? "Closed: " + title : "Close failed: " + _desktop.LastError;
        }

        private string ExecuteRunCommand(Dictionary<string, string> p)
        {
            string command = GetParam(p, "command", "cmd");
            if (string.IsNullOrEmpty(command))
                return "Error: No command specified";

            return _desktop.RunCommand(command);
        }

        private string ExecuteReadFile(Dictionary<string, string> p)
        {
            string path = GetParam(p, "path", "file");
            if (string.IsNullOrEmpty(path))
                return "Error: No file path specified";

            string content = _desktop.ReadFile(path);
            return content ?? ("Read failed: " + _desktop.LastError);
        }

        private string ExecuteWriteFile(Dictionary<string, string> p)
        {
            string path = GetParam(p, "path", "file");
            string content = GetParam(p, "content", "text", "data");

            if (string.IsNullOrEmpty(path) || content == null)
                return "Error: path and content required";

            bool ok = _desktop.WriteFile(path, content);
            return ok ? "Written: " + path : "Write failed: " + _desktop.LastError;
        }

        private string ExecuteMoveWindow(Dictionary<string, string> p)
        {
            string title = GetParam(p, "title", "window");
            string xStr = GetParam(p, "x") ?? "0";
            string yStr = GetParam(p, "y") ?? "0";
            string wStr = GetParam(p, "width", "w") ?? "800";
            string hStr = GetParam(p, "height", "h") ?? "600";

            if (string.IsNullOrEmpty(title))
                return "Error: No window title specified";

            int x = int.Parse(xStr);
            int y = int.Parse(yStr);
            int w = int.Parse(wStr);
            int h = int.Parse(hStr);

            bool ok = _desktop.MoveResizeWindow(title, x, y, w, h);
            return ok ? string.Format("Moved {0} to ({1},{2}) size {3}x{4}", title, x, y, w, h)
                      : "Move failed: " + _desktop.LastError;
        }

        // ===== 輔助 =====

        private string GetParam(Dictionary<string, string> p, params string[] keys)
        {
            if (p == null) return null;
            foreach (string key in keys)
            {
                if (p.ContainsKey(key) && !string.IsNullOrEmpty(p[key]))
                    return p[key];
            }
            return null;
        }
    }
}
