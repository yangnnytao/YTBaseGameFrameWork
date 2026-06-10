using System;
using System.IO;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 资源路径工具类 —— 统一处理资源路径解析、扩展名推断、AB 包名映射。
    /// </summary>
    public static class ResourcePathUtil
    {
        #region 扩展名相关

        /// <summary>
        /// 常见资源类型的扩展名列表（按优先级排序）。
        /// </summary>
        public static readonly string[] PrefabExtensions = { ".prefab" };
        public static readonly string[] SpriteExtensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };
        public static readonly string[] AudioExtensions = { ".mp3", ".wav", ".ogg", ".aiff" };
        public static readonly string[] TextExtensions = { ".json", ".txt", ".xml", ".csv", ".bytes" };
        public static readonly string[] MaterialExtensions = { ".mat" };
        public static readonly string[] AnimationExtensions = { ".anim", ".controller" };
        public static readonly string[] ShaderExtensions = { ".shader", ".shadervariants" };
        public static readonly string[] FontExtensions = { ".ttf", ".otf" };
        public static readonly string[] SceneExtensions = { ".unity" };

        /// <summary>
        /// 根据目标类型获取候选扩展名列表。
        /// </summary>
        public static string[] GetCandidateExtensions(Type targetType)
        {
            if (targetType == typeof(GameObject))
                return PrefabExtensions;
            if (targetType == typeof(Texture2D) || targetType == typeof(Sprite))
                return SpriteExtensions;
            if (targetType == typeof(Material))
                return MaterialExtensions;
            if (targetType == typeof(AnimationClip) || targetType == typeof(UnityEngine.RuntimeAnimatorController))
                return AnimationExtensions;
            if (targetType == typeof(AudioClip))
                return AudioExtensions;
            if (targetType == typeof(Shader) || targetType == typeof(ShaderVariantCollection))
                return ShaderExtensions;
            if (targetType == typeof(TextAsset))
                return TextExtensions;
            if (targetType == typeof(UnityEngine.SceneManagement.Scene) || targetType.Name.Contains("Scene"))
                return SceneExtensions;
            if (targetType == typeof(Font))
                return FontExtensions;

            // ScriptableObject 类型（如 PanelConfigData、AssetBundleConfig）
            if (typeof(ScriptableObject).IsAssignableFrom(targetType))
                return new[] { ".asset" };

            // 默认尝试所有常见扩展名
            return new[] { "" };
        }

        /// <summary>
        /// 根据目标类型获取首选扩展名。
        /// </summary>
        public static string GetPrimaryExtension(Type targetType)
        {
            var exts = GetCandidateExtensions(targetType);
            return exts.Length > 0 ? exts[0] : "";
        }

        #endregion

        #region 路径解析

        /// <summary>
        /// 将上层统一资源路径解析为编辑器完整路径。
        /// </summary>
        /// <param name="assetPath">统一资源路径（如 "Prefabs/GUI/UI/DiceTestPanel"）</param>
        /// <param name="editorAssetRoot">编辑器资源根目录（如 "4_GameAssets"）</param>
        /// <param name="targetType">目标资源类型</param>
        /// <returns>完整 AssetDatabase 路径</returns>
        public static string ToEditorFullPath(string assetPath, string editorAssetRoot, Type targetType)
        {
            string ext = GetPrimaryExtension(targetType);
            return $"Assets/{editorAssetRoot}/{assetPath}{ext}";
        }

        /// <summary>
        /// 将上层统一资源路径解析为编辑器完整路径（带候选扩展名列表）。
        /// </summary>
        public static string[] ToEditorFullPathCandidates(string assetPath, string editorAssetRoot, Type targetType)
        {
            var exts = GetCandidateExtensions(targetType);
            var paths = new string[exts.Length];
            for (int i = 0; i < exts.Length; i++)
            {
                paths[i] = $"Assets/{editorAssetRoot}/{assetPath}{exts[i]}";
            }
            return paths;
        }

        /// <summary>
        /// 从资源路径中提取 AB 包名（基于命名约定）。
        /// 例如："UI/MainMenu" → "ui.bundle"
        /// </summary>
        public static string GetBundleNameFromPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            // 取第一级目录作为包名前缀
            int slashIndex = assetPath.IndexOf('/');
            string prefix = slashIndex > 0 ? assetPath.Substring(0, slashIndex).ToLower() : assetPath.ToLower();
            return $"{prefix}.bundle";
        }

        /// <summary>
        /// 从资源路径中提取资源名（最后一级）。
        /// 例如："UI/MainMenu" → "MainMenu"
        /// </summary>
        public static string GetAssetNameFromPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return string.Empty;

            int lastSlash = assetPath.LastIndexOf('/');
            return lastSlash >= 0 ? assetPath.Substring(lastSlash + 1) : assetPath;
        }

        /// <summary>
        /// 规范化资源路径（统一分隔符、去除首尾斜杠）。
        /// </summary>
        public static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            return path.Replace('\\', '/').TrimStart('/').TrimEnd('/');
        }

        #endregion

        #region 海外资源适配

        /// <summary>
        /// 根据语言设置获取本地化资源路径。
        /// 例如："UI/MainMenu" + "_CN" → "UI/MainMenu_CN"
        /// </summary>
        public static string GetLocalizedPath(string basePath, string languageSuffix)
        {
            if (string.IsNullOrEmpty(languageSuffix))
                return basePath;
            return $"{basePath}{languageSuffix}";
        }

        /// <summary>
        /// 尝试获取带语言后缀的路径，如果不存在则回退到基础路径。
        /// </summary>
        public static string ResolveLocalizedPath(string basePath, string languageSuffix, Func<string, bool> existsChecker)
        {
            if (!string.IsNullOrEmpty(languageSuffix))
            {
                string localizedPath = GetLocalizedPath(basePath, languageSuffix);
                if (existsChecker(localizedPath))
                    return localizedPath;
            }
            return basePath;
        }

        #endregion
    }
}
