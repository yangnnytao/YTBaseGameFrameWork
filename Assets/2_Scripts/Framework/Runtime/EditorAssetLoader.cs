using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YGZFrameWork
{
    /// <summary>
    /// 编辑器资源加载器 —— 编辑器模式下直接从项目目录 `4_GameAssets/` 加载原始资源。
    /// 
    /// 设计说明：
    /// 1. 仅编辑器可用（#if UNITY_EDITOR），真机编译时此文件不参与编译。
    /// 2. 资源路径与 AB 包保持一致（如 "Prefabs/GUI/UI/DiceTestPanel"）。
    /// 3. 实际文件路径 = Assets/4_GameAssets/ + 资源路径 + 扩展名。
    /// 4. 支持多扩展名自动降级（如 .png 失败则尝试 .jpg、.tga）。
    /// 5. 异步加载在编辑器下用同步模拟（AssetDatabase 同步加载已足够快）。
    /// 
    /// 使用场景：
    /// - 编辑器开发时直接修改 4_GameAssets 下的资源，无需打包 AB 即可实时预览。
    /// - 与 AssetBundleLoader 共享同一套资源路径，上层代码零改动切换。
    /// </summary>
    public class EditorAssetLoader : IResourceLoader
    {
        public static readonly EditorAssetLoader Instance = new EditorAssetLoader();

        /// <summary>编辑器资源根目录（相对于 Assets/）</summary>
        public string EditorAssetRoot { get; set; } = "4_GameAssets";

        #region 同步加载

        public T Load<T>(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[EditorAssetLoader] 路径为空");
                return null;
            }

            // 1. 尝试从 4_GameAssets 加载（开发资源）
            string primaryPath = ResourcePathUtil.ToEditorFullPath(path, EditorAssetRoot, typeof(T));
            T asset = AssetDatabase.LoadAssetAtPath<T>(primaryPath);

            if (asset == null)
            {
                string[] candidatePaths = ResourcePathUtil.ToEditorFullPathCandidates(path, EditorAssetRoot, typeof(T));
                foreach (string candidatePath in candidatePaths)
                {
                    asset = AssetDatabase.LoadAssetAtPath<T>(candidatePath);
                    if (asset != null) break;
                }
            }

            if (asset == null)
            {
                string fallbackPath = $"Assets/{EditorAssetRoot}/{path}";
                asset = AssetDatabase.LoadAssetAtPath<T>(fallbackPath);
            }

            // 2. 4_GameAssets 中找不到，回退到 Resources 加载（首包资源、配置表等）
            if (asset == null)
            {
                asset = Resources.Load<T>(path);
                if (asset != null)
                {
                    Debug.Log($"[EditorAssetLoader] 从 Resources 回退加载成功: {path}");
                }
            }

            if (asset == null)
            {
                Debug.LogError($"[EditorAssetLoader] 加载失败（已尝试 4_GameAssets 和 Resources）: {path}");
            }
            return asset;
#else
            Debug.LogError("[EditorAssetLoader] 仅在编辑器模式下可用");
            return null;
#endif
        }

        public GameObject LoadPrefab(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate)
        {
            var prefab = Load<GameObject>(path, unloadType);
            if (prefab == null) return null;
            return UnityEngine.Object.Instantiate(prefab);
        }

        #endregion

        #region 异步加载

        public void LoadAsync<T>(string path, Action<T> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            // 编辑器下同步加载已足够快，直接同步回调
            T asset = Load<T>(path, unloadType);
            onComplete?.Invoke(asset);
#else
            onComplete?.Invoke(null);
#endif
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

        #endregion

        #region 资源释放与预加载

        public void Release(UnityEngine.Object asset)
        {
            // 编辑器直接加载的资源无需显式释放
        }

        public void Preload<T>(string path) where T : UnityEngine.Object
        {
            _ = Load<T>(path);
        }

        #endregion
    }
}
