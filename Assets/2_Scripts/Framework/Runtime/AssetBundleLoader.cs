using System;
using System.Collections.Generic;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// AssetBundle 加载器 —— 基于原生 Unity AssetBundle 的 IResourceLoader 实现。
    /// 
    /// 设计说明：
    /// 1. 通过 AssetBundleManager 管理底层 AB 包的加载、缓存与依赖解析。
    /// 2. 支持卸载策略控制（常驻/立即释放/延迟释放）。
    /// 3. 支持 GameObject 对象池集成（高频对象自动复用）。
    /// 4. 支持 Sprite 图集双层缓存（atlas → spriteName → Sprite）。
    /// 5. 上层代码统一通过 ResourceManager.Loader 访问，不关心底层是 Resources 还是 AB。
    /// </summary>
    public class AssetBundleLoader : IResourceLoader
    {
        public static readonly AssetBundleLoader Instance = new AssetBundleLoader();

        #region Sprite 图集缓存

        /// <summary>
        /// Sprite 双层缓存：atlasPath → spriteName → Sprite
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, Sprite>> _spriteCache = new Dictionary<string, Dictionary<string, Sprite>>();

        /// <summary>
        /// 从缓存中获取 Sprite。
        /// </summary>
        public Sprite GetCachedSprite(string atlasPath, string spriteName)
        {
            if (_spriteCache.TryGetValue(atlasPath, out var atlasDict))
            {
                if (atlasDict.TryGetValue(spriteName, out Sprite sprite))
                {
                    return sprite;
                }
            }
            return null;
        }

        /// <summary>
        /// 将 Sprite 存入缓存。
        /// </summary>
        public void CacheSprite(string atlasPath, string spriteName, Sprite sprite)
        {
            if (!_spriteCache.TryGetValue(atlasPath, out var atlasDict))
            {
                atlasDict = new Dictionary<string, Sprite>();
                _spriteCache[atlasPath] = atlasDict;
            }
            atlasDict[spriteName] = sprite;
        }

        /// <summary>
        /// 清空 Sprite 缓存。
        /// </summary>
        public void ClearSpriteCache()
        {
            _spriteCache.Clear();
        }

        #endregion

        #region 同步加载

        public T Load<T>(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[AssetBundleLoader] 路径为空");
                return null;
            }

            // 查询资源映射
            if (!AssetBundleManager.Instance.TryGetAssetMapping(path, out var mapping))
            {
                Debug.LogError($"[AssetBundleLoader] 未找到资源映射: {path}");
                return null;
            }

            // 从 AB 包加载资源
            T asset = AssetBundleManager.Instance.LoadAsset<T>(mapping.bundleName, mapping.assetName);
            if (asset == null)
            {
                Debug.LogError($"[AssetBundleLoader] 加载失败: {path} (bundle: {mapping.bundleName}, asset: {mapping.assetName})");
                return null;
            }

            // 根据卸载策略处理
            HandleUnloadStrategy(mapping.bundleName, unloadType);
            return asset;
        }

        public GameObject LoadPrefab(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            // 尝试从对象池获取
            if (ResourceManager.Instance.TryGetFromPool(path, out GameObject pooledGo))
            {
                return pooledGo;
            }

            var prefab = Load<GameObject>(path, unloadType);
            if (prefab == null) return null;
            return UnityEngine.Object.Instantiate(prefab);
        }

        #endregion

        #region 异步加载

        public void LoadAsync<T>(string path, Action<T> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[AssetBundleLoader] 路径为空");
                onComplete?.Invoke(null);
                return;
            }

            if (!AssetBundleManager.Instance.TryGetAssetMapping(path, out var mapping))
            {
                Debug.LogError($"[AssetBundleLoader] 未找到资源映射: {path}");
                onComplete?.Invoke(null);
                return;
            }

            AssetBundleManager.Instance.LoadAssetAsync<T>(mapping.bundleName, mapping.assetName, asset =>
            {
                if (asset == null)
                {
                    Debug.LogError($"[AssetBundleLoader] 异步加载失败: {path}");
                }
                else
                {
                    HandleUnloadStrategy(mapping.bundleName, unloadType);
                }
                onComplete?.Invoke(asset);
            });
        }

        public void LoadPrefabAsync(string path, Action<GameObject> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            // 尝试从对象池获取
            if (ResourceManager.Instance.TryGetFromPool(path, out GameObject pooledGo))
            {
                onComplete?.Invoke(pooledGo);
                return;
            }

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

        #endregion

        #region 资源释放

        public void Release(UnityEngine.Object asset)
        {
            // AssetBundle 加载的资源由 AssetBundleManager 的引用计数管理。
            // 上层调用 Release 时，AssetBundleLoader 不直接操作，
            // 因为 AssetBundle 的资源释放逻辑与 Resources 不同（需释放 Bundle 引用而非 Object）。
            // 如需精细控制，可通过 AssetBundleManager.ReleaseBundle(bundleName) 释放指定包。
        }

        public void Preload<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path)) return;

            if (!AssetBundleManager.Instance.TryGetAssetMapping(path, out var mapping))
            {
                Debug.LogWarning($"[AssetBundleLoader] 预加载失败，未找到映射: {path}");
                return;
            }

            AssetBundleManager.Instance.LoadBundle(mapping.bundleName);
            Debug.Log($"[AssetBundleLoader] 预加载 AB 包: {mapping.bundleName}");
        }

        #endregion

        #region 卸载策略处理

        /// <summary>
        /// 根据卸载策略处理 AB 包引用。
        /// </summary>
        private void HandleUnloadStrategy(string bundleName, ResourceUnloadType unloadType)
        {
            switch (unloadType)
            {
                case ResourceUnloadType.UnLoadNone:
                    // 常驻资源：不释放引用，保持 AB 包常驻内存
                    break;

                case ResourceUnloadType.UnLoadImmediately:
                    // 立即释放：减少引用计数，可能触发卸载
                    AssetBundleManager.Instance.ReleaseBundle(bundleName);
                    break;

                case ResourceUnloadType.UnloadLate:
                    // 延迟释放：由上层统一 GC 时处理，此处不做操作
                    break;
            }
        }

        #endregion

        #region Sprite 专用加载

        /// <summary>
        /// 加载 Sprite（支持图集缓存）。
        /// 路径格式："atlasPath/spriteName" 或 "UI/Icons/AttackIcon"
        /// </summary>
        public Sprite LoadSprite(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            // 解析 atlasPath 和 spriteName
            string atlasPath = path;
            string spriteName = ResourcePathUtil.GetAssetNameFromPath(path);

            // 1. 尝试从缓存获取
            Sprite cached = GetCachedSprite(atlasPath, spriteName);
            if (cached != null)
            {
                return cached;
            }

            // 2. 从 AB 包加载
            Sprite sprite = Load<Sprite>(path, unloadType);
            if (sprite != null)
            {
                CacheSprite(atlasPath, spriteName, sprite);
            }
            return sprite;
        }

        /// <summary>
        /// 异步加载 Sprite（支持图集缓存）。
        /// </summary>
        public void LoadSpriteAsync(string path, Action<Sprite> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            string atlasPath = path;
            string spriteName = ResourcePathUtil.GetAssetNameFromPath(path);

            // 1. 尝试从缓存获取
            Sprite cached = GetCachedSprite(atlasPath, spriteName);
            if (cached != null)
            {
                onComplete?.Invoke(cached);
                return;
            }

            // 2. 从 AB 包加载
            LoadAsync<Sprite>(path, sprite =>
            {
                if (sprite != null)
                {
                    CacheSprite(atlasPath, spriteName, sprite);
                }
                onComplete?.Invoke(sprite);
            }, unloadType);
        }

        #endregion
    }
}
