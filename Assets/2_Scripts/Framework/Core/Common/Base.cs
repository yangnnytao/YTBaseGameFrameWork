using UnityEngine;
using System.Collections.Generic;

namespace YGZFrameWork
{
    public class Base : MonoBehaviour
    {
        private AppFacade m_Facade;//游戏的外观模式

        /// <summary>
        /// 注册消息
        /// </summary>
        /// <param name="view"></param>
        /// <param name="messages"></param>
        protected void RegisterMessage(IView view, List<string> messages)
        {
            if (messages == null || messages.Count == 0) return;
            Facade.RegisterObserver(view, messages.ToArray());
        }

        /// <summary>
        /// 移除消息
        /// </summary>
        /// <param name="view">视图</param>
        /// <param name="messages">消息列表</param>
        protected void RemoveMessage(IView view, List<string> messages)
        {
            if (messages == null || messages.Count == 0) return;
            Facade.RemoveObserver(view, messages.ToArray());
        }

        protected AppFacade Facade
        {
            get
            {
                if (m_Facade == null)
                {
                    m_Facade = AppFacade.Instance;
                }
                return m_Facade;
            }
        }

    }
}