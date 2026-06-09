using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_WEBGL && !WEIXIN_MINIGAME
using System.Threading;
#endif

namespace YGZFrameWork
{
    /// <summary>
    /// 后台任务管理器 —— 统一封装 Thread（原生平台）和 Coroutine（微信小游戏/WebGL）两种后台执行方式。
    /// 
    /// 设计说明：
    /// 1. 原生平台（Android/iOS/PC）：使用 Thread 进行真正的后台执行。
    /// 2. 微信小游戏 / WebGL：降级为 Coroutine 分帧执行（主线程，通过 yield 避免卡顿）。
    /// 3. 上层代码统一调用 StartTask / StopTask，不关心底层实现。
    /// 4. 所有平台均支持 CancellationToken 协作取消。
    /// </summary>
    public class ThreadManager : ManagerMono<ThreadManager>, IManagerInterface
    {
        public static ThreadManager Instance => MonoSingleton<ThreadManager>.Instance;

#if !UNITY_WEBGL && !WEIXIN_MINIGAME
        private readonly Dictionary<int, Thread> _threads = new Dictionary<int, Thread>();
        private int _threadIdCounter = 0;
#else
        private readonly Dictionary<int, Coroutine> _coroutines = new Dictionary<int, Coroutine>();
        private int _coroutineIdCounter = 0;
#endif

        public override void InitDataM()
        {
            base.InitDataM();
        }

        public override void DestroyM()
        {
#if !UNITY_WEBGL && !WEIXIN_MINIGAME
            foreach (var kvp in _threads)
            {
                try { kvp.Value.Interrupt(); } catch { }
            }
            _threads.Clear();
#else
            foreach (var kvp in _coroutines)
            {
                if (kvp.Value != null) StopCoroutine(kvp.Value);
            }
            _coroutines.Clear();
#endif
            base.DestroyM();
        }

        public void RegisterMsg() { }
        public void ClearData() { }

        #region 启动任务

        /// <summary>
        /// 启动后台任务（统一接口）
        /// </summary>
        /// <param name="action">任务执行体</param>
        /// <param name="token">取消令牌</param>
        /// <returns>任务 ID（用于停止）</returns>
        public int StartTask(Action action, System.Threading.CancellationToken token = default)
        {
            if (action == null) return -1;

#if !UNITY_WEBGL && !WEIXIN_MINIGAME
            // 原生平台：使用 Thread
            int id = ++_threadIdCounter;
            Thread thread = new Thread(() =>
            {
                try
                {
                    action();
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
            _threads[id] = thread;
            return id;
#else
            // 微信小游戏 / WebGL：降级为 Coroutine
            int id = ++_coroutineIdCounter;
            Coroutine coroutine = StartCoroutine(CoroutineTask(action, token, id));
            _coroutines[id] = coroutine;
            return id;
#endif
        }

#if UNITY_WEBGL || WEIXIN_MINIGAME
        private IEnumerator CoroutineTask(Action action, System.Threading.CancellationToken token, int id)
        {
            Debug.LogWarning("[ThreadManager] 微信小游戏/WebGL 不支持 Thread，任务在主线程 Coroutine 中执行。");
            
            // 分帧执行：将任务拆分到多帧避免卡顿
            // 注意：如果 action 是阻塞式的，仍会卡顿。建议 action 内部使用 yield 配合异步 API。
            yield return null;
            
            try
            {
                if (!token.IsCancellationRequested)
                {
                    action();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ThreadManager] Coroutine 任务收到取消请求");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ThreadManager] Coroutine 任务异常: {ex}");
            }

            _coroutines.Remove(id);
        }
#endif

        #endregion

        #region 停止任务

        /// <summary>
        /// 停止后台任务
        /// </summary>
        /// <param name="taskId">任务 ID</param>
        /// <param name="cts">关联的 CancellationTokenSource</param>
        public void StopTask(int taskId, System.Threading.CancellationTokenSource cts = null)
        {
            if (taskId < 0) return;

#if !UNITY_WEBGL && !WEIXIN_MINIGAME
            if (_threads.TryGetValue(taskId, out var thread))
            {
                try { cts?.Cancel(); } catch { }
                if (thread.IsAlive)
                {
                    try { thread.Interrupt(); } catch { }
                    thread.Join(1000);
                }
                _threads.Remove(taskId);
            }
#else
            if (_coroutines.TryGetValue(taskId, out var coroutine))
            {
                try { cts?.Cancel(); } catch { }
                if (coroutine != null) StopCoroutine(coroutine);
                _coroutines.Remove(taskId);
            }
#endif
        }

        #endregion

        #region 兼容旧接口

        [Obsolete("建议使用 StartTask(Action, CancellationToken)")]
        public void StartThread(System.Threading.ThreadStart start)
        {
            StartTask(() => start());
        }

        [Obsolete("建议使用 StopTask(int, CancellationTokenSource)")]
        public void StopThread(System.Threading.Thread thread, System.Threading.CancellationTokenSource cts = null, int joinTimeoutMs = 1000)
        {
            // 旧接口无法映射到新 ID 系统，直接忽略
            Debug.LogWarning("[ThreadManager] StopThread 已废弃，请使用 StopTask");
        }

        #endregion
    }
}
