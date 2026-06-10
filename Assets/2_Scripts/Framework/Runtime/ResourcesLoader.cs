using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// Resources 加载器 —— 基于 Unity Resources.Load 的同步加载实现。
    /// 适用于编辑器调试、小型项目、或作为 AssetBundle 的降级回退。
    /// </summary>
    public class ResourcesLoader : IResourceLoader
    {
        public static readonly ResourcesLoader Instance = new ResourcesLoader();

        public T Load<T>(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate) where T : UnityEngine.Object
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

        public void LoadAsync<T>(string path, System.Action<T> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate) where T : UnityEngine.Object
        {
            var request = Resources.LoadAsync<T>(path);
            CoroutineRunner.Instance.StartCoroutine(LoadAsyncCoroutine(request, onComplete));
        }

        private System.Collections.IEnumerator LoadAsyncCoroutine<T>(ResourceRequest request, System.Action<T> onComplete) where T : UnityEngine.Object
        {
            yield return request;
            onComplete?.Invoke(request.asset as T);
        }

        public GameObject LoadPrefab(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            var prefab = Load<GameObject>(path, unloadType);
            if (prefab == null) return null;
            return Object.Instantiate(prefab);
        }

        public void LoadPrefabAsync(string path, System.Action<GameObject> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            LoadAsync<GameObject>(path, prefab =>
            {
                if (prefab == null)
                {
                    onComplete?.Invoke(null);
                    return;
                }
                onComplete?.Invoke(Object.Instantiate(prefab));
            }, unloadType);
        }

        public void Release(UnityEngine.Object asset)
        {
            // Resources.Load 不需要显式释放
        }

        public void Preload<T>(string path) where T : UnityEngine.Object
        {
            _ = Load<T>(path);
        }
    }
}
