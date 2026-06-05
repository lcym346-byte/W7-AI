using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using AIAgentTool.Models;


namespace AIAgentTool.Services.Core
{
    /// <summary>
    /// AI 推理引擎 - 意圖分析、答案合成、摘要、計算、知識快取
    /// </summary>
    public class AIReasoningEngine
    {
        // ═══════════════════════════════════════════
        // 知識快取
        // ═══════════════════════════════════════════
        private readonly Dictionary<string, KnowledgeItem> _cache;
        private readonly int _maxCacheSize;

        public AIReasoningEngine(int maxCacheSize = 100)
        {
            _maxCacheSize = maxCacheSize;
            _cache = new Dictionary<string, KnowledgeItem>(StringComparer.OrdinalIgnoreCase);
        }

        // ═══════════════════════════════════════════
        // 意圖分析
        // ═══════════════════════════════════════════
        public TaskType AnalyzeIntent(string input)
        {
            if (string.IsNullOrEmpty(input))
                return TaskType.Search;

            string lower = input.ToLower().Trim();

            // 計算
            if (Regex.IsMatch(lower, @"[\d\+\-\*\/\%\^]") &&
                (lower.Contains("計算") || lower.Contains("算") || lower.Contains("calc") ||
                 Regex.IsMatch(lower, @"^\s*[\d\(\+\-]")))
                return TaskType.Calculate;

            // 程式碼生成
            if (lower.Contains("寫程式") || lower.Contains("寫代碼") || lower.Contains("產生程式") ||
                lower.Contains("生成代碼") || lower.Contains("generate code") ||
                lower.Contains("write code") || lower.Contains("寫一個程式") ||
                lower.Contains("幫我寫") || lower.Contains("編寫"))
                return TaskType.GenerateCode;

            // 開啟程式
            if (lower.Contains("開啟") || lower.Contains("打開") || lower.Contains("啟動") ||
                lower.Contains("執行") || lower.Contains("open ") || lower.Contains("launch") ||
                lower.Contains("start ") || lower.Contains("run "))
                return TaskType.LaunchApp;

            // 關閉程式
            if (lower.Contains("關閉") || lower.Contains("結束") || lower.Contains("停止") ||
                lower.Contains("kill") || lower.Contains("close ") || lower.Contains("stop "))
                return TaskType.CloseApp;

            // 處理程序列表
            if (lower.Contains("程序列表") || lower.Contains("進程") || lower.Contains("process") ||
                lower.Contains("任務管理"))
                return TaskType.ListProcesses;

            // 檔案管理
            if (lower.Contains("檔案") || lower.Contains("文件") || lower.Contains("資料夾") ||
                lower.Contains("目錄") || lower.Contains("file") || lower.Contains("folder") ||
                lower.Contains("dir") || lower.Contains("瀏覽"))
                return TaskType.FileManagement;

            // CMD 命令
            if (lower.StartsWith("cmd ") || lower.StartsWith("cmd:") ||
                lower.Contains("命令") || lower.Contains("command"))
                return TaskType.RunCommand;

            // 系統資訊
            if (lower.Contains("系統") || lower.Contains("電腦") || lower.Contains("system") ||
                lower.Contains("computer") || lower.Contains("硬體"))
                return TaskType.SystemInfo;

            // 截圖
            if (lower.Contains("截圖") || lower.Contains("screenshot") || lower.Contains("擷取畫面"))
                return TaskType.ScreenCapture;

            // 剪貼簿
            if (lower.Contains("剪貼") || lower.Contains("clipboard") || lower.Contains("複製") ||
                lower.Contains("貼上"))
                return TaskType.ClipboardOp;

            // 已安裝程式
            if (lower.Contains("已安裝") || lower.Contains("installed") || lower.Contains("安裝了"))
                return TaskType.InstalledApps;

            // 比較
            if (lower.Contains(" vs ") || lower.Contains(" vs.") || lower.Contains("比較") ||
                lower.Contains("compare") || lower.Contains("versus") || lower.Contains("對比"))
                return TaskType.Compare;

            // 摘要
            if (lower.Contains("摘要") || lower.Contains("總結") || lower.Contains("summarize") ||
                lower.Contains("summary") || lower.Contains("歸納"))
                return TaskType.Summarize;

            // 研究
            if (lower.Contains("研究") || lower.Contains("分析") || lower.Contains("調查") ||
                lower.Contains("research") || lower.Contains("analyze") || lower.Contains("深入"))
                return TaskType.AutoResearch;

            // 批次
            if (lower.Contains("批次") || lower.Contains("batch") || lower.Contains("依序"))
                return TaskType.BatchOperation;

            // 預設：搜尋
            if (lower.Contains("搜尋") || lower.Contains("搜索") || lower.Contains("找") ||
                lower.Contains("search") || lower.Contains("what") || lower.Contains("who") ||
                lower.Contains("how") || lower.Contains("why") || lower.Contains("是什麼") ||
                lower.Contains("什麼是"))
                return TaskType.Search;

            // 無法判定時預設為自動研究
            return TaskType.AutoResearch;
        }

        // ═══════════════════════════════════════════
        // 關鍵字提取
        // ═══════════════════════════════════════════
        public List<string> ExtractKeywords(string text)
        {
            List<string> keywords = new List<string>();
            if (string.IsNullOrEmpty(text)) return keywords;

            // 停用詞
            HashSet<string> stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "的", "了", "是", "在", "我", "有", "和", "就", "不", "人", "都", "一", "個",
                "上", "也", "很", "到", "說", "要", "去", "你", "會", "著", "沒有", "看", "好",
                "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
                "have", "has", "had", "do", "does", "did", "will", "would", "could",
                "should", "may", "might", "shall", "can", "need", "dare", "ought",
                "to", "of", "in", "for", "on", "with", "at", "by", "from", "as",
                "into", "through", "during", "before", "after", "above", "below",
                "between", "out", "off", "over", "under", "again", "further", "then",
                "once", "here", "there", "when", "where", "why", "how", "all", "each",
                "every", "both", "few", "more", "most", "other", "some", "such", "no",
                "not", "only", "own", "same", "so", "than", "too", "very", "just",
                "because", "but", "and", "or", "if", "while", "about", "this", "that",
                "these", "those", "it", "its", "he", "she", "they", "them", "what", "which"
            };

            // 中文詞彙（2字以上連續中文）
            MatchCollection chineseMatches = Regex.Matches(text, @"[\u4e00-\u9fff]{2,}");
            foreach (Match m in chineseMatches)
            {
                if (!stopWords.Contains(m.Value) && !keywords.Contains(m.Value))
                    keywords.Add(m.Value);
            }

            // 英文詞彙
            MatchCollection englishMatches = Regex.Matches(text.ToLower(), @"[a-z]{3,}");
            foreach (Match m in englishMatches)
            {
                if (!stopWords.Contains(m.Value) && !keywords.Contains(m.Value))
                    keywords.Add(m.Value);
            }

            return keywords;
        }

        // ═══════════════════════════════════════════
        // 答案合成
        // ═══════════════════════════════════════════
        public string SynthesizeAnswer(string query, List<SearchResult> results)
        {
            if (results == null || results.Count == 0)
                return "未找到相關資訊。";

            // 依相關度排序
            results.Sort((a, b) => b.RelevanceScore.CompareTo(a.RelevanceScore));

            List<string> queryKeywords = ExtractKeywords(query);
            string bestAnswer = "";
            int bestScore = 0;

            foreach (SearchResult r in results)
            {
                string content = r.Snippet ?? "";
                int score = 0;
                foreach (string kw in queryKeywords)
                {
                    if (content.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                        score++;
                }
                score += (int)(r.RelevanceScore * 10);

                if (score > bestScore && content.Length > 20)
                {
                    bestScore = score;
                    bestAnswer = content;
                }
            }

            if (string.IsNullOrEmpty(bestAnswer))
                bestAnswer = results[0].Snippet ?? results[0].Title;

            // 組合結果
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("【最佳答案】");
            sb.AppendLine(bestAnswer);
            sb.AppendLine();

            int supplementCount = Math.Min(results.Count, 5);
            if (supplementCount > 1)
            {
                sb.AppendLine("【補充資料】");
                for (int i = 0; i < supplementCount; i++)
                {
                    SearchResult r = results[i];
                    sb.AppendLine(string.Format("{0}. {1}", i + 1, r.Title));
                    if (!string.IsNullOrEmpty(r.Snippet))
                        sb.AppendLine("   " + TruncateForDisplay(r.Snippet, 120));
                    if (!string.IsNullOrEmpty(r.Url))
                        sb.AppendLine("   來源: " + r.Url);
                    sb.AppendLine();
                }
            }

            // 關鍵字頻率
            Dictionary<string, int> keywordFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (SearchResult r in results)
            {
                List<string> kws = ExtractKeywords((r.Snippet ?? "") + " " + (r.Title ?? ""));
                foreach (string kw in kws)
                {
                    if (keywordFreq.ContainsKey(kw))
                        keywordFreq[kw]++;
                    else
                        keywordFreq[kw] = 1;
                }
            }

            // 排序取 Top 10
            List<KeyValuePair<string, int>> sortedKw = new List<KeyValuePair<string, int>>(keywordFreq);
            sortedKw.Sort((a, b) => b.Value.CompareTo(a.Value));
            if (sortedKw.Count > 0)
            {
                sb.AppendLine("【高頻關鍵字】");
                int topCount = Math.Min(sortedKw.Count, 10);
                for (int i = 0; i < topCount; i++)
                {
                    sb.Append(string.Format("{0}({1}) ", sortedKw[i].Key, sortedKw[i].Value));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ═══════════════════════════════════════════
        // 摘要（提取式）
        // ═══════════════════════════════════════════
        public string Summarize(string text, int maxSentences = 5)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // 切句
            string[] separators = new string[] { "。", "！", "？", ".", "!", "?", "\n" };
            string[] rawSentences = text.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            List<string> sentences = new List<string>();
            foreach (string s in rawSentences)
            {
                string trimmed = s.Trim();
                if (trimmed.Length >= 10)
                    sentences.Add(trimmed);
            }

            if (sentences.Count == 0) return TruncateForDisplay(text, 500);
            if (sentences.Count <= maxSentences)
                return string.Join("。", sentences.ToArray()) + "。";

            // 關鍵字頻率（去停用詞）
            List<string> allKeywords = ExtractKeywords(text);
            Dictionary<string, int> freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string kw in allKeywords)
            {
                if (freq.ContainsKey(kw))
                    freq[kw]++;
                else
                    freq[kw] = 1;
            }

            // 句子打分
            double[] scores = new double[sentences.Count];
            for (int i = 0; i < sentences.Count; i++)
            {
                double score = 0;
                string sentLower = sentences[i].ToLower();

                foreach (KeyValuePair<string, int> kv in freq)
                {
                    if (sentLower.Contains(kv.Key.ToLower()))
                        score += kv.Value;
                }

                // 位置加權
                if (i == 0) score *= 1.5;
                else if (i == 1) score *= 1.2;
                else if (i == sentences.Count - 1) score *= 1.1;

                // 長度加權（適中長度加分）
                if (sentences[i].Length >= 20 && sentences[i].Length <= 200)
                    score *= 1.2;

                scores[i] = score;
            }

            // 取最高分句子（保持原序）
            List<int> indices = new List<int>();
            for (int i = 0; i < sentences.Count; i++)
                indices.Add(i);
            indices.Sort((a, b) => scores[b].CompareTo(scores[a]));

            List<int> selected = new List<int>();
            for (int i = 0; i < Math.Min(maxSentences, indices.Count); i++)
                selected.Add(indices[i]);
            selected.Sort();

            StringBuilder sb = new StringBuilder();
            foreach (int idx in selected)
            {
                sb.Append(sentences[idx]);
                sb.Append("。");
            }

            return sb.ToString();
        }

        // ═══════════════════════════════════════════
        // 計算
        // ═══════════════════════════════════════════
        public string Calculate(string expression)
        {
            try
            {
                // 清理輸入
                string expr = expression;
                expr = Regex.Replace(expr, @"(計算|算|calc|calculate)\s*", "", RegexOptions.IgnoreCase);
                expr = expr.Trim();

                // 處理次方符號 ^ → Math.Pow
                if (expr.Contains("^"))
                {
                    expr = Regex.Replace(expr, @"(\d+\.?\d*)\s*\^\s*(\d+\.?\d*)",
                        new MatchEvaluator(delegate(Match m)
                        {
                            double baseNum = double.Parse(m.Groups[1].Value);
                            double power = double.Parse(m.Groups[2].Value);
                            return Math.Pow(baseNum, power).ToString();
                        }));
                }

                // 使用 DataTable.Compute 計算
                DataTable dt = new DataTable();
                object result = dt.Compute(expr, "");

                return string.Format("計算結果：{0} = {1}", expression.Trim(), result);
            }
            catch (Exception ex)
            {
                return string.Format("計算錯誤：{0}\n請確認表達式格式正確（支援 +, -, *, /, %, ^, ()）", ex.Message);
            }
        }

        // ═══════════════════════════════════════════
        // 比較分析
        // ═══════════════════════════════════════════
        public string CompareTopics(string topicA, string topicB,
            List<SearchResult> resultsA, List<SearchResult> resultsB)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("═══ 比較分析：{0} vs {1} ═══", topicA, topicB));
            sb.AppendLine();

            // 主題 A 摘要
            sb.AppendLine(string.Format("【{0}】", topicA));
            if (resultsA != null && resultsA.Count > 0)
            {
                sb.AppendLine(resultsA[0].Snippet ?? resultsA[0].Title);
            }
            else
            {
                sb.AppendLine("未找到相關資訊");
            }
            sb.AppendLine();

            // 主題 B 摘要
            sb.AppendLine(string.Format("【{0}】", topicB));
            if (resultsB != null && resultsB.Count > 0)
            {
                sb.AppendLine(resultsB[0].Snippet ?? resultsB[0].Title);
            }
            else
            {
                sb.AppendLine("未找到相關資訊");
            }
            sb.AppendLine();

            // 交集/差異分析
            List<string> kwA = ExtractKeywords(GetCombinedSnippets(resultsA));
            List<string> kwB = ExtractKeywords(GetCombinedSnippets(resultsB));

            List<string> common = new List<string>();
            List<string> onlyA = new List<string>();
            List<string> onlyB = new List<string>();

            HashSet<string> setB = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string k in kwB) setB.Add(k);

            HashSet<string> setA = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string k in kwA) setA.Add(k);

            foreach (string k in kwA)
            {
                if (setB.Contains(k))
                {
                    if (!common.Contains(k)) common.Add(k);
                }
                else
                {
                    if (!onlyA.Contains(k)) onlyA.Add(k);
                }
            }
            foreach (string k in kwB)
            {
                if (!setA.Contains(k) && !onlyB.Contains(k))
                    onlyB.Add(k);
            }

            sb.AppendLine("【共同特徵】");
            sb.AppendLine(common.Count > 0
                ? string.Join(", ", common.GetRange(0, Math.Min(common.Count, 10)).ToArray())
                : "無明顯共同點");
            sb.AppendLine();

            sb.AppendLine(string.Format("【{0} 獨有特徵】", topicA));
            sb.AppendLine(onlyA.Count > 0
                ? string.Join(", ", onlyA.GetRange(0, Math.Min(onlyA.Count, 10)).ToArray())
                : "無");
            sb.AppendLine();

            sb.AppendLine(string.Format("【{0} 獨有特徵】", topicB));
            sb.AppendLine(onlyB.Count > 0
                ? string.Join(", ", onlyB.GetRange(0, Math.Min(onlyB.Count, 10)).ToArray())
                : "無");

            return sb.ToString();
        }

        // ═══════════════════════════════════════════
        // 知識快取操作
        // ═══════════════════════════════════════════
        public void CacheKnowledge(string topic, string content, string source)
        {
            if (string.IsNullOrEmpty(topic) || string.IsNullOrEmpty(content))
                return;

            string key = topic.Trim().ToLower();

            if (_cache.Count >= _maxCacheSize && !_cache.ContainsKey(key))
            {
                // 移除使用次數最少的項目
                string leastUsedKey = null;
                int minUse = int.MaxValue;
                foreach (KeyValuePair<string, KnowledgeItem> kv in _cache)
                {
                    if (kv.Value.UseCount < minUse)
                    {
                        minUse = kv.Value.UseCount;
                        leastUsedKey = kv.Key;
                    }
                }
                if (leastUsedKey != null)
                    _cache.Remove(leastUsedKey);
            }

            _cache[key] = new KnowledgeItem
            {
                Topic = topic,
                Content = content,
                Source = source,
                CachedAt = DateTime.Now,
                UseCount = 0
            };
        }

        public KnowledgeItem GetCachedKnowledge(string topic)
        {
            if (string.IsNullOrEmpty(topic)) return null;

            string key = topic.Trim().ToLower();
            if (_cache.ContainsKey(key))
            {
                _cache[key].UseCount++;
                return _cache[key];
            }
            return null;
        }

        public List<KnowledgeItem> GetAllCachedItems()
        {
            return new List<KnowledgeItem>(_cache.Values);
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        // ═══════════════════════════════════════════
        // 輔助方法
        // ═══════════════════════════════════════════
        private string TruncateForDisplay(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + "...";
        }

        private string GetCombinedSnippets(List<SearchResult> results)
        {
            if (results == null || results.Count == 0) return "";
            StringBuilder sb = new StringBuilder();
            foreach (SearchResult r in results)
            {
                if (!string.IsNullOrEmpty(r.Snippet))
                    sb.Append(r.Snippet + " ");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 解析比較查詢中的兩個主題
        /// </summary>
        public string[] ParseCompareTopics(string query)
        {
            string[] separators = new string[] { " vs ", " VS ", " vs. ", " versus ", "比較", "對比", "和", "與", "跟" };
            foreach (string sep in separators)
            {
                int idx = query.IndexOf(sep, StringComparison.OrdinalIgnoreCase);
                if (idx > 0 && idx < query.Length - sep.Length)
                {
                    string a = query.Substring(0, idx).Trim();
                    string b = query.Substring(idx + sep.Length).Trim();

                    // 清除前綴指令詞
                    a = Regex.Replace(a, @"^(比較|對比|compare)\s*", "", RegexOptions.IgnoreCase).Trim();

                    if (a.Length > 0 && b.Length > 0)
                        return new string[] { a, b };
                }
            }
            return null;
        }
    }
}
