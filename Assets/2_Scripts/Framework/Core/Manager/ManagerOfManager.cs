using System;
using System.Collections.Generic;
using UnityEngine;
using YGZFrameWork;

/// <summary>
/// 管理器协调者 —— 负责按依赖顺序统一初始化和逆序销毁所有管理器。
/// 
/// 设计说明：
/// 1. 采用显式调用（Xxx.Instance.InitDataM()），IL2CPP / 微信小游戏完全兼容，零反射。
/// 2. 新增管理器时，在此类中按依赖顺序添加一行即可。
/// 3. 初始化顺序 = 代码书写顺序（越靠前越早初始化），销毁时自动逆序。
/// 4. 如需调整顺序，直接移动代码行位置即可。
/// </summary>
public class ManagerOfManager : Singleton<ManagerOfManager>
{
    /// <summary>按初始化顺序保存的实例引用，用于逆序销毁</summary>
    private readonly List<object> _initOrder = new List<object>();

    public override void InitDataM()
    {
        _initOrder.Clear();

        // ========== 基础设施层 —— 最先初始化 ==========

        // 1. 配置表管理器（所有系统依赖配置数据）
        CfgToolManager.Instance.InitDataM();
        _initOrder.Add(CfgToolManager.Instance);

        // 2. Canvas 层级管理器（UIManager 依赖它挂载面板）
        CanvasManager.Instance.InitDataM();
        _initOrder.Add(CanvasManager.Instance);

        // 3. UI 面板管理器（依赖 CanvasManager 的层级）
        UIManager.Instance.InitDataM();
        _initOrder.Add(UIManager.Instance);

        // ========== 业务逻辑层 ==========

        // 4. 时间管理器
        TimeManager.Instance.InitDataM();
        _initOrder.Add(TimeManager.Instance);

        // 5. 网络管理器
        NetWorkManager.Instance.InitDataM();
        _initOrder.Add(NetWorkManager.Instance);

        // 6. 骰子管理器（示例业务管理器）
        DiceManager.Instance.InitDataM();
        _initOrder.Add(DiceManager.Instance);

        // ========== 后续新增管理器，按依赖顺序在此添加 ==========
        // 示例：
        // GameDataManager.Instance.InitDataM();
        // _initOrder.Add(GameDataManager.Instance);

        // 启动首屏（保留原有行为）
        UIManager.Instance?.OpenPanel("DiceTestPanel");
    }

    public override void DestroyM()
    {
        // 逆序销毁：后初始化的先销毁，解决依赖问题
        for (int i = _initOrder.Count - 1; i >= 0; i--)
        {
            var instance = _initOrder[i];
            if (instance == null) continue;

            switch (instance)
            {
                case DiceManager mgr: mgr.DestroyM(); break;
                case NetWorkManager mgr: mgr.DestroyM(); break;
                case TimeManager mgr: mgr.DestroyM(); break;
                case UIManager mgr: mgr.DestroyM(); break;
                case CanvasManager mgr: mgr.DestroyM(); break;
                case CfgToolManager mgr: mgr.DestroyM(); break;
                default:
                    Debug.LogWarning($"[ManagerOfManager] 未知管理器类型 {instance.GetType().Name}，跳过销毁。");
                    break;
            }
        }

        _initOrder.Clear();
    }
}
