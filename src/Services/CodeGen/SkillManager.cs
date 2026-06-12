using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AIAgentTool.Services.CodeGen
{
    /// <summary>
    /// 技能管理系統 - 仿照 Hermes Agent 的 Skills System
    /// AI 完成任務後可以將經驗存為技能，下次直接重用
    /// </summary>
    public class SkillManager
    {
        private string _skillsDir;
        private List<Skill> _skills;

        public SkillManager()
        {
            _skillsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "skills");
            if (!Directory.Exists(_skillsDir))
                Directory.CreateDirectory(_skillsDir);
            _skills = new List<Skill>();
            LoadAllSkills();
        }

        /// <summary>
        /// 從成功的程式碼生成中建立新技能
        /// </summary>
        public void CreateSkillFromSuccess(string userRequest, string finalCode, 
            List<string> errorsFixed, int fixAttempts)
        {
            // 分析程式碼特徵，決定技能名稱和分類
            string skillName = GenerateSkillName(userRequest);
            string category = DetectCategory(userRequest, finalCode);

            // 檢查是否已有類似技能
            Skill existing = FindSkillByName(skillName);
            if (existing != null)
            {
                // 更新現有技能（增加使用次數和改進）
                existing.TimesUsed++;
                existing.LastUsed = DateTime.Now;
                if (errorsFixed.Count > 0)
                {
                    foreach (string err in errorsFixed)
                    {
                        if (!existing.KnownPitfalls.Contains(err))
                            existing.KnownPitfalls.Add(err);
                    }
                }
                SaveSkill(existing);
                return;
            }

            // 建立新技能
            Skill skill = new Skill();
            skill.Name = skillName;
            skill.Category = category;
            skill.Description = ExtractDescription(userRequest);
            skill.CreatedAt = DateTime.Now;
            skill.LastUsed = DateTime.Now;
            skill.TimesUsed = 1;
            skill.TemplateCode = finalCode;
            skill.Keywords = ExtractKeywords(userRequest);
            skill.KnownPitfalls = new List<string>();
            skill.FixPatterns = new List<string>();

            // 記錄修復過程中學到的陷阱
            foreach (string err in errorsFixed)
                skill.KnownPitfalls.Add(err);

            if (fixAttempts > 0)
                skill.FixPatterns.Add(string.Format("此程式經過 {0} 次修復才成功", fixAttempts));

            // 分析程式碼模式
            skill.CodePatterns = AnalyzeCodePatterns(finalCode);

            _skills.Add(skill);
            SaveSkill(skill);
        }

        /// <summary>
        /// 根據使用者需求找到最相關的技能
        /// </summary>
        public Skill FindBestSkill(string userRequest)
        {
            if (_skills.Count == 0) return null;

            string lower = userRequest.ToLower();
            Skill bestMatch = null;
            int bestScore = 0;

            foreach (var skill in _skills)
            {
                int score = 0;

                // 關鍵字匹配
                foreach (string kw in skill.Keywords)
                {
                    if (lower.Contains(kw.ToLower()))
                        score += 10;
                }

                // 分類匹配
                if (!string.IsNullOrEmpty(skill.Category) && lower.Contains(skill.Category.ToLower()))
                    score += 5;

                // 使用頻率加分
                score += skill.TimesUsed;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = skill;
                }
            }

            // 至少要有 10 分才算匹配
            return bestScore >= 10 ? bestMatch : null;
        }

        /// <summary>
        /// 生成技能提示詞，加入 AI 的 prompt 中
        /// </summary>
        public string GenerateSkillPrompt(string userRequest)
        {
            Skill skill = FindBestSkill(userRequest);
            if (skill == null) return "";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("【找到相關技能，請參考以下經驗】");
            sb.AppendLine(string.Format("技能：{0}（已成功使用 {1} 次）", skill.Name, skill.TimesUsed));
            sb.AppendLine(string.Format("描述：{0}", skill.Description));

            if (skill.KnownPitfalls.Count > 0)
            {
                sb.AppendLine("已知陷阱（必須避免）：");
                foreach (string pitfall in skill.KnownPitfalls)
                    sb.AppendLine("  - " + pitfall);
            }

            if (skill.CodePatterns.Count > 0)
            {
                sb.AppendLine("正確的程式碼模式：");
                foreach (string pattern in skill.CodePatterns)
                    sb.AppendLine("  - " + pattern);
            }

            // 提供模板程式碼作為參考
            if (!string.IsNullOrEmpty(skill.TemplateCode))
            {
                sb.AppendLine("參考模板（請根據新需求修改，不要完全照抄）：");
                // 只取前 2000 字元避免 prompt 太長
                string template = skill.TemplateCode;
                if (template.Length > 2000)
                    template = template.Substring(0, 2000) + "\n// ... (已截斷)";
                sb.AppendLine("```csharp");
                sb.AppendLine(template);
                sb.AppendLine("```");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 取得所有技能的摘要（用於狀態顯示）
        /// </summary>
        public string GetSkillsSummary()
        {
            if (_skills.Count == 0) return "尚無技能";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("已學會 {0} 項技能：", _skills.Count));
            foreach (var skill in _skills)
            {
                sb.AppendLine(string.Format("  - {0}（{1}）使用 {2} 次", 
                    skill.Name, skill.Category, skill.TimesUsed));
            }
            return sb.ToString();
        }

        // ====== 私有方法 ======

        private string GenerateSkillName(string request)
        {
            // 從需求中提取技能名稱
            string clean = request.Replace("寫", "").Replace("一個", "").Replace("程式", "").Trim();
            if (clean.Length > 20)
                clean = clean.Substring(0, 20);
            // 移除特殊字元
            clean = Regex.Replace(clean, @"[^\w\u4e00-\u9fff]", "_");
            if (string.IsNullOrEmpty(clean)) clean = "unnamed_skill";
            return clean;
        }

        private string DetectCategory(string request, string code)
        {
            string lower = request.ToLower();
            if (lower.Contains("計時") || lower.Contains("倒數") || lower.Contains("timer"))
                return "計時器";
            if (lower.Contains("計算") || lower.Contains("calculator"))
                return "計算機";
            if (lower.Contains("記事") || lower.Contains("筆記") || lower.Contains("note"))
                return "文字編輯";
            if (lower.Contains("圖片") || lower.Contains("畫") || lower.Contains("image"))
                return "圖形處理";
            if (lower.Contains("檔案") || lower.Contains("file"))
                return "檔案管理";
            if (lower.Contains("遊戲") || lower.Contains("game"))
                return "遊戲";
            return "一般工具";
        }

        private string ExtractDescription(string request)
        {
            if (request.Length > 100)
                return request.Substring(0, 100) + "...";
            return request;
        }

        private List<string> ExtractKeywords(string request)
        {
            List<string> keywords = new List<string>();
            string[] candidates = new string[] {
                "計時器", "倒數", "鬧鐘", "數字鍵盤", "按鈕", "輸入",
                "計算機", "計算", "加減乘除",
                "記事本", "文字", "編輯", "儲存",
                "圖片", "繪圖", "畫板",
                "檔案", "開啟", "關閉",
                "遊戲", "動畫", "音效",
                "表格", "資料", "列表",
                "時鐘", "日曆", "提醒",
                "響鈴", "變色", "重置"
            };

            string lower = request.ToLower();
            foreach (string kw in candidates)
            {
                if (lower.Contains(kw))
                    keywords.Add(kw);
            }
            return keywords;
        }

        private List<string> AnalyzeCodePatterns(string code)
        {
            List<string> patterns = new List<string>();

            if (code.Contains("System.Windows.Forms.Timer"))
                patterns.Add("使用 WinForms Timer 做定時任務");
            if (code.Contains("numberButtons") || Regex.IsMatch(code, @"btn\d"))
                patterns.Add("包含數字鍵盤按鈕陣列");
            if (code.Contains("_lastFocused") || code.Contains(".Focused"))
                patterns.Add("用 Focused 或 _lastFocused 追蹤焦點 TextBox");
            if (code.Contains("SystemSounds.Beep"))
                patterns.Add("使用 SystemSounds.Beep 播放提示音");
            if (code.Contains("BackColor") && code.Contains("Color.Red"))
                patterns.Add("使用 BackColor 變色提示");
            if (code.Contains("delegate(object"))
                patterns.Add("使用 delegate 語法（非 lambda）");
            if (code.Contains("string.Format"))
                patterns.Add("使用 string.Format（非 $interpolation）");
            if (code.Contains("TryParse"))
                patterns.Add("使用 TryParse 安全轉換數字");

            return patterns;
        }

        private Skill FindSkillByName(string name)
        {
            foreach (var skill in _skills)
            {
                if (skill.Name == name)
                    return skill;
            }
            return null;
        }

        private void SaveSkill(Skill skill)
        {
            try
            {
                string catDir = Path.Combine(_skillsDir, skill.Category);
                if (!Directory.Exists(catDir))
                    Directory.CreateDirectory(catDir);

                string filePath = Path.Combine(catDir, skill.Name + ".json");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"name\": \"" + EscJson(skill.Name) + "\",");
                sb.AppendLine("  \"category\": \"" + EscJson(skill.Category) + "\",");
                sb.AppendLine("  \"description\": \"" + EscJson(skill.Description) + "\",");
                sb.AppendLine("  \"createdAt\": \"" + skill.CreatedAt.ToString("o") + "\",");
                sb.AppendLine("  \"lastUsed\": \"" + skill.LastUsed.ToString("o") + "\",");
                sb.AppendLine("  \"timesUsed\": " + skill.TimesUsed + ",");
                sb.AppendLine("  \"keywords\": [" + JoinStringArray(skill.Keywords) + "],");
                sb.AppendLine("  \"knownPitfalls\": [" + JoinStringArray(skill.KnownPitfalls) + "],");
                sb.AppendLine("  \"fixPatterns\": [" + JoinStringArray(skill.FixPatterns) + "],");
                sb.AppendLine("  \"codePatterns\": [" + JoinStringArray(skill.CodePatterns) + "],");
                sb.AppendLine("  \"templateCode\": \"" + EscJson(skill.TemplateCode) + "\"");
                sb.AppendLine("}");
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        private void LoadAllSkills()
        {
            _skills.Clear();
            if (!Directory.Exists(_skillsDir)) return;

            string[] dirs = Directory.GetDirectories(_skillsDir);
            foreach (string dir in dirs)
            {
                string[] files = Directory.GetFiles(dir, "*.json");
                foreach (string file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file, Encoding.UTF8);
                        Skill skill = ParseSkill(json);
                        if (skill != null)
                            _skills.Add(skill);
                    }
                    catch { }
                }
            }
        }

        private Skill ParseSkill(string json)
        {
            Skill s = new Skill();
            s.Name = ExtractVal(json, "name");
            s.Category = ExtractVal(json, "category");
            s.Description = ExtractVal(json, "description");
            string created = ExtractVal(json, "createdAt");
            DateTime dt;
            if (DateTime.TryParse(created, out dt)) s.CreatedAt = dt;
            string lastUsed = ExtractVal(json, "lastUsed");
            if (DateTime.TryParse(lastUsed, out dt)) s.LastUsed = dt;
            string tu = ExtractVal(json, "timesUsed");
            int tui;
            if (int.TryParse(tu, out tui)) s.TimesUsed = tui;
            s.Keywords = ExtractArray(json, "keywords");
            s.KnownPitfalls = ExtractArray(json, "knownPitfalls");
            s.FixPatterns = ExtractArray(json, "fixPatterns");
            s.CodePatterns = ExtractArray(json, "codePatterns");
            s.TemplateCode = ExtractVal(json, "templateCode")
                .Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t")
                .Replace("\\\"", "\"").Replace("\\\\", "\\");

            if (string.IsNullOrEmpty(s.Name)) return null;
            return s;
        }

        private string ExtractVal(string json, string key)
        {
            string pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"";
            Match m = Regex.Match(json, pattern);
            if (m.Success)
                return m.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
            pattern = "\"" + Regex.Escape(key) + "\"\\s*:\\s*([^,}\\]\\s]+)";
            m = Regex.Match(json, pattern);
            return m.Success ? m.Groups[1].Value.Trim() : "";
        }

        private List<string> ExtractArray(string json, string key)
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

        private string JoinStringArray(List<string> items)
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

    public class Skill
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsed { get; set; }
        public int TimesUsed { get; set; }
        public string TemplateCode { get; set; }
        public List<string> Keywords { get; set; }
        public List<string> KnownPitfalls { get; set; }
        public List<string> FixPatterns { get; set; }
        public List<string> CodePatterns { get; set; }

        public Skill()
        {
            Keywords = new List<string>();
            KnownPitfalls = new List<string>();
            FixPatterns = new List<string>();
            CodePatterns = new List<string>();
        }
    }
}
