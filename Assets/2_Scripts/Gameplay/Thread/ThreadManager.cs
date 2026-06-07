using System.Threading;
using UnityEngine;
using System;

namespace YGZFrameWork
{
    /// <summary>
    /// 线程管理器 — 线程安全单例
    /// 提供基于 CancellationToken 的协作式线程取消机制，替代已废弃的 Thread.Abort()。
    /// </summary>
    public class ThreadManager : Singleton<ThreadManager>
    {
        /// <summary>
        /// 启动一个新线程，支持 CancellationToken 协作取消。
        /// </summary>
        /// <param name="action">线程执行体，可通过 token 检查取消请求</param>
        /// <param name="token">取消令牌</param>
        /// <returns>启动的 Thread 实例</returns>
        public Thread StartThread(Action<CancellationToken> action, CancellationToken token = default)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    action(token);
                }
                catch (ThreadInterruptedException)
                {
                    Debug.Log("[ThreadManager] 线程被中断");
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("[ThreadManager] 线程收到取消请求并正常退出");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ThreadManager] 线程异常: {ex}");
                }
            });
            thread.Start();
            return thread;
        }

        /// <summary>
        /// 启动一个新线程（兼容旧 ThreadStart 委托）。
        /// 建议优先使用带 CancellationToken 的重载。
        /// </summary>
        [Obsolete("建议使用带 CancellationToken 的重载，以便支持安全取消。")]
        public Thread StartThread(ThreadStart start)
        {
            Thread thread = new Thread(start);
            thread.Start();
            return thread;
        }

        /// <summary>
        /// 安全停止线程：先发送取消信号，再尝试中断，最后等待线程结束。
        /// 不再使用已废弃的 Thread.Abort()。
        /// </summary>
        /// <param name="thread">要停止的线程</param>
        /// <param name="cts">关联的 CancellationTokenSource，用于发送取消信号</param>
        /// <param name="joinTimeoutMs">等待线程结束的最大毫秒数</param>
        public void StopThread(Thread thread, CancellationTokenSource cts = null, int joinTimeoutMs = 1000)
        {
            if (thread == null) return;

            // 1. 发送取消信号（协作式取消）
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource 已被释放，忽略
            }

            // 2. 如果线程仍在运行，尝试中断
            if (thread.IsAlive)
            {
                try
                {
                    thread.Interrupt();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ThreadManager] 中断线程时发生异常: {ex.Message}");
                }

                // 3. 等待线程自然结束
                if (!thread.Join(joinTimeoutMs))
                {
                    Debug.LogWarning($"[ThreadManager] 线程未在 {joinTimeoutMs}ms 内结束，可能仍在运行。");
                }
            }
        }
    }
}
