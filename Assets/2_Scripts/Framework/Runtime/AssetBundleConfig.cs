using System;
using System.Collections.Generic;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// AssetBundle 资源配置 —— 定义 AB 包路径、资源映射、加载策略。
    /// 
    /// 使用方式：
    /// 1. 在 Editor 中创建 AssetBundleConfig.asset（ScriptableObject）。
    /// 2. 配置 AB 包根路径、资源路径映射表。
    /// 3. 运行时通过 AssetBundleManager 读取此配置。
    /// </summary>
    [CreateAssetMenu(fileName = "AssetBundleConfig", menuName = "YGZFrameWork/AssetBundleConfig")]
    public class AssetBundleConfig : ScriptableObject
    {
        #region 路径配置

        /// <summary>
        /// AB 包在 StreamingAssets 中的相对路径（首次安装时的内置 AB 包）。
        /// 例如："AssetBundles" → StreamingAssets/AssetBundles/
        /// </summary>
        [Tooltip("StreamingAssets 中 AB 包目录名")]
        public string streamingAssetsBundlePath = "AssetBundles";

        /// <summary>
        /// AB 包在持久化路径中的目录名（热更下载后的 AB 包）。
        /// 例如："AssetBundles" → Application.persistentDataPath/AssetBundles/
        /// </summary>
        [Tooltip("持久化路径中 AB 包目录名（热更后覆盖）")]
        public string persistentBundlePath = "AssetBundles";

        /// <summary>
        /// 资源映射文件路径（JSON 格式，记录资源路径 → AB 包名 + 资源名的映射）。
        /// 例如："AssetBundles/AssetBundleManifest.json"
        /// </summary>
        [Tooltip("资源映射表 JSON 文件名（含相对路径）")]
        public string assetMapFileName = "AssetBundleManifest.json";

        /// <summary>
        /// 编辑器模式下资源根目录（相对于 Assets/ 的路径）。
        /// 例如："4_GameAssets" → 编辑器从 Assets/4_GameAssets/ 直接加载原始资源。
        /// </summary>
        [Tooltip("编辑器资源根目录（相对于 Assets/）")]
        public string editorAssetRoot = "4_GameAssets";

        /// <summary>
        /// 加载优先级：true 优先从持久化路径加载（热更覆盖），false 优先从 StreamingAssets 加载。
        /// </summary>
        [Tooltip("true = 优先持久化路径（热更覆盖），false = 优先 StreamingAssets")]
        public bool preferPersistentPath = true;

        /// <summary>
        /// 场景 AB 包是否单独缓存（场景资源通常较大，切场景时统一卸载）。
        /// </summary>
        [Tooltip("场景 AB 包单独缓存管理")]
        public bool cacheSceneBundles = true;

        #endregion

        #region 资源分类配置

        /// <summary>
        /// 资源分类 → 默认卸载策略映射。
        /// 用于按类型批量管理资源生命周期。
        /// </summary>
        [Serializable]
        public class CategoryUnloadConfig
        {
            public ResourceCategory category;
            public ResourceUnloadType defaultUnloadType = ResourceUnloadType.UnloadLate;
        }

        public List<CategoryUnloadConfig> categoryUnloadConfigs = new List<CategoryUnloadConfig>
        {
            new CategoryUnloadConfig { category = ResourceCategory.UI, defaultUnloadType = ResourceUnloadType.UnloadLate },
            new CategoryUnloadConfig { category = ResourceCategory.Effect, defaultUnloadType = ResourceUnloadType.UnLoadImmediately },
            new CategoryUnloadConfig { category = ResourceCategory.Scene, defaultUnloadType = ResourceUnloadType.UnloadLate },
            new CategoryUnloadConfig { category = ResourceCategory.Config, defaultUnloadType = ResourceUnloadType.UnLoadNone },
        };

        #endregion

        #region 资源映射

        /// <summary>
        /// 资源路径 → AB 包信息映射表。
        /// Key：上层统一资源路径（如 "UI/MainMenu"）。
        /// Value：AB 包名 + 内部资源名。
        /// </summary>
        [Serializable]
        public class AssetBundleMapping
        {
            /// <summary>上层统一资源路径（如 "UI/MainMenu"）</summary>
            public string assetPath;
            /// <summary>AB 包名（如 "ui.bundle"）</summary>
            public string bundleName;
            /// <summary>AB 包内资源名（如 "MainMenu"）</summary>
            public string assetName;
        }

        /// <summary>
        /// 资源映射列表（Editor 中可手动配置，或运行时从 JSON 加载）。
        /// </summary>
        public List<AssetBundleMapping> assetMappings = new List<AssetBundleMapping>();

        #endregion

        #region 运行时辅助

        /// <summary>
        /// 根据上层资源路径查找 AB 包映射信息。
        /// </summary>
        public bool TryGetMapping(string assetPath, out AssetBundleMapping mapping)
        {
            mapping = assetMappings.Find(m => m.assetPath == assetPath);
            return mapping != null;
        }

        /// <summary>
        /// 获取 AB 包完整路径（优先持久化路径，其次 StreamingAssets）。
        /// </summary>
        public string GetBundlePath(string bundleName)
        {
            // 优先持久化路径（热更覆盖）
            if (preferPersistentPath)
            {
                string persistentPath = System.IO.Path.Combine(
                    Application.persistentDataPath, persistentBundlePath, bundleName);
                if (System.IO.File.Exists(persistentPath))
                {
                    return persistentPath;
                }
            }

            // 回退到 StreamingAssets
            string streamingPath = System.IO.Path.Combine(
                Application.streamingAssetsPath, streamingAssetsBundlePath, bundleName);
            return streamingPath;
        }

        #endregion
    }
}
