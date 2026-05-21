using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.LexicalAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
#if !UNITY_EDITOR
using UnityEngine.AddressableAssets;
#endif

namespace YGZFrameWork
{
    public enum ECfgToolType
    {
        Start,
        cfg_HeroBase,
        End,
    }
    /// <summary>
    /// 配置表管理器 — 标准单例，兼容旧 TableDataManager 接口
    /// </summary>
    public class CfgToolManager:Singleton<CfgToolManager>
    {
        public Dictionary<ECfgToolType, CfgToolClass> _cfgToolDic = new Dictionary<ECfgToolType, CfgToolClass>();
        /// <summary>
        /// 配置文件相对路径（基于 Application.dataPath，已包含 Assets 目录）
        /// Editor 下直接读文件，运行时使用 Addressables
        /// </summary>
        public static string mCfgDataPath = "2_Scripts/Config";

        #region 生命周期

        public void Init()
        {
            LoadAll();
        }

        public void Destroy()
        {
            ClearData();
        }

        public void ClearData()
        {
            foreach (var item in _cfgToolDic)
            {
                item.Value.Dispose();
            }
        }

        #endregion

        #region 加载

        public static void LoadAll()
        {
            var tempCfg = HeroBaseCfgTool.mInstance;
            foreach (var tempThe in tempCfg.GetAllKeys())
            {
                var tempData = tempCfg.GetCfgData(tempThe);
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
            jsonText = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(path));
#else
            // 运行时通过 Addressables 加载 TextAsset，支持热更
            var handle = Addressables.LoadAssetAsync<TextAsset>(fileName);
            var textAsset = handle.WaitForCompletion();
            if (textAsset == null)
            {
                Debug.LogError($"Addressables 加载失败: {fileName}，请检查 Addressables Group 中是否包含该配置");
                return new Dictionary<int, NewCfgData<T>>();
            }
            jsonText = textAsset.text;
            Addressables.Release(handle);
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

        public CfgToolClass NewCfgTool<T>(ECfgToolType type_, T cfgData_) where T : CfgToolClass
        {
            if (_cfgToolDic.TryGetValue(type_, out CfgToolClass outTool_))
                return outTool_;
            else
                _cfgToolDic[type_] = cfgData_;
            return _cfgToolDic[type_];
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
