using System;
using System.Collections.Generic;
using UnityEngine;
using YGZFrameWork;

/// <summary>
/// 管理器协调者 —— 负责按依赖顺序统一初始化和逆序销毁所有管理器。
/// 
/// 设计说明：
/// 1. 完全由 ManagerRegistry 注册表驱动，零硬编码管理器引用。
/// 2. 新增管理器时，只需在 ManagerRegistry.Entries 中加一行，无需改动此类。
/// 3. 初始化顺序由 Priority 显式控制，销毁时自动逆序。
/// 4. 所有管理器统一通过 IManagerInterface 接口调用生命周期方法。
/// 5. 使用 typeof() + 直接实例引用，IL2CPP / 微信小游戏完全兼容，零反射。
/// </summary>
public class ManagerOfManager : Singleton<ManagerOfManager>
{
    /// <summary>按初始化顺序保存的实例引用，用于逆序销毁</summary>
    private readonly List<object> _initOrder = new List<object>();

    public override void InitDataM()
    {
        _initOrder.Clear();

        var entries = ManagerRegistry.GetInitOrder();
        foreach (var entry in entries)
        {
            object instance = null;
            try
            {
                instance = entry.GetInstance();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManagerOfManager] 获取管理器实例失败：{entry.Type.Name}，异常：{ex.Message}");
                continue;
            }

            if (instance == null)
            {
                Debug.LogError($"[ManagerOfManager] 无法获取管理器实例：{entry.Type.Name}");
                continue;
            }

            if (instance is IManagerInterface mgr)
            {
                mgr.InitDataM();
            }
            else
            {
                Debug.LogWarning($"[ManagerOfManager] {entry.Type.Name} 未实现 IManagerInterface，跳过 InitDataM。");
            }

            _initOrder.Add(instance);
        }
    }

    public override void DestroyM()
    {
        // 逆序销毁：后初始化的先销毁，解决依赖问题
        for (int i = _initOrder.Count - 1; i >= 0; i--)
        {
            var instance = _initOrder[i];
            if (instance == null) continue;

            if (instance is IManagerInterface mgr)
            {
                try
                {
                    mgr.DestroyM();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ManagerOfManager] 销毁管理器异常：{instance.GetType().Name}，{ex.Message}");
                }
            }
        }

        _initOrder.Clear();
    }
}
