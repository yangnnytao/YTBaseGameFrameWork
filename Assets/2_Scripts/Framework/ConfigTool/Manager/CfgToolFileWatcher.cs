using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 配置文件监听器 —— Editor 下自动检测 JSON 文件变化并重新加载配置表。
    /// 基于 EditorApplication.update 轮询文件修改时间戳，轻量无依赖。
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    public static class CfgToolFileWatcher
    {
        private static readonly Dictionary<string, DateTime> _fileTimestamps = new Dictionary<string, DateTime>();
        private static bool _initialized = false;
        private static float _checkInterval = 2f; // 检查间隔（秒）
        private static float _lastCheckTime = 0f;

#if UNITY_EDITOR
        static CfgToolFileWatcher()
        {
            Init();
        }
#endif

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            RefreshTimestamps();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += OnEditorUpdate;
            Debug.Log("[CfgToolFileWatcher] 配置文件监听已启动");
#endif
        }

#if UNITY_EDITOR
        private static void OnEditorUpdate()
        {
            _lastCheckTime += Time.deltaTime;
            if (_lastCheckTime < _checkInterval) return;
            _lastCheckTime = 0f;

            CheckForChanges();
        }
#endif

        /// <summary>
        /// 刷新所有配置文件的时间戳记录
        /// </summary>
        public static void RefreshTimestamps()
        {
            _fileTimestamps.Clear();
            var dir = Path.Combine(Application.dataPath, CfgToolManager.mCfgDataPath);
            if (!Directory.Exists(dir)) return;

            var files = Directory.GetFiles(dir, "*.json");
            foreach (var file in files)
            {
                _fileTimestamps[file] = File.GetLastWriteTime(file);
            }
        }

        /// <summary>
        /// 检查文件变化，如有变化则自动重新加载
        /// </summary>
        public static void CheckForChanges()
        {
            var dir = Path.Combine(Application.dataPath, CfgToolManager.mCfgDataPath);
            if (!Directory.Exists(dir)) return;

            var files = Directory.GetFiles(dir, "*.json");
            bool changed = false;
            var newTimestamps = new Dictionary<string, DateTime>();

            foreach (var file in files)
            {
                var currentTime = File.GetLastWriteTime(file);
                newTimestamps[file] = currentTime;

                if (_fileTimestamps.TryGetValue(file, out var lastTime))
                {
                    if (currentTime > lastTime)
                    {
                        changed = true;
                        Debug.Log($"[CfgToolFileWatcher] 配置文件变化: {Path.GetFileName(file)}");
                    }
                }
                else
                {
                    changed = true;
                    Debug.Log($"[CfgToolFileWatcher] 新增配置文件: {Path.GetFileName(file)}");
                }
            }

            // 检测被删除的文件
            foreach (var key in _fileTimestamps.Keys)
            {
                if (!newTimestamps.ContainsKey(key))
                {
                    changed = true;
                    Debug.Log($"[CfgToolFileWatcher] 配置文件删除: {Path.GetFileName(key)}");
                }
            }

            _fileTimestamps.Clear();
            foreach (var kvp in newTimestamps)
            {
                _fileTimestamps[kvp.Key] = kvp.Value;
            }

            if (changed)
            {
                Debug.Log("[CfgToolFileWatcher] 检测到配置变化，自动重新加载...");
                CfgToolManager.LoadAll();
            }
        }
    }
}
