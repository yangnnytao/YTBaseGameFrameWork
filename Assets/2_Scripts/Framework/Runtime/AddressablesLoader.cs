using System;
using UnityEngine;
#if USE_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace YGZFrameWork
{
    /// <summary>
    /// Addressables 加载器 —— 基于 Unity Addressables 的加载实现。
    /// 支持热更新、分包加载、远程 Catalog 更新。
    /// 
    /// 使用条件编译 USE_ADDRESSABLES 控制是否启用，未定义时降级为 ResourcesLoader。
    /// </summary>
    public class AddressablesLoader : IResourceLoader
    {
        public static readonly AddressablesLoader Instance = new AddressablesLoader();

        public T Load<T>(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate) where T : UnityEngine.Object
        {
#if USE_ADDRESSABLES
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[AddressablesLoader] 路径为空");
                return null;
            }
            var handle = Addressables.LoadAssetAsync<T>(path);
            var asset = handle.WaitForCompletion();
            if (asset == null)
            {
                Debug.LogError($"[AddressablesLoader] 加载失败: {path}");
            }
            return asset;
#else
            Debug.LogWarning("[AddressablesLoader] USE_ADDRESSABLES 未定义，降级为 ResourcesLoader");
            return ResourcesLoader.Instance.Load<T>(path, unloadType);
#endif
        }

        public void LoadAsync<T>(string path, Action<T> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate) where T : UnityEngine.Object
        {
#if USE_ADDRESSABLES
            var handle = Addressables.LoadAssetAsync<T>(path);
            handle.Completed += op =>
            {
                if (op.Status == UnityEngine.ResourceManagement.AsyncOperationStatus.Succeeded)
                {
                    onComplete?.Invoke(op.Result);
                }
                else
                {
                    Debug.LogError($"[AddressablesLoader] 异步加载失败: {path}");
                    onComplete?.Invoke(null);
                }
            };
#else
            ResourcesLoader.Instance.LoadAsync(path, onComplete, unloadType);
#endif
        }

        public GameObject LoadPrefab(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            var prefab = Load<GameObject>(path, unloadType);
            if (prefab == null) return null;
            return UnityEngine.Object.Instantiate(prefab);
        }

        public void LoadPrefabAsync(string path, Action<GameObject> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            LoadAsync<GameObject>(path, prefab =>
            {
                if (prefab == null)
                {
                    onComplete?.Invoke(null);
                    return;
                }
                onComplete?.Invoke(UnityEngine.Object.Instantiate(prefab));
            }, unloadType);
        }

        public void Release(UnityEngine.Object asset)
        {
#if USE_ADDRESSABLES
            if (asset != null)
            {
                Addressables.Release(asset);
            }
#else
            // Resources 不需要释放
#endif
        }

        public void Preload<T>(string path) where T : UnityEngine.Object
        {
#if USE_ADDRESSABLES
            // Addressables 预加载：加载后保持引用
            _ = Load<T>(path);
#else
            ResourcesLoader.Instance.Preload<T>(path);
#endif
        }
    }
}
