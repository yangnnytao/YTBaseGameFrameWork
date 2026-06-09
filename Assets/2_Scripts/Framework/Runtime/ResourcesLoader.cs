using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// Resources 加载器 —— 基于 Unity Resources.Load 的同步加载实现。
    /// 适用于编辑器调试、小型项目、或作为 Addressables 的降级回退。
    /// </summary>
    public class ResourcesLoader : IResourceLoader
    {
        public static readonly ResourcesLoader Instance = new ResourcesLoader();

        public T Load<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[ResourcesLoader] 路径为空");
                return null;
            }
            var asset = Resources.Load<T>(path);
            if (asset == null)
            {
                Debug.LogError($"[ResourcesLoader] 加载失败: Resources/{path}");
            }
            return asset;
        }

        public void LoadAsync<T>(string path, System.Action<T> onComplete) where T : UnityEngine.Object
        {
            // Resources.LoadAsync 是异步的，但这里用同步简化
            var request = Resources.LoadAsync<T>(path);
            // 简单轮询等待完成（实际项目中可优化为 Coroutine）
            while (!request.isDone)
            {
                // 阻塞等待，仅用于简化
            }
            onComplete?.Invoke(request.asset as T);
        }

        public GameObject LoadPrefab(string path)
        {
            var prefab = Load<GameObject>(path);
            if (prefab == null) return null;
            return Object.Instantiate(prefab);
        }

        public void LoadPrefabAsync(string path, System.Action<GameObject> onComplete)
        {
            LoadAsync<GameObject>(path, prefab =>
            {
                if (prefab == null)
                {
                    onComplete?.Invoke(null);
                    return;
                }
                onComplete?.Invoke(Object.Instantiate(prefab));
            });
        }

        public void Release(UnityEngine.Object asset)
        {
            // Resources.Load 不需要显式释放
        }

        public void Preload<T>(string path) where T : UnityEngine.Object
        {
            // Resources 预加载即直接加载
            _ = Load<T>(path);
        }
    }
}
