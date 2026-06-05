using System;
using System.Collections.Generic;

namespace AIAgentTool.Models
{
    /// <summary>
    /// 任務狀態列舉
    /// </summary>
    public enum TaskStatus
    {
        Pending,
        Searching,
        Analyzing,
        Executing,
        Completed,
        Failed
    }

    /// <summary>
    /// 任務類型列舉
    /// </summary>
    public enum TaskType
    {
        // 網路資訊
        Search,
        AutoResearch,
        Summarize,
        Compare,

        // AI 互動
        AskAI,
        Translate,

        // 程式碼生成
        CodeGeneration,
        GenerateCode,

        // 電腦操作
        LaunchApp,
        CloseApp,
        ListProcesses,
        ManageWindows,

        // 檔案管理
        FileManagement,
        BrowseDirectory,
        SearchFile,

        // 系統
        RunCommand,
        SystemInfo,
        InstalledApps,
        ScreenCapture,
        ClipboardOp,

        // 工具
        Calculate,

        // 批次
        BatchOperation
    }

    /// <summary>
    /// 代理任務資料模型
    /// </summary>
    public class AgentTask
    {
        public string Id { get; set; }
        public string Query { get; set; }
        public TaskType Type { get; set; }
        public TaskStatus Status { get; set; }
        public string Result { get; set; }
        public List<SearchResult> SearchResults { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public List<string> Steps { get; set; }
        public int Progress { get; set; }

        public AgentTask()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            Query = "";
            Result = "";
            SearchResults = new List<SearchResult>();
            Steps = new List<string>();
            CreatedAt = DateTime.Now;
            Status = TaskStatus.Pending;
            Progress = 0;
        }

        public void AddStep(string step)
        {
            Steps.Add(string.Format("[{0}] {1}",
                DateTime.Now.ToString("HH:mm:ss"), step));
        }
    }
}
