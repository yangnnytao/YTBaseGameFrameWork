using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 配置表注册表 —— 集中声明所有配置表类型，供 CfgToolManager 统一加载。
    /// 
    /// 设计意图：
    /// 1. 新增配置表时，只需在这里加一行 (typeof(XxxCfgTool), () => XxxCfgTool.Instance)。
    /// 2. 使用 typeof() 直接引用，IL2CPP 代码裁剪时不会误删，兼容微信小游戏。
    /// 3. CfgToolManager 遍历此注册表触发懒加载，无需硬编码逐个引用。
    /// </summary>
    public static class CfgToolRegistry
    {
        /// <summary>
        /// 注册的配置表列表（Type + 实例获取委托）。
        /// </summary>
        public static readonly (Type Type, Func<CfgToolClass> GetInstance)[] Entries = new (Type, Func<CfgToolClass>)[]
        {
            (typeof(HeroBaseCfgTool), () => HeroBaseCfgTool.Instance),
            (typeof(ItemCfgTool),    () => ItemCfgTool.Instance),

            // 后续新增配置表，只需在这里加一行：
            // (typeof(XxxCfgTool), () => XxxCfgTool.Instance),
        };
    }
}
