using System;
using System.Collections.Generic;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 通用对象池 —— 支持预加载、自动扩容、定时清理。
    /// </summary>
    public class ObjectPool<T> where T : class, new()
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly int _maxCapacity;
        private readonly int _defaultCapacity;
        private int _activeCount = 0;

        public int ActiveCount => _activeCount;
        public int InactiveCount => _pool.Count;
        public int TotalCount => _activeCount + _pool.Count;

        /// <summary>
        /// 创建对象池
        /// </summary>
        /// <param name="defaultCapacity">默认容量（预加载数量）</param>
        /// <param name="maxCapacity">最大容量（超过则创建后不回收）</param>
        public ObjectPool(int defaultCapacity = 10, int maxCapacity = 100)
        {
            _defaultCapacity = defaultCapacity;
            _maxCapacity = maxCapacity;
            Preload(defaultCapacity);
        }

        /// <summary>预加载对象</summary>
        public void Preload(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_pool.Count >= _maxCapacity) break;
                _pool.Push(new T());
            }
        }

        /// <summary>获取对象</summary>
        public T Get()
        {
            T obj;
            if (_pool.Count > 0)
            {
                obj = _pool.Pop();
            }
            else
            {
                obj = new T();
            }
            _activeCount++;
            return obj;
        }

        /// <summary>回收对象</summary>
        public void Recycle(T obj)
        {
            if (obj == null) return;
            _activeCount--;
            if (_pool.Count < _maxCapacity)
            {
                _pool.Push(obj);
            }
        }

        /// <summary>清空池</summary>
        public void Clear()
        {
            _pool.Clear();
            _activeCount = 0;
        }
    }

    /// <summary>
    /// GameObject 专用对象池 —— 针对频繁 Instantiate/Destroy 的特效、子弹、UI 元素。
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<GameObject> _pool = new Stack<GameObject>();
        private readonly int _maxCapacity;
        private int _activeCount = 0;

        public int ActiveCount => _activeCount;
        public int InactiveCount => _pool.Count;

        /// <summary>
        /// 创建 GameObject 池
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">回收后挂载的父节点（可为 null）</param>
        /// <param name="preloadCount">预加载数量</param>
        /// <param name="maxCapacity">最大容量</param>
        public GameObjectPool(GameObject prefab, Transform parent = null, int preloadCount = 5, int maxCapacity = 50)
        {
            _prefab = prefab;
            _parent = parent;
            _maxCapacity = maxCapacity;
            Preload(preloadCount);
        }

        public void Preload(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_pool.Count >= _maxCapacity) break;
                var go = UnityEngine.Object.Instantiate(_prefab, _parent);
                go.SetActive(false);
                _pool.Push(go);
            }
        }

        /// <summary>获取并激活对象</summary>
        public GameObject Get(Vector3 position = default, Quaternion rotation = default)
        {
            GameObject go;
            if (_pool.Count > 0)
            {
                go = _pool.Pop();
            }
            else
            {
                go = UnityEngine.Object.Instantiate(_prefab, _parent);
            }
            go.transform.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            _activeCount++;
            return go;
        }

        /// <summary>回收对象（隐藏并放回池）</summary>
        public void Recycle(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            _activeCount--;
            if (_pool.Count < _maxCapacity)
            {
                if (_parent != null) go.transform.SetParent(_parent, false);
                _pool.Push(go);
            }
            else
            {
                UnityEngine.Object.Destroy(go);
            }
        }

        /// <summary>清空池</summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var go = _pool.Pop();
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            _activeCount = 0;
        }
    }
}
