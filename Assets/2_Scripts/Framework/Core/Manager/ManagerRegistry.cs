using System;
using System.Collections.Generic;
using UnityEngine;
using YGZFrameWork;

namespace YGZFrameWork
{
    /// <summary>
    /// 管理器注册表 —— 集中声明所有需要由 ManagerOfManager 统一初始化和销毁的管理器。
    /// 
    /// 设计意图：
    /// 1. 新增管理器时，只需在这里加一行 (typeof(XxxManager), Priority, () => XxxManager.Instance)，无需改动 ManagerOfManager.cs。
    /// 2. 初始化顺序由 Priority 显式控制（越小越早初始化）。
    /// 3. 销毁时自动按初始化顺序的逆序执行，解决依赖销毁问题。
    /// 4. 使用 typeof() + 直接实例引用，IL2CPP 代码裁剪时不会误删，兼容微信小游戏。
    /// </summary>
    public static class ManagerRegistry
    {
        /// <summary>
        /// 注册的管理器列表（Type + 初始化优先级 + 实例获取委托）。
        /// Priority 越小越早初始化，销毁时自动逆序。
        /// </summary>
        public static readonly (Type Type, int Priority, Func<object> GetInstance)[] Entries = new (Type, int, Func<object>)[]
        {
            // 基础设施层 —— 最先初始化
            (typeof(CfgToolManager),  1, () => (object)CfgToolManager.Instance),   // 配置表加载，所有系统依赖
            (typeof(CanvasManager),   2, () => (object)CanvasManager.Instance),    // Canvas 层级构建，UIManager 依赖它
            (typeof(UIManager),       3, () => (object)UIManager.Instance),      // UI 面板系统，依赖 CanvasManager

            // 业务逻辑层
            (typeof(TimeManager),      4, () => (object)TimeManager.Instance),     // 时间同步
            (typeof(NetWorkManager),   5, () => (object)NetWorkManager.Instance), // 网络请求
            (typeof(DiceManager),      6, () => (object)DiceManager.Instance),    // 骰子游戏逻辑

            // 后续新增管理器，按依赖关系插入合适位置即可：
            // (typeof(GameDataManager),  7, () => (object)GameDataManager.Instance),
            // (typeof(AudioManager),     8, () => (object)AudioManager.Instance),
        };

        /// <summary>按 Priority 排序后的只读列表（缓存，避免每次重新排序）</summary>
        private static readonly (Type Type, int Priority, Func<object> GetInstance)[] _sortedEntries;

        static ManagerRegistry()
        {
            // 编译时校验：确保没有重复 Priority 导致顺序歧义
            var priorities = new HashSet<int>();
            foreach (var (_, priority, _) in Entries)
            {
                if (!priorities.Add(priority))
                {
                    Debug.LogError($"[ManagerRegistry] 检测到重复的 Priority = {priority}，请确保每个管理器的优先级唯一。");
                }
            }

            // 按 Priority 升序排序（初始化顺序）
            var list = new List<(Type, int, Func<object>)>(Entries);
            list.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            _sortedEntries = list.ToArray();
        }

        /// <summary>获取按初始化顺序排列的注册表条目</summary>
        public static IReadOnlyList<(Type Type, int Priority, Func<object> GetInstance)> GetInitOrder()
        {
            return _sortedEntries;
        }

        /// <summary>获取按销毁顺序排列的注册表条目（初始化顺序的逆序）</summary>
        public static IReadOnlyList<(Type Type, int Priority, Func<object> GetInstance)> GetDestroyOrder()
        {
            // 逆序遍历，无需额外数组
            var result = new List<(Type, int, Func<object>)>(_sortedEntries.Length);
            for (int i = _sortedEntries.Length - 1; i >= 0; i--)
            {
                result.Add(_sortedEntries[i]);
            }
            return result;
        }
    }
}
