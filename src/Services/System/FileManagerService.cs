using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace AIAgentTool.Services.System
{
    /// <summary>
    /// 檔案管理服務 — 目錄瀏覽、檔案搜尋
    /// </summary>
    public class FileManagerService
    {
        // 中文路徑別名
        private static readonly Dictionary<string, string> PathAliases = CreatePathAliases();

        private static Dictionary<string, string> CreatePathAliases()
        {
            Dictionary<string, string> d = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            d["桌面"] = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            d["desktop"] = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            d["文件"] = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            d["documents"] = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            d["下載"] = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            d["downloads"] = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            d["首頁"] = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            d["home"] = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            d["音樂"] = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            d["music"] = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            d["圖片"] = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            d["pictures"] = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            d["影片"] = Environment.GetFolderPath(
                Environment.SpecialFolder.MyVideos);
            d["videos"] = Environment.GetFolderPath(
                Environment.SpecialFolder.MyVideos);

            return d;
        }

        /// <summary>
        /// 瀏覽目錄
        /// </summary>
        public string BrowseDirectory(string path)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                path = ResolvePath(path);

                if (!Directory.Exists(path))
                {
                    sb.AppendLine(string.Format("✗ 目錄不存在: {0}", path));
                    sb.AppendLine("\n可用的路徑別名:");
                    foreach (string key in PathAliases.Keys)
                    {
                        sb.AppendLine(string.Format("  • {0} → {1}", key, PathAliases[key]));
                    }
                    return sb.ToString();
                }

                sb.AppendLine(string.Format("═══ 目錄: {0} ═══\n", path));

                // 子目錄
                string[] dirs = Directory.GetDirectories(path);
                if (dirs.Length > 0)
                {
                    sb.AppendLine(string.Format("【資料夾】 ({0} 個)", dirs.Length));
                    int dirCount = 0;
                    foreach (string dir in dirs)
                    {
                        if (dirCount >= 30) break;
                        try
                        {
                            DirectoryInfo dirInfo = new DirectoryInfo(dir);
                            sb.AppendLine(string.Format("  [DIR] {0,-35} {1}",
                                dirInfo.Name,
                                dirInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm")));
                        }
                        catch { }
                        dirCount++;
                    }
                    if (dirs.Length > 30)
                        sb.AppendLine(string.Format("  ... 還有 {0} 個資料夾",
                            dirs.Length - 30));
                    sb.AppendLine();
                }

                // 檔案
                string[] files = Directory.GetFiles(path);
                if (files.Length > 0)
                {
                    sb.AppendLine(string.Format("【檔案】 ({0} 個)", files.Length));
                    int fileCount = 0;
                    foreach (string file in files)
                    {
                        if (fileCount >= 50) break;
                        try
                        {
                            FileInfo fileInfo = new FileInfo(file);
                            sb.AppendLine(string.Format("  {0,-35} {1,10} {2}",
                                Truncate(fileInfo.Name, 33),
                                FormatFileSize(fileInfo.Length),
                                fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm")));
                        }
                        catch { }
                        fileCount++;
                    }
                    if (files.Length > 50)
                        sb.AppendLine(string.Format("  ... 還有 {0} 個檔案",
                            files.Length - 50));
                }

                // 統計
                long totalSize = 0;
                foreach (string f in files)
                {
                    try { totalSize += new FileInfo(f).Length; }
                    catch { }
                }

                sb.AppendLine(string.Format(
                    "\n═══ 共 {0} 資料夾, {1} 檔案, 總大小: {2} ═══",
                    dirs.Length, files.Length, FormatFileSize(totalSize)));
            }
            catch (UnauthorizedAccessException)
            {
                sb.AppendLine("✗ 存取被拒，需要管理員權限");
            }
            catch (Exception ex)
            {
                sb.AppendLine(string.Format("✗ 錯誤: {0}", ex.Message));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 搜尋檔案
        /// </summary>
                /// <summary>
        /// 搜尋檔案（無指定路徑，預設使用者目錄）
        /// </summary>
        public string SearchFiles(string keyword)
        {
            return SearchFiles(keyword, null);
        }


        public string SearchFiles(string keyword, string basePath)
        {
            StringBuilder sb = new StringBuilder();
            keyword = keyword.Trim();

            if (string.IsNullOrEmpty(basePath))
            {
                basePath = Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile);
            }
            else
            {
                basePath = ResolvePath(basePath);
            }

            sb.AppendLine(string.Format("搜尋 \"{0}\" 於 {1} ...\n", keyword, basePath));

            List<string> found = new List<string>();
            SearchRecursive(basePath, keyword, found, 3);

            if (found.Count == 0)
            {
                sb.AppendLine("未找到匹配的檔案。");
            }
            else
            {
                sb.AppendLine(string.Format("找到 {0} 個結果：\n", found.Count));
                int showCount = 0;
                foreach (string file in found)
                {
                    if (showCount >= 30) break;
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        sb.AppendLine(string.Format("  {0} ({1})",
                            file, FormatFileSize(fi.Length)));
                    }
                    catch
                    {
                        sb.AppendLine(string.Format("  {0}", file));
                    }
                    showCount++;
                }
                if (found.Count > 30)
                    sb.AppendLine(string.Format("\n  ... 還有 {0} 個結果",
                        found.Count - 30));
            }

            return sb.ToString();
        }
        /// <summary>
        /// OpenInExplorer — OpenFolder 的別名，供 TaskAutomationService 呼叫
        /// </summary>
        public string OpenInExplorer(string path)
        {
            return OpenFolder(path);
        }


        /// <summary>
        /// 開啟資料夾 (在檔案總管中)
        /// </summary>
        public string OpenFolder(string path)
        {
            try
            {
                path = ResolvePath(path);

                if (!Directory.Exists(path))
                    return string.Format("✗ 目錄不存在: {0}", path);

                Process.Start("explorer.exe", path);
                return string.Format("✓ 已在檔案總管開啟: {0}", path);
            }
            catch (Exception ex)
            {
                return string.Format("✗ 開啟失敗: {0}", ex.Message);
            }
        }

        // ============================================================
        // 輔助方法
        // ============================================================

        private string ResolvePath(string path)
        {
            path = path.Trim();

            // 中文別名
            if (PathAliases.ContainsKey(path))
                return PathAliases[path];

            // 環境變數展開
            path = Environment.ExpandEnvironmentVariables(path);

            return path;
        }

        private void SearchRecursive(string path, string keyword,
            List<string> results, int maxDepth)
        {
            if (maxDepth <= 0 || results.Count >= 100) return;

            try
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    if (results.Count >= 100) return;
                    string fileName = Path.GetFileName(file);
                    if (fileName.ToLower().Contains(keyword.ToLower()))
                    {
                        results.Add(file);
                    }
                }

                foreach (string dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        SearchRecursive(dir, keyword, results, maxDepth - 1);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1048576) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1073741824) return (bytes / 1048576.0).ToString("F1") + " MB";
            return (bytes / 1073741824.0).ToString("F2") + " GB";
        }

        private string Truncate(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLen) return text;
            return text.Substring(0, maxLen - 2) + "..";
        }
    }
}
