using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 配置表管理器 — 标准单例，兼容旧 TableDataManager 接口
    /// 
    /// 设计说明：
    /// 1. 配置表字典以 Type 为键，彻底移除 ECfgToolType 枚举硬编码。
    /// 2. 新增配置表时，只需在 CfgToolRegistry 中加一行，无需改动此类。
    /// 3. 所有配置表通过 CfgToolBase 构造时自动注册到本管理器。
    /// 4. 使用 typeof() 直接引用，IL2CPP / 微信小游戏完全兼容。
    /// </summary>
    public class CfgToolManager : Singleton<CfgToolManager>, IManagerInterface
    {
        public static CfgToolManager Instance => GetInstance();

        /// <summary>配置表字典：Type → CfgToolClass（以配置表 Tool 类型为键）</summary>
        public Dictionary<Type, CfgToolClass> _cfgToolDic = new Dictionary<Type, CfgToolClass>();

        /// <summary>
        /// 配置文件相对路径（基于 Application.dataPath，已包含 Assets 目录）
        /// Editor 下直接读文件，运行时使用 Addressables
        /// </summary>
        public static string mCfgDataPath = "4_GameAssets/CfgData";

        #region 生命周期（统一为框架标准命名）

        public override void InitDataM()
        {
            LoadAll();
        }

        public override void DestroyM()
        {
            ClearData();
        }

        public void RegisterMsg()
        {
            // 当前无消息需要注册
        }

        public void ClearData()
        {
            foreach (var item in _cfgToolDic)
            {
                item.Value.Dispose();
            }
            _cfgToolDic.Clear();
        }

        #endregion

        #region 注册与获取

        /// <summary>
        /// 注册配置表（由 CfgToolBase 构造时自动调用）
        /// </summary>
        public void RegisterCfgTool(CfgToolClass tool)
        {
            if (tool == null) return;
            var type = tool.GetType();
            if (!_cfgToolDic.ContainsKey(type))
                _cfgToolDic[type] = tool;
        }

        /// <summary>
        /// 泛型获取配置表实例
        /// </summary>
        public T GetCfgTool<T>() where T : CfgToolClass
        {
            var type = typeof(T);
            if (_cfgToolDic.TryGetValue(type, out var tool))
                return tool as T;
            return null;
        }

        /// <summary>
        /// 按类型获取配置表实例（非泛型版本）
        /// </summary>
        public CfgToolClass GetCfgTool(Type type)
        {
            if (type == null) return null;
            if (_cfgToolDic.TryGetValue(type, out var tool))
                return tool;
            return null;
        }

        #endregion

        #region 加载

        public static void LoadAll()
        {
            // 遍历注册表触发懒加载（IL2CPP 防裁剪 + 统一入口）
            foreach (var entry in CfgToolRegistry.Entries)
            {
                try
                {
                    var instance = entry.GetInstance();
                    UnityEngine.Debug.Log("[CfgTool] Loaded: " + entry.Type.Name);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[CfgToolManager] 加载配置表失败: {entry.Type.Name}, {ex.Message}");
                }
            }
        }

        public T Load<T>(string fileName) where T : class, new()
        {
            string jsonText = null;

#if UNITY_EDITOR
            // Editor 下直接读取文件，方便策划实时修改
            var path = Path.Combine(Application.dataPath, mCfgDataPath, fileName + ".json");
            if (!File.Exists(path))
            {
                Debug.LogError($"配置文件不存在: {path}");
                return default;
            }
            using (var reader = new StreamReader(path, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                jsonText = reader.ReadToEnd();
            }
#else
            // 运行时通过 ResourceManager 统一加载 TextAsset，支持热更
            var textAsset = ResourceManager.Instance.Loader.Load<TextAsset>(fileName);
            if (textAsset == null)
            {
                Debug.LogError($"配置加载失败: {fileName}，请检查 Addressables Group 或 Resources 目录中是否包含该配置");
                return default;
            }
            jsonText = textAsset.text;
            ResourceManager.Instance.Loader.Release(textAsset);
#endif

            if (string.IsNullOrEmpty(jsonText))
            {
                Debug.LogError($"配置文件内容为空: {fileName}");
                return default;
            }

            try
            {
                var jsonList = JsonConvert.DeserializeObject<T>(jsonText);
                if (jsonList != null)
                {
                    return jsonList;
                }
                return default;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载配置文件失败 {fileName}: {e.Message}");
                return default;
            }
        }

        #endregion
    }

#if UNITY_EDITOR
    /// <summary>
    /// 配置表工具 Editor 扩展
    /// </summary>
    public static class NewCfgToolEditor
    {
        [UnityEditor.MenuItem("Tools/CfgTool/Generate Sample JSON")]
        public static void GenerateSampleJSON()
        {
            var dir = Path.Combine(Application.dataPath, CfgToolManager.mCfgDataPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // HeroBaseCfgData.json 示例
            var heroPath = Path.Combine(dir, "HeroBaseCfgData.json");
            var heroJson = @"[
  {
    ""id"": 1,
    ""name"": ""亚瑟"",
    ""job"": ""骑士"",
    ""isHuman"": true,
    ""skills"": [1001, 1002],
    ""baseHp"": 100,
    ""baseAtk"": 20,
    ""baseDef"": 10,
    ""baseSpd"": 5
  },
  {
    ""id"": 2,
    ""name"": ""伊莎贝拉"",
    ""job"": ""圣骑士"",
    ""isHuman"": true,
    ""skills"": [2001, 2002],
    ""baseHp"": 120,
    ""baseAtk"": 15,
    ""baseDef"": 15,
    ""baseSpd"": 4
  }
]";
            File.WriteAllText(heroPath, heroJson, System.Text.Encoding.UTF8);
            Debug.Log($"[CfgTool] 已生成示例配置: {heroPath}");
            UnityEditor.AssetDatabase.Refresh();
        }

        [UnityEditor.MenuItem("Tools/CfgTool/Reload Configs")]
        public static void ReloadConfigs()
        {
            CfgToolManager.LoadAll();
            Debug.Log("[CfgTool] 配置表已重新加载");
        }
    }
#endif
}
