using System;

namespace AIAgentTool.Models
{
    /// <summary>
    /// 搜尋結果資料模型
    /// </summary>
    public class SearchResult
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Snippet { get; set; }
        public string Source { get; set; }
        public DateTime RetrievedAt { get; set; }
        public double RelevanceScore { get; set; }

        public SearchResult()
        {
            Title = "";
            Url = "";
            Snippet = "";
            Source = "";
            RetrievedAt = DateTime.Now;
            RelevanceScore = 0;
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1}\n{2}\n{3}",
                Source, Title, Snippet, Url);
        }
    }
}
