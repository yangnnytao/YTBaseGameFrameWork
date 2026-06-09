using System;
using System.Collections.Generic;

namespace YGZFrameWork
{
    public class Controller : IController
    {
        protected IDictionary<string, Func<ICommand>> m_commandMap;
        protected IDictionary<IView, List<string>> m_viewCmdMap;

        protected static volatile IController m_instance;
        protected readonly object m_syncRoot = new object();
        protected static readonly object m_staticSyncRoot = new object();

        protected Controller()
        {
            InitializeController();
        }

        static Controller() { }

        public static IController Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (m_staticSyncRoot)
                    {
                        if (m_instance == null) m_instance = new Controller();
                    }
                }
                return m_instance;
            }
        }

        protected virtual void InitializeController()
        {
            m_commandMap = new Dictionary<string, Func<ICommand>>();
            m_viewCmdMap = new Dictionary<IView, List<string>>();
        }

        public virtual void ExecuteCommand(IMessage note)
        {
            Func<ICommand> commandFactory = null;//数据初始化
            List<IView> views = null;

            lock (m_syncRoot)//保护锁
            {
                if (m_commandMap.ContainsKey(note.Name)) //如果该字典有这个数据
                {
                    commandFactory = m_commandMap[note.Name];
                }
                else
                {
                    views = new List<IView>();
                    foreach (var de in m_viewCmdMap)
                    {
                        if (de.Key.Equals(null)) continue;
                        if (de.Value.Contains(note.Name))
                        {
                            views.Add(de.Key);
                        }
                    }
                }
            }
            if (commandFactory != null)
            {  //Controller —— 零反射：直接调用委托工厂
                ICommand commandInstance = commandFactory();
                commandInstance?.Execute(note);
            }
            if (views != null && views.Count > 0)
            {
                for (int i = 0; i < views.Count; i++)
                {
                    views[i].OnMessage(note);
                }
                views = null;
            }
        }

        /// <summary>注册命令（零反射委托工厂版本，推荐）</summary>
        public virtual void RegisterCommand(string commandName, Func<ICommand> commandFactory)
        {
            lock (m_syncRoot)
            {
                m_commandMap[commandName] = commandFactory;
            }
        }

        /// <summary>注册命令（兼容旧接口，内部包装为委托）</summary>
        public virtual void RegisterCommand(string commandName, Type commandType)
        {
            lock (m_syncRoot)
            {
                m_commandMap[commandName] = () => (ICommand)Activator.CreateInstance(commandType);
            }
        }

        public virtual void RegisterViewCommand(IView view, string[] commandNames)
        {
            lock (m_syncRoot)
            {
                if (m_viewCmdMap.ContainsKey(view))
                {
                    List<string> list = null;
                    if (m_viewCmdMap.TryGetValue(view, out list))
                    {
                        for (int i = 0; i < commandNames.Length; i++)
                        {
                            if (list.Contains(commandNames[i])) continue;
                            list.Add(commandNames[i]);
                        }
                    }
                }
                else
                {
                    m_viewCmdMap.Add(view, new List<string>(commandNames));
                }
            }
        }

        public virtual bool HasCommand(string commandName)
        {
            lock (m_syncRoot)
                return m_commandMap.ContainsKey(commandName);
        }

        public virtual void RemoveCommand(string commandName)
        {
            lock (m_syncRoot)
            {
                if (m_commandMap.ContainsKey(commandName))
                    m_commandMap.Remove(commandName);
            }
        }

        public virtual void RemoveViewCommand(IView view, string[] commandNames)
        {
            lock (m_syncRoot)
            {
                if (m_viewCmdMap.ContainsKey(view))
                {
                    List<string> list = null;
                    if (m_viewCmdMap.TryGetValue(view, out list))
                    {
                        for (int i = 0; i < commandNames.Length; i++)
                        {
                            if (!list.Contains(commandNames[i])) continue;
                            list.Remove(commandNames[i]);
                        }
                    }
                }
            }
        }
    }

}