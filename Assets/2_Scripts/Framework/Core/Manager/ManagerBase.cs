using UnityEngine;

namespace YGZFrameWork
{
	/// <summary> 管理器基类(非Mono) </summary>
	public class ManagerBase<T> : Singleton<T>, IManagerInterface where T : ManagerBase<T>, new()
    {
		/// <summary> 内置事件 </summary>
		protected EventDispatcher eventDispatcher;

		/// <summary> 构建 </summary>
		public ManagerBase()
		{
			eventDispatcher = new EventDispatcher();
		}

        #region 事件相关

        /// <summary> 清空数据 </summary>
        public override void DestroyM()
		{
			// 清空事件数据
			eventDispatcher?.ClearEvent();
		}

		/// <summary> 添加事件监听 </summary>
		/// <param name="id">事件ID</param>
		/// <param name="handler">事件委托</param>
		/// <param name="save">是否是永久事件</param>
		protected void AddEventListener(int id, EventDispatcher.eventHandler handler, bool save = false)
		{
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
	
		public virtual void RegisterMsg() { }
		public virtual void ClearData() { }
	}


}