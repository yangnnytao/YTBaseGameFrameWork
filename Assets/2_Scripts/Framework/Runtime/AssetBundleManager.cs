using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// AssetBundle 管理器 —— 负责 AB 包的加载、缓存、依赖解析与释放。
    /// 
    /// 核心职责：
    /// 1. 维护已加载 AB 包的引用缓存（bundleName → AssetBundle）。
    /// 2. 自动解析并加载依赖 AB 包（基于 AssetBundleManifest）。
    /// 3. 支持从 StreamingAssets（首包）和 persistentDataPath（热更）双路径加载。
    /// 4. 支持场景 AB 包单独缓存（场景资源较大，切场景时统一卸载）。
    /// 5. 支持显式 GC 接口（切场景时手动回收内存）。
    /// 6. 提供同步/异步加载接口，上层通过 AssetBundleLoader 间接调用。
    /// 
    /// 生命周期：
    /// - InitDataM()：加载 Manifest，初始化路径配置。
    /// - DestroyM()：卸载所有 AB 包，清理缓存。
    /// </summary>
    public class AssetBundleManager : Singleton<AssetBundleManager>, IManagerInterface
    {
        #region 字段

        /// <summary>已加载的 AB 包缓存：bundleName → AssetBundle</summary>
        private readonly Dictionary<string, AssetBundle> _loadedBundles = new Dictionary<string, AssetBundle>();

        /// <summary>AB 包引用计数：bundleName → 引用次数</summary>
        private readonly Dictionary<string, int> _bundleRefCount = new Dictionary<string, int>();

        /// <summary>场景 AB 包单独缓存：bundleName → AssetBundle</summary>
        private readonly Dictionary<string, AssetBundle> _sceneBundles = new Dictionary<string, AssetBundle>();

        /// <summary>延迟卸载队列：bundleName → 待卸载标记</summary>
        private readonly HashSet<string> _lateUnloadQueue = new HashSet<string>();

        /// <summary>资源路径 → AB 包映射缓存（运行时从配置加载）</summary>
        private AssetBundleConfig _config;

        /// <summary>AB 包 Manifest（记录所有包的依赖关系）</summary>
        private AssetBundleManifest _manifest;

        /// <summary>AB 包根目录路径（运行时确定：优先 persistentDataPath）</summary>
        private string _bundleRootPath;

        #endregion

        #region 初始化

        public override void InitDataM()
        {
            base.InitDataM();
            _config = Resources.Load<AssetBundleConfig>("AssetBundleConfig");
            if (_config == null)
            {
                Debug.LogWarning("[AssetBundleManager] 未找到 AssetBundleConfig，使用默认配置");
                _config = ScriptableObject.CreateInstance<AssetBundleConfig>();
            }

            _bundleRootPath = DetermineBundleRootPath();
            Debug.Log($"[AssetBundleManager] AB 包根路径: {_bundleRootPath}");
            LoadManifest();
        }

        public override void DestroyM()
        {
            UnloadAllBundles(true);
            _loadedBundles.Clear();
            _bundleRefCount.Clear();
            _sceneBundles.Clear();
            _lateUnloadQueue.Clear();
            _manifest = null;
            base.DestroyM();
        }

        public void RegisterMsg() { }

        public void ClearData()
        {
            UnloadAllBundles(false);
            _loadedBundles.Clear();
            _bundleRefCount.Clear();
            _sceneBundles.Clear();
            _lateUnloadQueue.Clear();
        }

        #endregion

        #region 路径管理

        private string DetermineBundleRootPath()
        {
            if (_config.preferPersistentPath)
            {
                string persistentPath = Path.Combine(Application.persistentDataPath, _config.persistentBundlePath);
                if (Directory.Exists(persistentPath) && File.Exists(Path.Combine(persistentPath, _config.assetMapFileName)))
                {
                    return persistentPath;
                }
            }
            return Path.Combine(Application.streamingAssetsPath, _config.streamingAssetsBundlePath);
        }

        public string GetBundleFilePath(string bundleName)
        {
            return Path.Combine(_bundleRootPath, bundleName);
        }

        #endregion

        #region Manifest 加载

        private void LoadManifest()
        {
            string manifestBundleName = Path.GetFileName(_bundleRootPath);
            string manifestPath = GetBundleFilePath(manifestBundleName);

            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"[AssetBundleManager] Manifest 文件不存在: {manifestPath}");
                return;
            }

            AssetBundle manifestBundle = AssetBundle.LoadFromFile(manifestPath);
            if (manifestBundle == null)
            {
                Debug.LogError($"[AssetBundleManager] Manifest AB 包加载失败: {manifestPath}");
                return;
            }

            _manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            manifestBundle.Unload(false);

            if (_manifest == null)
            {
                Debug.LogError("[AssetBundleManager] AssetBundleManifest 资源加载失败");
            }
            else
            {
                Debug.Log("[AssetBundleManager] Manifest 加载成功");
            }
        }

        #endregion

        #region AB 包加载（同步）

        public AssetBundle LoadBundle(string bundleName, bool isSceneBundle = false)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                Debug.LogError("[AssetBundleManager] bundleName 为空");
                return null;
            }

            // 场景包单独缓存
            if (isSceneBundle && _config.cacheSceneBundles)
            {
                if (_sceneBundles.TryGetValue(bundleName, out AssetBundle sceneBundle))
                {
                    return sceneBundle;
                }
            }

            // 普通包缓存
            if (_loadedBundles.TryGetValue(bundleName, out AssetBundle cachedBundle))
            {
                _bundleRefCount[bundleName] = _bundleRefCount.GetValueOrDefault(bundleName, 0) + 1;
                return cachedBundle;
            }

            LoadDependencies(bundleName);

            string bundlePath = GetBundleFilePath(bundleName);
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Debug.LogError($"[AssetBundleManager] AB 包加载失败: {bundlePath}");
                return null;
            }

            if (isSceneBundle && _config.cacheSceneBundles)
            {
                _sceneBundles[bundleName] = bundle;
            }
            else
            {
                _loadedBundles[bundleName] = bundle;
                _bundleRefCount[bundleName] = 1;
            }

            Debug.Log($"[AssetBundleManager] AB 包加载成功: {bundleName}");
            return bundle;
        }

        private void LoadDependencies(string bundleName)
        {
            if (_manifest == null) return;

            string[] dependencies = _manifest.GetAllDependencies(bundleName);
            foreach (string dep in dependencies)
            {
                if (_loadedBundles.ContainsKey(dep))
                {
                    _bundleRefCount[dep] = _bundleRefCount.GetValueOrDefault(dep, 0) + 1;
                }
                else
                {
                    string depPath = GetBundleFilePath(dep);
                    AssetBundle depBundle = AssetBundle.LoadFromFile(depPath);
                    if (depBundle != null)
                    {
                        _loadedBundles[dep] = depBundle;
                        _bundleRefCount[dep] = 1;
                        Debug.Log($"[AssetBundleManager] 依赖包加载: {dep}");
                    }
                    else
                    {
                        Debug.LogError($"[AssetBundleManager] 依赖包加载失败: {dep}");
                    }
                }
            }
        }

        #endregion

        #region AB 包加载（异步）

        public void LoadBundleAsync(string bundleName, Action<AssetBundle> onComplete, bool isSceneBundle = false)
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                Debug.LogError("[AssetBundleManager] bundleName 为空");
                onComplete?.Invoke(null);
                return;
            }

            // 场景包单独缓存
            if (isSceneBundle && _config.cacheSceneBundles)
            {
                if (_sceneBundles.TryGetValue(bundleName, out AssetBundle sceneBundle))
                {
                    onComplete?.Invoke(sceneBundle);
                    return;
                }
            }

            // 普通包缓存
            if (_loadedBundles.TryGetValue(bundleName, out AssetBundle cachedBundle))
            {
                _bundleRefCount[bundleName] = _bundleRefCount.GetValueOrDefault(bundleName, 0) + 1;
                onComplete?.Invoke(cachedBundle);
                return;
            }

            CoroutineRunner.Instance.StartCoroutine(LoadBundleAsyncCoroutine(bundleName, onComplete, isSceneBundle));
        }

        private IEnumerator LoadBundleAsyncCoroutine(string bundleName, Action<AssetBundle> onComplete, bool isSceneBundle)
        {
            if (_manifest != null)
            {
                string[] dependencies = _manifest.GetAllDependencies(bundleName);
                foreach (string dep in dependencies)
                {
                    if (!_loadedBundles.ContainsKey(dep))
                    {
                        string depPath = GetBundleFilePath(dep);
                        AssetBundleCreateRequest depRequest = AssetBundle.LoadFromFileAsync(depPath);
                        yield return depRequest;

                        if (depRequest.assetBundle != null)
                        {
                            _loadedBundles[dep] = depRequest.assetBundle;
                            _bundleRefCount[dep] = 1;
                        }
                        else
                        {
                            Debug.LogError($"[AssetBundleManager] 依赖包异步加载失败: {dep}");
                        }
                    }
                    else
                    {
                        _bundleRefCount[dep] = _bundleRefCount.GetValueOrDefault(dep, 0) + 1;
                    }
                }
            }

            string bundlePath = GetBundleFilePath(bundleName);
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return request;

            if (request.assetBundle != null)
            {
                if (isSceneBundle && _config.cacheSceneBundles)
                {
                    _sceneBundles[bundleName] = request.assetBundle;
                }
                else
                {
                    _loadedBundles[bundleName] = request.assetBundle;
                    _bundleRefCount[bundleName] = 1;
                }
                Debug.Log($"[AssetBundleManager] AB 包异步加载成功: {bundleName}");
                onComplete?.Invoke(request.assetBundle);
            }
            else
            {
                Debug.LogError($"[AssetBundleManager] AB 包异步加载失败: {bundlePath}");
                onComplete?.Invoke(null);
            }
        }

        #endregion

        #region 资源加载

        public T LoadAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
        {
            AssetBundle bundle = LoadBundle(bundleName);
            if (bundle == null) return null;

            T asset = bundle.LoadAsset<T>(assetName);
            if (asset == null)
            {
                Debug.LogError($"[AssetBundleManager] 资源加载失败: {bundleName}/{assetName}");
            }
            return asset;
        }

        public void LoadAssetAsync<T>(string bundleName, string assetName, Action<T> onComplete) where T : UnityEngine.Object
        {
            LoadBundleAsync(bundleName, bundle =>
            {
                if (bundle == null)
                {
                    onComplete?.Invoke(null);
                    return;
                }

                CoroutineRunner.Instance.StartCoroutine(LoadAssetAsyncCoroutine<T>(bundle, assetName, onComplete));
            });
        }

        private IEnumerator LoadAssetAsyncCoroutine<T>(AssetBundle bundle, string assetName, Action<T> onComplete) where T : UnityEngine.Object
        {
            AssetBundleRequest request = bundle.LoadAssetAsync<T>(assetName);
            yield return request;

            if (request.asset != null)
            {
                onComplete?.Invoke(request.asset as T);
            }
            else
            {
                Debug.LogError($"[AssetBundleManager] 资源异步加载失败: {bundle.name}/{assetName}");
                onComplete?.Invoke(null);
            }
        }

        #endregion

        #region 释放与卸载

        public void ReleaseBundle(string bundleName)
        {
            if (!_bundleRefCount.ContainsKey(bundleName)) return;

            _bundleRefCount[bundleName]--;
            if (_bundleRefCount[bundleName] <= 0)
            {
                UnloadBundle(bundleName, false);
            }
        }

        public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (_loadedBundles.TryGetValue(bundleName, out AssetBundle bundle))
            {
                bundle.Unload(unloadAllLoadedObjects);
                _loadedBundles.Remove(bundleName);
                _bundleRefCount.Remove(bundleName);
                _lateUnloadQueue.Remove(bundleName);
                Debug.Log($"[AssetBundleManager] AB 包卸载: {bundleName}");
            }
        }

        public void UnloadAllBundles(bool unloadAllLoadedObjects = false)
        {
            foreach (var kvp in _loadedBundles)
            {
                kvp.Value?.Unload(unloadAllLoadedObjects);
            }
            _loadedBundles.Clear();
            _bundleRefCount.Clear();
            _lateUnloadQueue.Clear();
            Debug.Log("[AssetBundleManager] 所有普通 AB 包已卸载");
        }

        /// <summary>
        /// 卸载所有场景 AB 包。
        /// </summary>
        public void UnloadAllSceneBundles(bool unloadAllLoadedObjects = false)
        {
            foreach (var kvp in _sceneBundles)
            {
                kvp.Value?.Unload(unloadAllLoadedObjects);
            }
            _sceneBundles.Clear();
            Debug.Log("[AssetBundleManager] 所有场景 AB 包已卸载");
        }

        /// <summary>
        /// 执行延迟卸载队列中的 AB 包。
        /// </summary>
        public void ProcessLateUnloadQueue()
        {
            foreach (string bundleName in _lateUnloadQueue)
            {
                UnloadBundle(bundleName, false);
            }
            _lateUnloadQueue.Clear();
            Debug.Log("[AssetBundleManager] 延迟卸载队列已处理");
        }

        #endregion

        #region 显式 GC

        /// <summary>
        /// 显式触发垃圾回收和资源清理。
        /// 建议在切场景、内存告警时调用。
        /// </summary>
        public void GC()
        {
            // 1. 处理延迟卸载队列
            ProcessLateUnloadQueue();

            // 2. 卸载未使用的资源
            Resources.UnloadUnusedAssets();

            // 3. 触发系统 GC
            System.GC.Collect();

            Debug.Log("[AssetBundleManager] GC 完成");
        }

        #endregion

        #region 配置查询

        public bool TryGetAssetMapping(string assetPath, out AssetBundleConfig.AssetBundleMapping mapping)
        {
            mapping = null;
            if (_config == null) return false;
            return _config.TryGetMapping(assetPath, out mapping);
        }

        public string GetBundleRootPath() => _bundleRootPath;

        /// <summary>
        /// 获取指定资源分类的默认卸载策略。
        /// </summary>
        public ResourceUnloadType GetDefaultUnloadType(ResourceCategory category)
        {
            if (_config == null) return ResourceUnloadType.UnloadLate;

            var config = _config.categoryUnloadConfigs.Find(c => c.category == category);
            return config?.defaultUnloadType ?? ResourceUnloadType.UnloadLate;
        }

        #endregion
    }
}
