using System;

namespace AIAgentTool.Models
{
    /// <summary>
    /// 知識快取項目
    /// </summary>
    public class KnowledgeItem
    {
        public string Topic { get; set; }
        public string Content { get; set; }
        public string Source { get; set; }
        public DateTime CachedAt { get; set; }
        public int UseCount { get; set; }

        public KnowledgeItem()
        {
            Topic = "";
            Content = "";
            Source = "";
            CachedAt = DateTime.Now;
            UseCount = 0;
        }
    }
}
