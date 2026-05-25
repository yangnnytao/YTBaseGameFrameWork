using System.Collections.Generic;
using UnityEngine;
using YGZFrameWork;

/// <summary>
/// UI 面板管理器
/// 负责面板栈管理、Open/Close/Show/Hide/ClearAll、配置表驱动。
/// 配合 CanvasManager 的层级系统挂载面板到对应 Canvas 层。
/// </summary>
public class UIManager : ManagerBase<UIManager>, IManagerInterface
{

    // 面板栈（从底到顶）
    private readonly List<BasePanel> _panelStack = new List<BasePanel>();

    // 缓存的面板（key = panelId）
    private readonly Dictionary<string, BasePanel> _cachedPanels = new Dictionary<string, BasePanel>();

    // 配置表映射
    private Dictionary<string, PanelConfigData> _configMap;

    public override void InitDataM()
    {
        base.InitDataM();
        RegisterMsg();
        LoadConfig();
    }

    public override void DestroyM()
    {
        ClearData();
        base.DestroyM();
    }

    public void RegisterMsg()
    {
        AddEventListener((int)UIMessage.OpenPanel, OnMsgOpenPanel);
        AddEventListener((int)UIMessage.ClosePanel, OnMsgClosePanel);
        AddEventListener((int)UIMessage.CloseTopPanel, OnMsgCloseTopPanel);
        AddEventListener((int)UIMessage.ClearAllPanels, OnMsgClearAllPanels);
    }

    public void ClearData()
    {
        ClearAllEvents();
        ClearAllPanels();
        _configMap?.Clear();
        _cachedPanels.Clear();
        _panelStack.Clear();
    }

    #region 配置加载

    private void LoadConfig()
    {
        _configMap = new Dictionary<string, PanelConfigData>();
        // TODO: 从 JSON/ScriptableObject 加载配置
        // 示例：var configs = Resources.LoadAll<PanelConfigData>("Config/PanelConfig");
        var configs = Resources.Load<PanelConfigData>("Config/DiceTestPanelConfig");
        _configMap[configs.panelId] = Resources.Load<PanelConfigData>("Config/DiceTestPanelConfig");
    }

    public void SetConfigMap(Dictionary<string, PanelConfigData> map)
    {
        _configMap = map ?? new Dictionary<string, PanelConfigData>();
    }

    #endregion

    #region 公开 API

    /// <summary>打开指定面板</summary>
    public BasePanel OpenPanel(string panelId, object param = null)
    {
        if (!_configMap.TryGetValue(panelId, out var cfg))
        {
            Debug.LogError($"[UIManager] 找不到面板配置: {panelId}");
            return null;
        }

        // 如果面板已打开且是独占的，忽略
        var existing = FindInStack(panelId);
        if (existing != null)
        {
            if (cfg.isExclusive)
            {
                Debug.LogWarning($"[UIManager] 独占面板 {panelId} 已存在");
                return existing;
            }
            existing.Show();
            return existing;
        }

        // 独占面板：关闭同层级其他面板
        if (cfg.isExclusive)
        {
            ClosePanelsInLayer(cfg.canvasLayer);
        }

        // 实例化面板
        BasePanel panel;
        if (cfg.isCache && _cachedPanels.TryGetValue(panelId, out panel))
        {
            panel.gameObject.SetActive(true);
        }
        else
        {
            panel = InstantiatePanel(cfg);
            if (cfg.isCache)
            {
                _cachedPanels[panelId] = panel;
            }
        }

        if (panel == null) return null;

        // 压入栈
        _panelStack.Add(panel);
        panel.OnOpen(param);

        // 广播
        DispatchEvent((int)UIMessage.PanelOpened, panelId);
        AppFacade.Instance.SendMessageCommand(NotiConst.UI_PANEL_OPENED, panelId);

        return panel;
    }

    /// <summary>关闭指定面板</summary>
    public void ClosePanel(string panelId)
    {
        var panel = FindInStack(panelId);
        if (panel == null) return;

        RemoveFromStack(panel);
        panel.OnClose();

        if (!_cachedPanels.ContainsKey(panelId))
        {
            GameObject.Destroy(panel.gameObject);
        }
        else
        {
            panel.gameObject.SetActive(false);
        }

        DispatchEvent((int)UIMessage.PanelClosed, panelId);
        AppFacade.Instance.SendMessageCommand(NotiConst.UI_PANEL_CLOSED, panelId);
    }

    /// <summary>关闭栈顶面板（返回键用）</summary>
    public void CloseTopPanel()
    {
        if (_panelStack.Count == 0) return;
        var top = _panelStack[_panelStack.Count - 1];
        ClosePanel(top.PanelId);
    }

    /// <summary>关闭所有面板</summary>
    public void ClearAllPanels()
    {
        for (int i = _panelStack.Count - 1; i >= 0; i--)
        {
            var panel = _panelStack[i];
            panel.OnClose();
            if (!_cachedPanels.ContainsKey(panel.PanelId))
            {
                UnityEngine.GameObject.Destroy(panel.gameObject);
            }
            else
            {
                panel.gameObject.SetActive(false);
            }
        }
        _panelStack.Clear();
    }

    /// <summary>获取当前栈顶面板</summary>
    public BasePanel GetTopPanel()
    {
        if (_panelStack.Count == 0) return null;
        return _panelStack[_panelStack.Count - 1];
    }

    /// <summary>面板是否已打开</summary>
    public bool IsPanelOpen(string panelId)
    {
        return FindInStack(panelId) != null;
    }

    #endregion

    #region 内部方法

    private BasePanel InstantiatePanel(PanelConfigData cfg)
    {
        if (cfg.panelPrefab == null)
        {
            Debug.LogError($"[UIManager] 面板预制体为空: {cfg.panelId}");
            return null;
        }

        var canvasMgr = AppFacade.Instance.GetManager<CanvasManager>(ManagerName.Canvas);
        if (canvasMgr == null)
        {
            Debug.LogError("[UIManager] CanvasManager 未初始化");
            return null;
        }

        var parent = canvasMgr.GetLayer(cfg.canvasLayer);
        if (parent == null)
        {
            Debug.LogError($"[UIManager] Canvas 层级不存在: {cfg.canvasLayer}");
            return null;
        }

        var go = GameObject.Instantiate(cfg.panelPrefab, parent);
        var panel = go.GetComponent<BasePanel>();
        if (panel == null)
        {
            panel = go.AddComponent<BasePanel>();
        }
        panel.PanelId = cfg.panelId;
        panel.Config = cfg;

        return panel;
    }

    private BasePanel FindInStack(string panelId)
    {
        for (int i = 0; i < _panelStack.Count; i++)
        {
            if (_panelStack[i].PanelId == panelId)
                return _panelStack[i];
        }
        return null;
    }

    private void RemoveFromStack(BasePanel panel)
    {
        _panelStack.Remove(panel);
    }

    private void ClosePanelsInLayer(string layerName)
    {
        for (int i = _panelStack.Count - 1; i >= 0; i--)
        {
            if (_panelStack[i].Config.canvasLayer == layerName)
            {
                var panel = _panelStack[i];
                _panelStack.RemoveAt(i);
                panel.OnClose();
                if (!_cachedPanels.ContainsKey(panel.PanelId))
                {
                    GameObject.Destroy(panel.gameObject);
                }
                else
                {
                    panel.gameObject.SetActive(false);
                }
            }
        }
    }

    #endregion

    #region 消息回调

    private void OnMsgOpenPanel(params object[] objs)
    {
        if (objs.Length < 1) return;
        string panelId = objs[0].ToString();
        object param = objs.Length > 1 ? objs[1] : null;
        OpenPanel(panelId, param);
    }

    private void OnMsgClosePanel(params object[] objs)
    {
        if (objs.Length < 1) return;
        ClosePanel(objs[0].ToString());
    }

    private void OnMsgCloseTopPanel(params object[] objs)
    {
        CloseTopPanel();
    }

    private void OnMsgClearAllPanels(params object[] objs)
    {
        ClearAllPanels();
    }

    #endregion
}
