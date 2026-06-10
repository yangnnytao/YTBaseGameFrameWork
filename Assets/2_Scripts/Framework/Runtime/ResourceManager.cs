using System;
using System.Collections.Generic;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 资源管理器 —— 统一资源加载入口，封装底层加载器切换逻辑。
    /// 
    /// 设计说明：
    /// 1. 支持三种加载模式：Resources（编辑器/小包）、AssetBundle（完整包/热更）、Addressables（可选扩展）。
    /// 2. 通过 AppConst.ResourceLoadMode 或构建时宏切换底层加载器。
    /// 3. 上层代码（UIManager、CfgToolManager 等）统一通过 ResourceManager.Loader 访问。
    /// 4. 编辑器默认使用 EditorAssetLoader（从 4_GameAssets 直接加载），移动端默认使用 AssetBundle。
    /// 5. 集成 GameObject 对象池（高频对象自动复用）。
    /// 6. 集成 Sprite 图集缓存（双层缓存结构）。
    /// 7. 提供显式 GC 接口（切场景时手动回收内存）。
    /// </summary>
    public class ResourceManager : Singleton<ResourceManager>, IManagerInterface
    {
        public static ResourceManager Instance => GetInstance();

        /// <summary>当前使用的资源加载器</summary>
        public IResourceLoader Loader { get; private set; }

        /// <summary>当前加载模式</summary>
        public ResourceLoadMode CurrentMode { get; private set; }

        #region 对象池

        /// <summary>GameObject 对象池缓存：资源路径 → GameObjectPool</summary>
        private readonly Dictionary<string, GameObjectPool> _objectPools = new Dictionary<string, GameObjectPool>();

        /// <summary>对象池默认父节点（可为 null）</summary>
        private Transform _poolParent;

        #endregion

        public override void InitDataM()
        {
            base.InitDataM();

            CurrentMode = DetermineLoadMode();

            switch (CurrentMode)
            {
                case ResourceLoadMode.Resources:
#if UNITY_EDITOR
                    // 编辑器模式下优先使用 EditorAssetLoader（从 4_GameAssets 直接加载）
                    Loader = EditorAssetLoader.Instance;
                    Debug.Log("[ResourceManager] 当前模式：Editor（从 4_GameAssets 直接加载）");
#else
                    // 真机 Resources 模式
                    Loader = ResourcesLoader.Instance;
                    Debug.Log("[ResourceManager] 当前模式：Resources（小包）");
#endif
                    break;

                case ResourceLoadMode.AssetBundle:
                    AssetBundleManager.Instance.InitDataM();
                    Loader = AssetBundleLoader.Instance;
                    Debug.Log("[ResourceManager] 当前模式：AssetBundle（完整包/热更）");
                    break;

                case ResourceLoadMode.Addressables:
#if USE_ADDRESSABLES
                    Loader = AddressablesLoader.Instance;
                    Debug.Log("[ResourceManager] 当前模式：Addressables（可选扩展）");
#else
                    Debug.LogWarning("[ResourceManager] USE_ADDRESSABLES 未定义，降级为 Resources");
                    Loader = ResourcesLoader.Instance;
                    CurrentMode = ResourceLoadMode.Resources;
#endif
                    break;

                default:
#if UNITY_EDITOR
                    Loader = EditorAssetLoader.Instance;
                    Debug.LogWarning($"[ResourceManager] 未知模式 {CurrentMode}，编辑器降级为 EditorAssetLoader");
#else
                    Loader = ResourcesLoader.Instance;
                    Debug.LogWarning($"[ResourceManager] 未知模式 {CurrentMode}，降级为 Resources");
#endif
                    break;
            }
        }

        public override void DestroyM()
        {
            // 清理对象池
            ClearAllPools();

            if (CurrentMode == ResourceLoadMode.AssetBundle)
            {
                AssetBundleManager.Instance.DestroyM();
            }

            Loader = null;
            base.DestroyM();
        }

        public void RegisterMsg() { }

        public void ClearData()
        {
            ClearAllPools();
            if (CurrentMode == ResourceLoadMode.AssetBundle)
            {
                AssetBundleManager.Instance.ClearData();
            }
        }

        #region 模式判定

        private ResourceLoadMode DetermineLoadMode()
        {
#if USE_ADDRESSABLES
            return ResourceLoadMode.Addressables;
#endif

#if UNITY_EDITOR
            return ResourceLoadMode.Resources;
#endif

            if (AppConst.ResourceLoadMode != ResourceLoadMode.Addressables)
            {
                return AppConst.ResourceLoadMode;
            }

            return ResourceLoadMode.AssetBundle;
        }

        #endregion

        #region 对象池接口

        /// <summary>
        /// 注册对象池（用于高频创建的 GameObject）。
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="preloadCount">预加载数量</param>
        /// <param name="maxCapacity">最大容量</param>
        public void RegisterPool(string path, int preloadCount = 5, int maxCapacity = 50)
        {
            if (_objectPools.ContainsKey(path)) return;

            // 加载预制体
            GameObject prefab = Loader.Load<GameObject>(path, ResourceUnloadType.UnLoadNone);
            if (prefab == null)
            {
                Debug.LogError($"[ResourceManager] 注册对象池失败，无法加载预制体: {path}");
                return;
            }

            var pool = new GameObjectPool(prefab, _poolParent, preloadCount, maxCapacity);
            _objectPools[path] = pool;
            Debug.Log($"[ResourceManager] 对象池注册成功: {path}");
        }

        /// <summary>
        /// 尝试从对象池获取 GameObject。
        /// </summary>
        public bool TryGetFromPool(string path, out GameObject go)
        {
            go = null;
            if (!_objectPools.TryGetValue(path, out var pool))
                return false;

            go = pool.Get();
            return go != null;
        }

        /// <summary>
        /// 回收 GameObject 到对象池。
        /// </summary>
        public bool RecycleToPool(string path, GameObject go)
        {
            if (go == null) return false;
            if (!_objectPools.TryGetValue(path, out var pool))
                return false;

            pool.Recycle(go);
            return true;
        }

        /// <summary>
        /// 清空指定对象池。
        /// </summary>
        public void ClearPool(string path)
        {
            if (_objectPools.TryGetValue(path, out var pool))
            {
                pool.Clear();
                _objectPools.Remove(path);
            }
        }

        /// <summary>
        /// 清空所有对象池。
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var kvp in _objectPools)
            {
                kvp.Value?.Clear();
            }
            _objectPools.Clear();
        }

        /// <summary>
        /// 设置对象池父节点（回收后的 GameObject 挂载于此）。
        /// </summary>
        public void SetPoolParent(Transform parent)
        {
            _poolParent = parent;
        }

        #endregion

        #region Sprite 图集缓存（委托给 AssetBundleLoader）

        /// <summary>
        /// 加载 Sprite（支持图集缓存，仅 AssetBundle 模式有效）。
        /// </summary>
        public Sprite LoadSprite(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            if (Loader is AssetBundleLoader abLoader)
            {
                return abLoader.LoadSprite(path, unloadType);
            }

            // Resources 模式下直接加载
            return Loader.Load<Sprite>(path, unloadType);
        }

        /// <summary>
        /// 异步加载 Sprite（支持图集缓存，仅 AssetBundle 模式有效）。
        /// </summary>
        public void LoadSpriteAsync(string path, Action<Sprite> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            if (Loader is AssetBundleLoader abLoader)
            {
                abLoader.LoadSpriteAsync(path, onComplete, unloadType);
                return;
            }

            // Resources 模式下直接异步加载
            Loader.LoadAsync<Sprite>(path, onComplete, unloadType);
        }

        /// <summary>
        /// 清空 Sprite 缓存（仅 AssetBundle 模式有效）。
        /// </summary>
        public void ClearSpriteCache()
        {
            if (Loader is AssetBundleLoader abLoader)
            {
                abLoader.ClearSpriteCache();
            }
        }

        #endregion

        #region 显式 GC

        /// <summary>
        /// 显式触发垃圾回收和资源清理。
        /// 建议在切场景、内存告警时调用。
        /// </summary>
        public void GC()
        {
            if (CurrentMode == ResourceLoadMode.AssetBundle)
            {
                AssetBundleManager.Instance.GC();
            }
            else
            {
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
                Debug.Log("[ResourceManager] GC 完成");
            }
        }

        /// <summary>
        /// 卸载所有场景 AB 包（仅 AssetBundle 模式有效）。
        /// </summary>
        public void UnloadAllSceneBundles()
        {
            if (CurrentMode == ResourceLoadMode.AssetBundle)
            {
                AssetBundleManager.Instance.UnloadAllSceneBundles();
            }
        }

        #endregion

        #region 便捷加载接口

        /// <summary>
        /// 按资源分类加载（自动应用默认卸载策略）。
        /// </summary>
        public T LoadByCategory<T>(string path, ResourceCategory category) where T : UnityEngine.Object
        {
            ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate;
            if (CurrentMode == ResourceLoadMode.AssetBundle)
            {
                unloadType = AssetBundleManager.Instance.GetDefaultUnloadType(category);
            }
            return Loader.Load<T>(path, unloadType);
        }

        #endregion
    }
}
