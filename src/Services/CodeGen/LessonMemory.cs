using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AIAgentTool.Services.CodeGen
{
    /// <summary>
    /// 經驗記憶系統 - 記錄編譯錯誤和修復方式
    /// </summary>
    public class LessonMemory
    {
        private string _lessonsFile;
        private List<Lesson> _lessons;
        private const int MAX_LESSONS = 50;

        public int LessonCount { get { return _lessons.Count; } }

        public LessonMemory()
        {
            _lessonsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lessons.json");
            _lessons = new List<Lesson>();
            LoadLessons();
        }

        /// <summary>
        /// 記錄一次修復經驗
        /// </summary>
        public void RecordLesson(string errorCode, string errorMessage, string wrongSnippet, string fixSnippet)
        {
            // 檢查是否已有相同錯誤碼的記錄
            Lesson existing = null;
            foreach (Lesson l in _lessons)
            {
                if (l.ErrorCode == errorCode)
                {
                    existing = l;
                    break;
                }
            }

            if (existing != null)
            {
                existing.OccurrenceCount++;
                existing.LastOccurred = DateTime.Now;
                if (!string.IsNullOrEmpty(errorMessage) && !existing.ErrorMessages.Contains(errorMessage))
                {
                    existing.ErrorMessages.Add(errorMessage);
                    if (existing.ErrorMessages.Count > 5)
                        existing.ErrorMessages.RemoveAt(0);
                }
            }
            else
            {
                Lesson lesson = new Lesson();
                lesson.ErrorCode = errorCode;
                lesson.ErrorMessages = new List<string>();
                lesson.ErrorMessages.Add(errorMessage);
                lesson.WrongSnippet = TruncateCode(wrongSnippet, 500);
                lesson.FixSnippet = TruncateCode(fixSnippet, 500);
                lesson.OccurrenceCount = 1;
                lesson.FirstOccurred = DateTime.Now;
                lesson.LastOccurred = DateTime.Now;
                _lessons.Add(lesson);

                // 限制總數
                if (_lessons.Count > MAX_LESSONS)
                    _lessons.RemoveAt(0);
            }

            SaveLessons();
        }

        /// <summary>
        /// 取得與目前錯誤相關的歷史修復經驗
        /// </summary>
        public string GetRelevantLessons(string errors)
        {
            if (_lessons.Count == 0 || string.IsNullOrEmpty(errors))
                return "";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("【歷史修復經驗 - 請參考避免重複錯誤】");

            int found = 0;
            foreach (Lesson l in _lessons)
            {
                if (errors.Contains(l.ErrorCode) || ContainsAny(errors, l.ErrorMessages))
                {
                    sb.AppendLine(string.Format("- 錯誤 {0}（發生 {1} 次）：", l.ErrorCode, l.OccurrenceCount));
                    if (l.ErrorMessages.Count > 0)
                        sb.AppendLine("  訊息：" + l.ErrorMessages[0]);
                    if (!string.IsNullOrEmpty(l.FixSnippet))
                        sb.AppendLine("  修復方式：參考正確程式碼模式");
                    found++;
                    if (found >= 3) break;
                }
            }

            return found > 0 ? sb.ToString() : "";
        }

        /// <summary>
        /// 取得預防提示（在生成新程式碼時使用）
        /// </summary>
        public string GetPreventionHints()
        {
            if (_lessons.Count == 0) return "";

            // 只取最常出現的前 5 個錯誤
            List<Lesson> sorted = new List<Lesson>(_lessons);
            sorted.Sort(delegate(Lesson a, Lesson b)
            {
                return b.OccurrenceCount.CompareTo(a.OccurrenceCount);
            });

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("【注意：以下是過去常犯的錯誤，請務必避免】");

            int count = 0;
            foreach (Lesson l in sorted)
            {
                if (count >= 5) break;
                sb.AppendLine(string.Format("- 避免 {0}：{1}",
                    l.ErrorCode,
                    l.ErrorMessages.Count > 0 ? l.ErrorMessages[0] : "未知錯誤"));
                count++;
            }

            return sb.ToString();
        }

        private bool ContainsAny(string text, List<string> items)
        {
            foreach (string item in items)
            {
                if (!string.IsNullOrEmpty(item) && text.Contains(item))
                    return true;
            }
            return false;
        }

        private string TruncateCode(string code, int maxLen)
        {
            if (string.IsNullOrEmpty(code)) return "";
            if (code.Length <= maxLen) return code;
            return code.Substring(0, maxLen) + "\n// ... (已截斷)";
        }

        private void SaveLessons()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[");
                for (int i = 0; i < _lessons.Count; i++)
                {
                    Lesson l = _lessons[i];
                    sb.Append("  {");
                    sb.Append("\"errorCode\": \"" + EscJson(l.ErrorCode) + "\", ");
                    sb.Append("\"errorMessages\": [" + JoinStrings(l.ErrorMessages) + "], ");
                    sb.Append("\"wrongSnippet\": \"" + EscJson(l.WrongSnippet) + "\", ");
                    sb.Append("\"fixSnippet\": \"" + EscJson(l.FixSnippet) + "\", ");
                    sb.Append("\"occurrenceCount\": " + l.OccurrenceCount + ", ");
                    sb.Append("\"firstOccurred\": \"" + l.FirstOccurred.ToString("o") + "\", ");
                    sb.Append("\"lastOccurred\": \"" + l.LastOccurred.ToString("o") + "\"");
                    sb.Append("}");
                    if (i < _lessons.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("]");
                File.WriteAllText(_lessonsFile, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void LoadLessons()
        {
            _lessons.Clear();
            if (!File.Exists(_lessonsFile)) return;

            try
            {
                string json = File.ReadAllText(_lessonsFile, Encoding.UTF8);
                MatchCollection matches = Regex.Matches(json, @"\{[^{}]*\}");
                foreach (Match m in matches)
                {
                    string block = m.Value;
                    Lesson l = new Lesson();
                    l.ErrorCode = ExtractVal(block, "errorCode");
                    l.ErrorMessages = ExtractStringArray(block, "errorMessages");
                    l.WrongSnippet = ExtractVal(block, "wrongSnippet");
                    l.FixSnippet = ExtractVal(block, "fixSnippet");
                    string countStr = ExtractVal(block, "occurrenceCount");
                    int cnt;
                    if (int.TryParse(countStr, out cnt)) l.OccurrenceCount = cnt;
                    else l.OccurrenceCount = 1;
                    string fo = ExtractVal(block, "firstOccurred");
                    DateTime dt;
                    if (DateTime.TryParse(fo, out dt)) l.FirstOccurred = dt;
                    string lo = ExtractVal(block, "lastOccurred");
                    if (DateTime.TryParse(lo, out dt)) l.LastOccurred = dt;

                    if (!string.IsNullOrEmpty(l.ErrorCode))
                        _lessons.Add(l);
                }
            }
            catch { }
        }

        private string ExtractVal(string json, string key)
        {
            string pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"";
            Match m = Regex.Match(json, pattern);
            if (m.Success)
                return m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\\\", "\\");
            // try numeric
            pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*([^,}\\]\\s]+)";
            m = Regex.Match(json, pattern);
            return m.Success ? m.Groups[1].Value.Trim() : "";
        }

        private List<string> ExtractStringArray(string json, string key)
        {
            List<string> items = new List<string>();
            string pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\\[(.*?)\\]";
            Match m = Regex.Match(json, pattern, RegexOptions.Singleline);
            if (!m.Success) return items;
            MatchCollection strings = Regex.Matches(m.Groups[1].Value, "\"((?:[^\"\\\\]|\\\\.)*)\"");
            foreach (Match sm in strings)
                items.Add(sm.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\"));
            return items;
        }

        private string JoinStrings(List<string> items)
        {
            if (items == null || items.Count == 0) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                sb.Append("\"" + EscJson(items[i]) + "\"");
                if (i < items.Count - 1) sb.Append(", ");
            }
            return sb.ToString();
        }

        private string EscJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }

    public class Lesson
    {
        public string ErrorCode { get; set; }
        public List<string> ErrorMessages { get; set; }
        public string WrongSnippet { get; set; }
        public string FixSnippet { get; set; }
        public int OccurrenceCount { get; set; }
        public DateTime FirstOccurred { get; set; }
        public DateTime LastOccurred { get; set; }

        public Lesson()
        {
            ErrorMessages = new List<string>();
        }
    }
}
