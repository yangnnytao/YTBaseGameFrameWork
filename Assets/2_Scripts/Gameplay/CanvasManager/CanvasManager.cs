using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YGZFrameWork;

/// <summary> Canvas 宽高比适配配置 </summary>
[System.Serializable]
public class CanvasScalerConfig
{
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    [Range(0f, 1f)]
    public float matchWidthOrHeight = 0.5f;
    public CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
}

/// <summary> 子层级配置 </summary>
[System.Serializable]
public class SubLayerConfig
{
    public string subLayerName;
    [Tooltip("相对于主层级 baseSortingOrder 的偏移量")]
    public int sortOrderOffset = 1;
}

/// <summary> 主层级配置 </summary>
[System.Serializable]
public class CanvasLayerConfig
{
    public string layerName;
    public int baseSortingOrder;
    public RenderMode renderMode = RenderMode.ScreenSpaceOverlay;
    [Tooltip("为空则使用全局适配配置")]
    public bool overrideScalerConfig;
    public CanvasScalerConfig scalerConfig;
    public SubLayerConfig[] subLayers;
}

/// <summary> Canvas 层级管理器 </summary>
public class CanvasManager : ManagerMono<CanvasManager>
{
    [Header("全局适配配置")]
    [SerializeField] private CanvasScalerConfig globalScalerConfig = new CanvasScalerConfig();

    [Header("层级配置")]
    [SerializeField] private CanvasLayerConfig[] layerConfigs = new CanvasLayerConfig[]
    {
        new CanvasLayerConfig { layerName = "Background", baseSortingOrder = 0 },
        new CanvasLayerConfig { layerName = "Main",       baseSortingOrder = 100 },
        new CanvasLayerConfig { layerName = "Popup",      baseSortingOrder = 200 },
        new CanvasLayerConfig { layerName = "Top",        baseSortingOrder = 300 },
    };

    [Header("次级页面配置")]
    [SerializeField]private SubLayerConfig[] subLayerConfig = new SubLayerConfig[] { 
        new SubLayerConfig{ subLayerName = "BaseLayer", sortOrderOffset = 0},
        new SubLayerConfig{ subLayerName = "SecondLayer", sortOrderOffset = 30},
        new SubLayerConfig{ subLayerName = "ThirdLayer", sortOrderOffset = 60},
    };

    private readonly Dictionary<string, RectTransform> _layers = new Dictionary<string, RectTransform>();
    private readonly Dictionary<string, Canvas> _layerCanvases = new Dictionary<string, Canvas>();
    private readonly Dictionary<string, Dictionary<string, RectTransform>> _subLayers =
        new Dictionary<string, Dictionary<string, RectTransform>>();

    private Transform _mainParent = null;

    protected override void Awake()
    {
        base.Awake();
        AppFacade.Instance.AddManager(ManagerName.Canvas, this);
    }

    public override void InitDataM()
    {
        base.InitDataM();
        BuildLayers();
    }

    public override void DestroyM()
    {
        base.DestroyM();
        ClearLayers();
    }

    #region 层级构建

    private void BuildLayers()
    {
        foreach (var cfg in layerConfigs)
        {
            cfg.subLayers = subLayerConfig;
            CreateMainLayer(cfg);
        }
    }

    private void CreateMainLayer(CanvasLayerConfig cfg)
    {
        if (_layers.ContainsKey(cfg.layerName)) return;

        var go = new GameObject(cfg.layerName);
        go.transform.SetParent(Instance.transform, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = cfg.renderMode;
        canvas.sortingOrder = cfg.baseSortingOrder;

        var scalerCfg = cfg.overrideScalerConfig ? cfg.scalerConfig : globalScalerConfig;
        ApplyScalerConfig(go, scalerCfg);

        go.AddComponent<GraphicRaycaster>();

        var rt = go.GetComponent<RectTransform>();
        _layers[cfg.layerName] = rt;
        _layerCanvases[cfg.layerName] = canvas;
        _subLayers[cfg.layerName] = new Dictionary<string, RectTransform>();

        if (cfg.subLayers != null)
        {
            foreach (var sub in cfg.subLayers)
            {
                CreateSubLayerInternal(cfg.layerName, sub.subLayerName,
                    cfg.baseSortingOrder + sub.sortOrderOffset);
            }
        }
    }

    private RectTransform CreateSubLayerInternal(string layerName, string subLayerName, int sortingOrder)
    {
        if (!_layers.TryGetValue(layerName, out var parentRt))
        {
            Debug.LogError($"[CanvasManager] 主层级 '{layerName}' 不存在");
            return null;
        }

        var subs = _subLayers[layerName];
        if (subs.ContainsKey(subLayerName))
        {
            Debug.LogWarning($"[CanvasManager] 子层级 '{layerName}/{subLayerName}' 已存在");
            return subs[subLayerName];
        }

        var go = new GameObject(subLayerName);
        go.transform.SetParent(parentRt, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        go.AddComponent<GraphicRaycaster>();

        var rt = go.GetComponent<RectTransform>();
        StretchRectTransform(rt);

        subs[subLayerName] = rt;
        return rt;
    }

    private void ApplyScalerConfig(GameObject go, CanvasScalerConfig cfg)
    {
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = cfg.referenceResolution;
        scaler.screenMatchMode = cfg.screenMatchMode;
        scaler.matchWidthOrHeight = cfg.matchWidthOrHeight;
    }

    private void StretchRectTransform(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void ClearLayers()
    {
        foreach (var kvp in _layers)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        _layers.Clear();
        _layerCanvases.Clear();
        _subLayers.Clear();
    }

    #endregion

    #region 公开 API

    /// <summary> 获取主层级 RectTransform </summary>
    public RectTransform GetLayer(string layerName)
    {
        _layers.TryGetValue(layerName, out var rt);
        return rt;
    }

    /// <summary> 获取主层级 Canvas </summary>
    public Canvas GetLayerCanvas(string layerName)
    {
        _layerCanvases.TryGetValue(layerName, out var canvas);
        return canvas;
    }

    /// <summary> 获取子层级 RectTransform </summary>
    public RectTransform GetSubLayer(string layerName, string subLayerName)
    {
        if (_subLayers.TryGetValue(layerName, out var subs))
        {
            subs.TryGetValue(subLayerName, out var rt);
            return rt;
        }
        return null;
    }

    /// <summary> 运行时动态添加子层级 </summary>
    /// <param name="sortOffset">相对于主层级 baseSortingOrder 的偏移</param>
    public RectTransform AddSubLayer(string layerName, string subLayerName, int sortOffset = 1)
    {
        if (!_layerCanvases.TryGetValue(layerName, out var parentCanvas))
        {
            Debug.LogError($"[CanvasManager] 主层级 '{layerName}' 不存在");
            return null;
        }
        int sortingOrder = parentCanvas.sortingOrder + sortOffset;
        return CreateSubLayerInternal(layerName, subLayerName, sortingOrder);
    }

    /// <summary> 运行时移除子层级 </summary>
    public void RemoveSubLayer(string layerName, string subLayerName)
    {
        if (!_subLayers.TryGetValue(layerName, out var subs)) return;
        if (!subs.TryGetValue(subLayerName, out var rt)) return;

        subs.Remove(subLayerName);
        if (rt != null) Destroy(rt.gameObject);
    }

    /// <summary> 运行时动态添加主层级 </summary>
    public RectTransform AddLayer(CanvasLayerConfig config)
    {
        if (_layers.ContainsKey(config.layerName))
        {
            Debug.LogWarning($"[CanvasManager] 主层级 '{config.layerName}' 已存在");
            return _layers[config.layerName];
        }
        CreateMainLayer(config);
        return _layers[config.layerName];
    }

    /// <summary> 运行时移除主层级（同时移除其所有子层级） </summary>
    public void RemoveLayer(string layerName)
    {
        if (!_layers.TryGetValue(layerName, out var rt)) return;

        if (rt != null) Destroy(rt.gameObject);
        _layers.Remove(layerName);
        _layerCanvases.Remove(layerName);
        _subLayers.Remove(layerName);
    }

    /// <summary> 动态修改全局适配配置并刷新所有层级 </summary>
    public void SetGlobalScalerConfig(CanvasScalerConfig config)
    {
        globalScalerConfig = config;
        RefreshAllScalers();
    }

    /// <summary> 刷新所有主层级的 CanvasScaler（用于运行时切换适配策略） </summary>
    private void RefreshAllScalers()
    {
        foreach (var cfg in layerConfigs)
        {
            if (cfg.overrideScalerConfig) continue;
            if (!_layers.TryGetValue(cfg.layerName, out var rt)) continue;

            var scaler = rt.GetComponent<CanvasScaler>();
            if (scaler == null) continue;

            scaler.referenceResolution = globalScalerConfig.referenceResolution;
            scaler.screenMatchMode = globalScalerConfig.screenMatchMode;
            scaler.matchWidthOrHeight = globalScalerConfig.matchWidthOrHeight;
        }
    }

    #endregion
}
