using UnityEngine;
using YGZFrameWork;

public class ManagerMono<T> : MonoSingleton<T>, IManagerInterface where T : ManagerMono<T>
{
    /// <summary> 内置事件 </summary>
    protected EventDispatcher eventDispatcher;

    protected override void Awake()
    {
        base.Awake();
        if (eventDispatcher == null)
        {
            eventDispatcher = new EventDispatcher();
        }
    }

    public virtual void InitDataM()
    {
        if (eventDispatcher == null)
        {
            eventDispatcher = new EventDispatcher();
        }
        RegisterMsg();
    }

    public virtual void DestroyM()
    {
        ClearAllEvents();
        eventDispatcher?.ClearEvent();
        eventDispatcher = null;
    }

    public virtual void RegisterMsg()
    {

    }

    public virtual void ClearData()
    {

    }

    #region 事件相关

    /// <summary> 添加事件监听 </summary>
    /// <param name="id">事件ID</param>
    /// <param name="handler">事件委托</param>
    /// <param name="save">是否是永久事件</param>
    protected void AddEventListener(int id, EventDispatcher.eventHandler handler, bool save = false)
    {
        if (eventDispatcher == null)
        {
            Debug.LogWarning($"[ManagerMono<{typeof(T).Name}>] eventDispatcher 未初始化，尝试重新初始化。");
            eventDispatcher = new EventDispatcher();
        }
        eventDispatcher?.AddEvent(id, handler, save);
    }

    /// <summary> 发送事件 </summary>
    /// <param name="id"> 事件ID </param>
    /// <param name="objs"> 相关参数 </param>
    protected void DispatchEvent(int id, params object[] objs)
    {
        eventDispatcher?.DoEvent(id, objs);
    }

    /// <summary> 删除该ID所有事件 </summary>
    /// <param name="id"> 事件ID </param>
    protected void RemoveEventListener(int id)
    {
        eventDispatcher?.RemoveEvent(id);
    }

    /// <summary> 删除指定事件 </summary>
    /// <param name="id"> 事件ID </param>
    /// <param name="handler">  </param>
    protected void RemoveEventListener(int id, EventDispatcher.eventHandler handler)
    {
        eventDispatcher?.RemoveEvent(id, handler);
    }

    /// <summary> 清空所有事件 </summary>
    protected void ClearAllEvents()
    {
        eventDispatcher?.ClearEvent();
    }

    #endregion 事件相关_end
}
