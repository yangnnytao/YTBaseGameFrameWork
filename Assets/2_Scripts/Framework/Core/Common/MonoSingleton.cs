using UnityEngine;
using System;

namespace YGZFrameWork
{
    /// <summary>
    /// MonoBehaviour 单例基类
    /// 提供线程安全的 Mono 单例实现，自动创建 GameObject 并标记为 DontDestroyOnLoad。
    /// 子类 Awake 中必须调用 base.Awake() 以确保生命周期正确。
    /// </summary>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static readonly object _lock = new object();
        private static volatile T _instance;
        private static bool _applicationIsQuitting = false;

        /// <summary>静态实例属性（线程安全）</summary>
        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[MonoSingleton] {typeof(T).Name} 实例在应用退出后被访问，返回 null。");
                    return null;
                }

                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // 先查找场景中是否已有实例（兼容 Facade/AddManager 创建方式）
                            T[] instances = FindObjectsOfType<T>();
                            if (instances.Length > 0)
                            {
                                _instance = instances[0];
                                for (int i = 1; i < instances.Length; i++)
                                {
                                    Debug.LogWarning($"[MonoSingleton] 发现重复的 {typeof(T).Name} 实例，已销毁。");
                                    Destroy(instances[i].gameObject);
                                }
                            }
                            else
                            {
                                GameObject go = new GameObject(typeof(T).Name);
                                _instance = go.AddComponent<T>();
                                DontDestroyOnLoad(go);
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[MonoSingleton] {typeof(T).Name} 已存在实例，销毁当前重复对象。");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _applicationIsQuitting = true;
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
    }
}
