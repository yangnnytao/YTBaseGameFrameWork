using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace YGZFrameWork
{

    public abstract class CfgToolClass
    {
        protected abstract string mTableName { get; }

        protected CfgToolManager _tableHanle = null;

        // 静态注册表：所有配置表实例统一登记，供 CfgToolManager 遍历
        private static readonly List<CfgToolClass> _registry = new List<CfgToolClass>();
        public static IReadOnlyList<CfgToolClass> AllRegistered => _registry;

        public CfgToolClass():base()
        {
            _tableHanle = CfgToolManager.Instance;
        }

        /// <summary>
        /// 子类在首次创建实例时调用，自动加入注册表
        /// </summary>
        protected static void Register(CfgToolClass tool)
        {
            if (tool != null && !_registry.Contains(tool))
                _registry.Add(tool);
        }

        public virtual void Dispose()
        {
        }
    }

    public abstract class CfgBase<TKeyType>
    {
        public TKeyType id;
    }

    public abstract class CfgToolBase<TKeyType, TCfgClass> : CfgToolClass where TCfgClass : CfgBase<TKeyType>
    {
        protected Dictionary<TKeyType, TCfgClass> _cfgDataDic = null;

        public CfgToolBase() : base() {
            List<TCfgClass> luaDict = _tableHanle.Load<List<TCfgClass>>(mTableName);
            _cfgDataDic = new Dictionary<TKeyType, TCfgClass>(luaDict.Count);
            for (int i = 0; i < luaDict.Count; i++)
            {
                _cfgDataDic[luaDict[i].id] = luaDict[i];
            }
        }

        public override void Dispose()
        {
            _cfgDataDic?.Clear();
            _cfgDataDic = null;
            base.Dispose();
        }

        public TCfgClass GetCfgData(TKeyType cfgID_)
        {
            if (_cfgDataDic == null)//没有配置表
            {
                throw new KeyNotFoundException($"Cfg tool type mismatch, type = {typeof(TCfgClass).Name}");
            }

            if (_cfgDataDic.TryGetValue(cfgID_, out TCfgClass cacheValue))//已有缓存
            {
                return cacheValue;
            }
            return null;
        }

        public IEnumerable<TKeyType> GetAllKeys()
        {
            if (_cfgDataDic == null)
            {
                throw new KeyNotFoundException($"Cfg tool type mismatch, type = {typeof(TCfgClass).Name}");
            }
            return _cfgDataDic.Keys;
        }
    }
}
