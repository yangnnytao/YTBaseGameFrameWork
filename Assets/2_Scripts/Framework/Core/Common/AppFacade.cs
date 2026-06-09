using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace YGZFrameWork
{
    public class AppFacade : Facade
    {
        private static readonly object _lock = new object();
        private static volatile AppFacade _instance;

        public static AppFacade Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AppFacade();
                        }
                    }
                }
                return _instance;
            }
        }

        public AppFacade() : base()
        {
        }

        override protected void InitFramework()
        {
            base.InitFramework();
            //注册启动框架事件（零反射：使用委托工厂）
            RegisterCommand(NotiConst.START_UP, () => new StartUpCommand());
        }

        /// <summary> 启动框架 </summary>
        public void StartUp()
        {
            SendMessageCommand(NotiConst.START_UP);
            RemoveMultiCommand(NotiConst.START_UP);
        }
    }
}
