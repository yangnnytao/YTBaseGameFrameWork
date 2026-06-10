using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace YGZFrameWork
{
    /// <summary>
    /// HTTP 请求任务
    /// </summary>
    public class HttpRequestTask
    {
        public string Url;
        public string Method; // GET / POST / JSON
        public Dictionary<string, string> FormData;
        public string JsonBody;
        public int Timeout;
        public int RetryCount;      // 剩余重试次数
        public int MaxRetry;        // 最大重试次数
        public Action<string> OnSuccess;
        public Action OnError;
        public float StartTime;
    }

    /// <summary>
    /// 网络管理器 —— 支持请求队列、并发控制、重试与熔断。
    /// </summary>
    public class NetWorkManager : ManagerMono<NetWorkManager>, IManagerInterface
    {
        public static NetWorkManager Instance => MonoSingleton<NetWorkManager>.Instance;

        public const int DEFAULT_TIMEOUT = 5;
        public const int MAX_CONCURRENT = 3;      // 最大并发请求数
        public const int MAX_RETRY = 2;           // 默认最大重试次数

        // 请求队列
        private readonly Queue<HttpRequestTask> _requestQueue = new Queue<HttpRequestTask>();
        private int _currentConcurrent = 0;

        // 熔断状态
        private int _consecutiveFailures = 0;
        private const int CIRCUIT_BREAK_THRESHOLD = 5; // 连续失败5次触发熔断
        private const float CIRCUIT_BREAK_COOLDOWN = 10f; // 熔断冷却10秒
        private float _circuitBreakResetTime = 0f;
        private bool _isCircuitBroken => _consecutiveFailures >= CIRCUIT_BREAK_THRESHOLD
                                         && Time.time < _circuitBreakResetTime;

        public override void InitDataM()
        {
            base.InitDataM();
            Debug.Log("[NetWorkManager] 初始化完成");
        }

        #region 公共 API

        /// <summary>发送 GET 请求</summary>
        public void SendGet(string url, Dictionary<string, string> param = null,
                            Action<string> onSuccess = null, Action onError = null,
                            int timeout = DEFAULT_TIMEOUT, int maxRetry = MAX_RETRY)
        {
            EnqueueRequest(url, "GET", param, null, timeout, maxRetry, onSuccess, onError);
        }

        /// <summary>发送 POST 请求</summary>
        public void SendPost(string url, Dictionary<string, string> form = null,
                             Action<string> onSuccess = null, Action onError = null,
                             int timeout = DEFAULT_TIMEOUT, int maxRetry = MAX_RETRY)
        {
            EnqueueRequest(url, "POST", form, null, timeout, maxRetry, onSuccess, onError);
        }

        /// <summary>发送 JSON 请求</summary>
        public void SendJson(string url, string jsonBody,
                             Action<string> onSuccess = null, Action onError = null,
                             int timeout = DEFAULT_TIMEOUT, int maxRetry = MAX_RETRY)
        {
            EnqueueRequest(url, "JSON", null, jsonBody, timeout, maxRetry, onSuccess, onError);
        }

        #endregion

        #region 请求队列与执行

        private void EnqueueRequest(string url, string method,
                                    Dictionary<string, string> form, string jsonBody,
                                    int timeout, int maxRetry,
                                    Action<string> onSuccess, Action onError)
        {
            if (_isCircuitBroken)
            {
                Debug.LogWarning("[NetWorkManager] 熔断中，请求被拒绝");
                onError?.Invoke();
                return;
            }

            var task = new HttpRequestTask
            {
                Url = url,
                Method = method,
                FormData = form,
                JsonBody = jsonBody,
                Timeout = timeout,
                MaxRetry = maxRetry,
                RetryCount = maxRetry,
                OnSuccess = onSuccess,
                OnError = onError,
                StartTime = Time.time
            };

            _requestQueue.Enqueue(task);
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            while (_currentConcurrent < MAX_CONCURRENT && _requestQueue.Count > 0)
            {
                var task = _requestQueue.Dequeue();
                _currentConcurrent++;
                StartCoroutine(ExecuteRequest(task));
            }
        }

        private IEnumerator ExecuteRequest(HttpRequestTask task)
        {
            // 检查网络
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogWarning("[NetWorkManager] 无网络连接");
                HandleFailure(task, "无网络");
                yield break;
            }

            UnityWebRequest request = null;

            if (task.Method == "GET")
            {
                string url = task.Url;
                if (task.FormData != null && task.FormData.Count > 0)
                {
                    url += "?";
                    foreach (var pair in task.FormData)
                        url += $"{pair.Key}={UnityWebRequest.EscapeURL(pair.Value)}&";
                    url = url.TrimEnd('&');
                }
                request = UnityWebRequest.Get(url);
            }
            else if (task.Method == "POST")
            {
                WWWForm form = new WWWForm();
                if (task.FormData != null)
                {
                    foreach (var pair in task.FormData)
                        form.AddField(pair.Key, pair.Value);
                }
                request = UnityWebRequest.Post(task.Url, form);
            }
            else if (task.Method == "JSON")
            {
                byte[] body = System.Text.Encoding.UTF8.GetBytes(task.JsonBody ?? "{}");
                request = new UnityWebRequest(task.Url, "POST");
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
            }

            if (request == null)
            {
                HandleFailure(task, "请求构建失败");
                yield break;
            }

            request.timeout = task.Timeout;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string text = request.downloadHandler.text;
                Debug.Log($"[NetWorkManager] 请求成功: {task.Url}, 耗时: {Time.time - task.StartTime:F2}s");
                _consecutiveFailures = 0; // 重置失败计数
                task.OnSuccess?.Invoke(text);
            }
            else
            {
                HandleFailure(task, request.error);
            }

            request.Dispose();
            _currentConcurrent--;
            ProcessQueue(); // 继续处理队列
        }

        private void HandleFailure(HttpRequestTask task, string error)
        {
            Debug.LogWarning($"[NetWorkManager] 请求失败: {task.Url}, 错误: {error}, 剩余重试: {task.RetryCount}");

            if (task.RetryCount > 0)
            {
                task.RetryCount--;
                // 指数退避：等待时间随重试次数增加
                float delay = Mathf.Pow(2, task.MaxRetry - task.RetryCount);
                StartCoroutine(RetryAfterDelay(task, delay));
            }
            else
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= CIRCUIT_BREAK_THRESHOLD)
                {
                    _circuitBreakResetTime = Time.time + CIRCUIT_BREAK_COOLDOWN;
                    Debug.LogError($"[NetWorkManager] 触发熔断！连续失败 {_consecutiveFailures} 次，冷却 {CIRCUIT_BREAK_COOLDOWN} 秒");
                }
                task.OnError?.Invoke();
            }
        }

        private IEnumerator RetryAfterDelay(HttpRequestTask task, float delay)
        {
            yield return new WaitForSeconds(delay);
            _requestQueue.Enqueue(task);
            ProcessQueue();
        }

        #endregion

        public void RegisterMsg() { }
        public void ClearData()
        {
            _requestQueue.Clear();
            _currentConcurrent = 0;
            _consecutiveFailures = 0;
        }
    }
}
