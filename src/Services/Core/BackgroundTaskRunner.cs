using System;
using System.Collections.Generic;
using System.Threading;
using AIAgentTool.Models;

namespace AIAgentTool.Services.Core
{
    /// <summary>
    /// 背景任務執行器 - 支援任務佇列、背景執行、回呼通知
    /// 使用 Thread + 佇列實現（.NET 4.0 相容，不使用 async/await）
    /// </summary>
    public class BackgroundTaskRunner
    {
        // ═══════════════════════════════════════════
        // 佇列與執行狀態
        // ═══════════════════════════════════════════
        private readonly Queue<TaskRequest> _taskQueue;
        private readonly object _queueLock = new object();
        private bool _isRunning;
        private Thread _workerThread;
        private readonly TaskAutomationService _automationService;

        // ═══════════════════════════════════════════
        // 事件
        // ═══════════════════════════════════════════
        public event Action<AgentTask> OnTaskCompleted;
        public event Action<string> OnStepUpdate;
        public event Action<int> OnProgressUpdate;
        public event Action<string> OnError;

        // ═══════════════════════════════════════════
        // 任務請求結構
        // ═══════════════════════════════════════════
        public class TaskRequest
        {
            public string Query { get; set; }
            public TaskType? ForceType { get; set; }
            public DateTime QueuedAt { get; set; }

            public TaskRequest(string query, TaskType? forceType = null)
            {
                Query = query;
                ForceType = forceType;
                QueuedAt = DateTime.Now;
            }
        }

        // ═══════════════════════════════════════════
        // 建構子
        // ═══════════════════════════════════════════
        public BackgroundTaskRunner(TaskAutomationService automationService)
        {
            _automationService = automationService;
            _taskQueue = new Queue<TaskRequest>();
            _isRunning = false;

            // 串接事件
            _automationService.OnStepUpdate += delegate(string step)
            {
                if (OnStepUpdate != null) OnStepUpdate(step);
            };
            _automationService.OnProgressUpdate += delegate(int progress)
            {
                if (OnProgressUpdate != null) OnProgressUpdate(progress);
            };
        }

        // ═══════════════════════════════════════════
        // 啟動/停止背景工作執行緒
        // ═══════════════════════════════════════════
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _workerThread = new Thread(WorkerLoop);
            _workerThread.IsBackground = true;
            _workerThread.Name = "AIAgent_BackgroundWorker";
            _workerThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            lock (_queueLock)
            {
                Monitor.PulseAll(_queueLock);
            }
            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(3000);
            }
        }

        // ═══════════════════════════════════════════
        // 提交任務
        // ═══════════════════════════════════════════
        public void EnqueueTask(string query, TaskType? forceType = null)
        {
            TaskRequest request = new TaskRequest(query, forceType);
            lock (_queueLock)
            {
                _taskQueue.Enqueue(request);
                Monitor.Pulse(_queueLock);
            }
        }

        // ═══════════════════════════════════════════
        // 同步執行（直接在呼叫端執行緒）
        // ═══════════════════════════════════════════
        public AgentTask ExecuteSync(string query, TaskType? forceType = null)
        {
            return _automationService.ExecuteTask(query, forceType);
        }

        // ═══════════════════════════════════════════
        // 背景執行（透過新執行緒，帶回呼）
        // ═══════════════════════════════════════════
        public void ExecuteAsync(string query, TaskType? forceType = null, Action<AgentTask> callback = null)
        {
            Thread t = new Thread(delegate()
            {
                try
                {
                    AgentTask result = _automationService.ExecuteTask(query, forceType);
                    if (callback != null) callback(result);
                    if (OnTaskCompleted != null) OnTaskCompleted(result);
                }
                catch (Exception ex)
                {
                    if (OnError != null) OnError(ex.Message);
                }
            });
            t.IsBackground = true;
            t.Name = "AIAgent_AsyncTask";
            t.Start();
        }

        // ═══════════════════════════════════════════
        // 佇列狀態
        // ═══════════════════════════════════════════
        public int QueueCount
        {
            get
            {
                lock (_queueLock)
                {
                    return _taskQueue.Count;
                }
            }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public void ClearQueue()
        {
            lock (_queueLock)
            {
                _taskQueue.Clear();
            }
        }

        // ═══════════════════════════════════════════
        // 背景工作迴圈
        // ═══════════════════════════════════════════
        private void WorkerLoop()
        {
            while (_isRunning)
            {
                TaskRequest request = null;

                lock (_queueLock)
                {
                    while (_taskQueue.Count == 0 && _isRunning)
                    {
                        Monitor.Wait(_queueLock, 1000);
                    }

                    if (!_isRunning) break;

                    if (_taskQueue.Count > 0)
                        request = _taskQueue.Dequeue();
                }

                if (request != null)
                {
                    try
                    {
                        AgentTask result = _automationService.ExecuteTask(request.Query, request.ForceType);
                        if (OnTaskCompleted != null)
                            OnTaskCompleted(result);
                    }
                    catch (Exception ex)
                    {
                        if (OnError != null)
                            OnError(string.Format("佇列任務失敗：{0} - {1}", request.Query, ex.Message));
                    }
                }
            }
        }
    }
}
