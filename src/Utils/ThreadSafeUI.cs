using System;
using System.Windows.Forms;

namespace AIAgentTool.Utils
{
    /// <summary>
    /// 跨執行緒安全 UI 更新工具
    /// .NET 4.0 相容：用 delegate + Invoke 模式
    /// </summary>
    public static class ThreadSafeUI
    {
        /// <summary>
        /// 安全地在 UI 執行緒上執行動作
        /// </summary>
        public static void Run(Control control, Action action)
        {
            if (control == null || control.IsDisposed) return;

            if (control.InvokeRequired)
            {
                try
                {
                    control.Invoke(action);
                }
                catch (ObjectDisposedException)
                {
                    // 控件已被銷毀，忽略
                }
                catch (InvalidOperationException)
                {
                    // 控件不可用，忽略
                }
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// 安全地更新 Label 文字
        /// </summary>
        public static void SetText(Control control, string text)
        {
            Run(control, delegate { control.Text = text; });
        }

        /// <summary>
        /// 安全地更新 ProgressBar 值
        /// </summary>
        public static void SetProgress(ProgressBar bar, int value)
        {
            Run(bar, delegate
            {
                if (value < bar.Minimum) value = bar.Minimum;
                if (value > bar.Maximum) value = bar.Maximum;
                bar.Value = value;
            });
        }

        /// <summary>
        /// 安全地在 ListBox 加入項目
        /// </summary>
        public static void AddListItem(ListBox list, string item)
        {
            Run(list, delegate
            {
                list.Items.Add(item);
                list.TopIndex = list.Items.Count - 1;
            });
        }

        /// <summary>
        /// 安全地在 RichTextBox 附加文字
        /// </summary>
        public static void AppendText(RichTextBox box, string text)
        {
            Run(box, delegate
            {
                box.AppendText(text);
                box.ScrollToCaret();
            });
        }

        /// <summary>
        /// 安全地設定控件是否啟用
        /// </summary>
        public static void SetEnabled(Control control, bool enabled)
        {
            Run(control, delegate { control.Enabled = enabled; });
        }
    }
}
