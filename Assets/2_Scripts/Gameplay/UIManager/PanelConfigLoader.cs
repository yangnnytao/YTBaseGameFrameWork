using System.Collections.Generic;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 面板配置加载器 —— ScriptableObject 实现
    /// 从 Resources/Config/PanelConfig.asset 加载所有面板配置。
    /// 
    /// 使用说明：
    /// 1. 在 Unity 中右键 Create → YGZFrameWork/PanelConfig 创建配置资产
    /// 2. 将资产放入 Assets/Resources/Config/ 目录，命名为 PanelConfig.asset
    /// 3. 在 panels 数组中添加面板条目（panelId 必须与代码中 OpenPanel 参数一致）
    /// </summary>
    public class PanelConfigLoader : IPanelConfigLoader
    {
        private Dictionary<string, PanelConfigEntry> _configMap;
        private PanelConfigData _configAsset;

        /// <summary>配置资产在 Resources 下的路径（不含 Resources/ 前缀，不含 .asset 后缀）</summary>
        public const string ConfigAssetPath = "Config/PanelConfig";

        public void LoadConfigs()
        {
            _configMap = new Dictionary<string, PanelConfigEntry>();

            _configAsset = ResourceManager.Instance.Loader.Load<PanelConfigData>(ConfigAssetPath);
            if (_configAsset == null)
            {
                Debug.LogError($"[PanelConfigLoader] 未找到配置资产：Resources/{ConfigAssetPath}.asset。请右键 Create → YGZFrameWork/PanelConfig 创建并放入 Resources/Config/ 目录。");
                return;
            }

            if (_configAsset.panels == null || _configAsset.panels.Length == 0)
            {
                Debug.LogWarning($"[PanelConfigLoader] PanelConfig.asset 中 panels 数组为空，请添加面板配置。");
                return;
            }

            foreach (var entry in _configAsset.panels)
            {
                if (entry == null) continue;
                if (string.IsNullOrEmpty(entry.panelId))
                {
                    Debug.LogWarning($"[PanelConfigLoader] 发现 panelId 为空的配置条目，已跳过。");
                    continue;
                }

                if (_configMap.ContainsKey(entry.panelId))
                {
                    Debug.LogWarning($"[PanelConfigLoader] 发现重复的 panelId：{entry.panelId}，已覆盖。");
                }

                _configMap[entry.panelId] = entry;
            }

            Debug.Log($"[PanelConfigLoader] 配置加载完成，共 { _configMap.Count } 个面板。");
        }

        public PanelConfigEntry GetConfig(string panelId)
        {
            if (_configMap == null)
            {
                Debug.LogWarning($"[PanelConfigLoader] 配置未加载，尝试自动加载。");
                LoadConfigs();
            }

            if (_configMap != null && _configMap.TryGetValue(panelId, out var config))
                return config;

            return null;
        }

        public Dictionary<string, PanelConfigEntry> GetAllConfigs()
        {
            if (_configMap == null)
            {
                LoadConfigs();
            }

            if (_configMap == null)
                return new Dictionary<string, PanelConfigEntry>();

            return new Dictionary<string, PanelConfigEntry>(_configMap);
        }

        public void Reload()
        {
            Debug.Log("[PanelConfigLoader] 重新加载配置...");
            _configMap = null;
            _configAsset = null;
            LoadConfigs();
        }
    }
}
