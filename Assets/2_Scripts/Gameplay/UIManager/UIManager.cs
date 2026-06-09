using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace YGZFrameWork
{
    /// <summary>
    /// UI 面板管理器
    /// 负责面板的打开、关闭、栈导航、生命周期管理和预制体加载。
    /// 非 Mono Manager，继承 ManagerBase<T>，通过 ManagerOfManager 初始化。
    /// </summary>
    public class UIManager : ManagerBase<UIManager>, IManagerInterface
    {
        public static UIManager Instance => GetInstance();

        #region Fields

        /// <summary>已实例化的面板缓存：name → BasePanel（含已打开和已关闭的缓存面板）</summary>
        private Dictionary<string, BasePanel> m_PanelCache;

        /// <summary>面板栈（后进先出，记录打开顺序）</summary>
        private List<BasePanel> m_PanelStack;

        /// <summary>面板配置加载器（通过接口注入，与具体实现解耦）</summary>
        private IPanelConfigLoader m_ConfigLoader;

        /// <summary>CanvasManager 引用（通过 Facade 消息或运行时查找获取）</summary>
        private CanvasManager m_CanvasManager;

        /// <summary>当前是否有独占面板处于打开状态</summary>
        private bool m_HasExclusivePanelOpen;

        /// <summary>面板名 → 配置映射（适配现有 BasePanel，不依赖 panel.Config）</summary>
        private Dictionary<string, PanelConfigEntry> m_PanelConfigMap;

        #endregion

        #region Initialization

        public override void InitDataM()
        {
            base.InitDataM();
            m_PanelCache = new Dictionary<string, BasePanel>();
            m_PanelStack = new List<BasePanel>();
            m_PanelConfigMap = new Dictionary<string, PanelConfigEntry>();
            
            // 注入面板配置加载器（ScriptableObject 实现）
            SetConfigLoader(new PanelConfigLoader());
            
            RegisterMsg();
        }

        public override void DestroyM()
        {
            ClearAllPanels();
            base.DestroyM();
        }

        public void RegisterMsg()
        {
            // 监听框架消息（如场景切换时清空面板）
            // AppFacade.Instance.RegisterObserver(NotiConst.SCENE_CHANGED, OnSceneChanged);
        }

        public void ClearData()
        {
            ClearAllPanels();
        }

        #endregion

        #region Config Loader Injection

        /// <summary>
        /// 注入面板配置加载器
        /// 主分支只提供接口，子分支/开发者实现具体加载逻辑后注入
        /// </summary>
        public void SetConfigLoader(IPanelConfigLoader loader)
        {
            m_ConfigLoader = loader;
            m_ConfigLoader?.LoadConfigs();
        }

        /// <summary> 重新加载配置（热重载，开发调试用）</summary>
        public void ReloadConfig()
        {
            m_ConfigLoader?.Reload();
        }

        /// <summary> 按面板名获取配置（内部辅助）</summary>
        private PanelConfigEntry GetPanelConfig(string panelName)
        {
            m_PanelConfigMap.TryGetValue(panelName, out var config);
            return config;
        }

        #endregion

        #region Core API —— Open / Close

        /// <summary>
        /// 打开指定面板
        /// </summary>
        /// <param name="panelId">面板标识名（与 PanelConfig 中的 panelId 一致）</param>
        /// <param name="data">可选传入数据（会传递给 BasePanel.OnOpen）</param>
        /// <param name="onComplete">打开完成回调</param>
        public void OpenPanel(string panelId, object data = null, Action onComplete = null)
        {
            if (m_ConfigLoader == null)
            {
                Debug.LogError("[UIManager] ConfigLoader 未注入，无法打开面板");
                onComplete?.Invoke();
                return;
            }

            var config = m_ConfigLoader.GetConfig(panelId);
            if (config == null)
            {
                Debug.LogError($"[UIManager] 未找到面板配置：{panelId}，请检查配置加载器");
                onComplete?.Invoke();
                return;
            }

            // 检查是否已打开
            if (m_PanelCache.TryGetValue(panelId, out var existingPanel) && existingPanel.IsOpen)
            {
                Debug.LogWarning($"[UIManager] 面板 {panelId} 已经处于打开状态，跳过重复打开。");
                onComplete?.Invoke();
                return;
            }

            // 独占面板：关闭同层级其他面板
            if (config.isExclusive)
            {
                ClosePanelsInSameLayer(config.canvasLayer, panelId);
            }

            // 暂停下层面板
            if (config.pauseBelow)
            {
                PausePanelsBelow(config.canvasLayer);
            }

            // 获取或创建面板实例
            BasePanel panel = GetOrCreatePanel(config);
            if (panel == null)
            {
                Debug.LogError($"[UIManager] 面板实例化失败：{panelId}");
                onComplete?.Invoke();
                return;
            }

            // 挂载到对应 Canvas 层
            AttachToCanvasLayer(panel, config.canvasLayer);

            // 执行打开生命周期（动画完成后推入栈、广播事件）
            panel.OnOpen(data, () =>
            {
                // 动画完成后推入栈
                PushToStack(panel);
                Debug.Log($"[UIManager] 面板打开完成：{panelId}");
                onComplete?.Invoke();

                // 广播事件
                AppFacade.Instance.SendMessageCommand(NotiConst.UI_PANEL_OPENED, panelId);
            });
        }

        /// <summary>
        /// 关闭指定面板
        /// </summary>
        /// <param name="panelId">面板标识名</param>
        /// <param name="onComplete">关闭完成回调</param>
        public void ClosePanel(string panelId, Action onComplete = null)
        {
            if (!m_PanelCache.TryGetValue(panelId, out var panel))
            {
                Debug.LogWarning($"[UIManager] 尝试关闭未实例化的面板：{panelId}");
                onComplete?.Invoke();
                return;
            }

            if (!panel.IsOpen)
            {
                Debug.LogWarning($"[UIManager] 面板 {panelId} 已经是关闭状态。");
                onComplete?.Invoke();
                return;
            }

            // 执行关闭生命周期（动画完成后再清理）
            panel.OnClose(() =>
            {
                // 从栈中移除
                RemoveFromStack(panel);

                // 处理缓存/销毁
                HandlePanelAfterClose(panel);

                // 恢复下层面板
                var cfg = GetPanelConfig(panel.PanelId);
                if (cfg != null && cfg.pauseBelow)
                {
                    ResumePanelsBelow(cfg.canvasLayer);
                }

                Debug.Log($"[UIManager] 面板关闭完成：{panelId}");
                onComplete?.Invoke();

                // 广播事件
                AppFacade.Instance.SendMessageCommand(NotiConst.UI_PANEL_CLOSED, panelId);
            });
        }

        /// <summary>
        /// 关闭栈顶面板（用于返回按钮）
        /// </summary>
        public void CloseTopPanel(Action onComplete = null)
        {
            if (m_PanelStack.Count == 0)
            {
                Debug.Log("[UIManager] 面板栈为空，无法 CloseTopPanel。");
                onComplete?.Invoke();
                return;
            }

            var topPanel = m_PanelStack[m_PanelStack.Count - 1];
            var topConfig = GetPanelConfig(topPanel.PanelId);
            if (topConfig != null && !topConfig.handleBackButton)
            {
                // 栈顶面板不处理返回键，继续向下找
                for (int i = m_PanelStack.Count - 2; i >= 0; i--)
                {
                    var cfg = GetPanelConfig(m_PanelStack[i].PanelId);
                    if (cfg != null && cfg.handleBackButton)
                    {
                        topPanel = m_PanelStack[i];
                        break;
                    }
                }
            }

            ClosePanel(topPanel.PanelId, onComplete);
        }

        /// <summary>
        /// 清空所有面板（场景切换时调用）
        /// </summary>
        public void ClearAllPanels()
        {
            // 逆序关闭所有面板（跳过动画，直接清理）
            for (int i = m_PanelStack.Count - 1; i >= 0; i--)
            {
                var panel = m_PanelStack[i];
                if (panel.IsOpen)
                {
                    panel.Hide();
                    HandlePanelDestroy(panel);
                }
            }

            m_PanelStack.Clear();
            m_PanelCache.Clear();
            m_PanelConfigMap.Clear();

            Debug.Log("[UIManager] 所有面板已清空。");
        }

        #endregion

        #region Show / Hide (显隐不移除)

        /// <summary>
        /// 只显示面板（不移除、不触发打开动画）
        /// 适用于弹窗叠加后恢复下层面板显示
        /// </summary>
        public void ShowPanel(string panelId)
        {
            if (m_PanelCache.TryGetValue(panelId, out var panel))
            {
                panel.Show();
            }
            else
            {
                Debug.LogWarning($"[UIManager] ShowPanel 失败，面板未实例化：{panelId}");
            }
        }

        /// <summary>
        /// 只隐藏面板（不移除、不触发关闭动画）
        /// 适用于弹窗叠加时隐藏下层面板
        /// </summary>
        public void HidePanel(string panelId)
        {
            if (m_PanelCache.TryGetValue(panelId, out var panel))
            {
                panel.Hide();
            }
            else
            {
                Debug.LogWarning($"[UIManager] HidePanel 失败，面板未实例化：{panelId}");
            }
        }

        #endregion

        #region Query

        /// <summary> 获取已打开的面板实例 </summary>
        /// <param name="panelId">面板标识名</param>
        /// <returns>BasePanel 实例，未找到返回 null</returns>
        public BasePanel GetPanel(string panelId)
        {
            m_PanelCache.TryGetValue(panelId, out var panel);
            return panel;
        }

        /// <summary> 获取已打开的面板实例（泛型版本） </summary>
        public T GetPanel<T>(string panelId) where T : BasePanel
        {
            return GetPanel(panelId) as T;
        }

        /// <summary> 检查面板是否已打开 </summary>
        public bool IsPanelOpen(string panelId)
        {
            return m_PanelCache.TryGetValue(panelId, out var panel) && panel.IsOpen;
        }

        /// <summary> 获取当前栈顶面板 </summary>
        public BasePanel GetTopPanel()
        {
            if (m_PanelStack.Count == 0) return null;
            return m_PanelStack[m_PanelStack.Count - 1];
        }

        /// <summary> 获取当前打开的面板数量 </summary>
        public int OpenPanelCount => m_PanelStack.Count;

        #endregion

        #region Back Button (Android 物理返回键)

        /// <summary>
        /// 处理返回按钮（供 GameManager 或 InputManager 调用）
        /// </summary>
        /// <returns>true = 已处理，false = 无面板可关闭</returns>
        public bool HandleBackButton()
        {
            // 从栈顶开始找第一个响应返回键的面板
            for (int i = m_PanelStack.Count - 1; i >= 0; i--)
            {
                var panel = m_PanelStack[i];
                var cfg = GetPanelConfig(panel.PanelId);
                if (cfg != null && cfg.handleBackButton && panel.IsOpen)
                {
                    panel.OnBackButton();
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// 获取或创建面板实例
        /// </summary>
        private BasePanel GetOrCreatePanel(PanelConfigEntry config)
        {
            string panelId = config.panelId;

            // 尝试从缓存复用
            if (config.isCache && m_PanelCache.TryGetValue(panelId, out var cachedPanel))
            {
                if (cachedPanel != null)
                {
                    Debug.Log($"[UIManager] 复用缓存面板：{panelId}");
                    return cachedPanel;
                }
            }

            // 加载预制体（统一走 ResourceManager）
            GameObject prefab = ResourceManager.Instance.Loader.Load<GameObject>(config.prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] 预制体加载失败：{config.prefabPath}");
                return null;
            }

            // 实例化
            GameObject instance = Object.Instantiate(prefab);
            instance.name = panelId;

            // 获取 BasePanel 组件
            BasePanel panel = instance.GetComponent<BasePanel>();
            if (panel == null)
            {
                Debug.LogError($"[UIManager] 预制体缺少 BasePanel 组件：{panelId}");
                Object.Destroy(instance);
                return null;
            }

            // 初始化面板标识和配置映射（适配现有 BasePanel）
            panel.PanelId = config.panelId;
            panel.Config = config;
            m_PanelConfigMap[panelId] = config;

            // 存入缓存
            m_PanelCache[panelId] = panel;

            return panel;
        }

        /// <summary>
        /// 将面板挂载到对应 Canvas 层
        /// </summary>
        private void AttachToCanvasLayer(BasePanel panel, CanvasLayer layer)
        {
            if (m_CanvasManager == null)
            {
                m_CanvasManager = CanvasManager.Instance;
                if (m_CanvasManager == null)
                {
                    Debug.LogError("[UIManager] CanvasManager 未找到！请确保 CanvasManager 已初始化。");
                    return;
                }
            }

            // 通过 CanvasManager 获取对应层级的 Transform 并挂载（适配 GetLayer(string)）
            Transform layerRoot = m_CanvasManager.GetLayer(layer.ToString());
            if (layerRoot != null)
            {
                panel.transform.SetParent(layerRoot, false);
                ((RectTransform)panel.transform).anchoredPosition = Vector2.zero;
                ((RectTransform)panel.transform).localScale = Vector3.one;
            }
            else
            {
                Debug.LogWarning($"[UIManager] Canvas 层级 {layer} 未找到，面板将挂载到当前激活的根节点下。");
            }
        }

        /// <summary> 推入面板栈 </summary>
        private void PushToStack(BasePanel panel)
        {
            if (!m_PanelStack.Contains(panel))
            {
                m_PanelStack.Add(panel);
            }
        }

        /// <summary> 从面板栈移除 </summary>
        private void RemoveFromStack(BasePanel panel)
        {
            m_PanelStack.Remove(panel);
        }

        /// <summary>
        /// 面板关闭后的处理：缓存 or 销毁
        /// </summary>
        private void HandlePanelAfterClose(BasePanel panel)
        {
            var cfg = GetPanelConfig(panel.PanelId);
            if (cfg != null && cfg.isCache)
            {
                // 缓存模式：隐藏但不销毁
                panel.gameObject.SetActive(false);
                Debug.Log($"[UIManager] 面板 {panel.PanelId} 已缓存（隐藏未销毁）。");
            }
            else
            {
                // 非缓存模式：销毁
                HandlePanelDestroy(panel);
            }
        }

        /// <summary> 销毁面板实例 </summary>
        private void HandlePanelDestroy(BasePanel panel)
        {
            if (panel == null) return;

            string name = panel.PanelId;
            m_PanelCache.Remove(name);
            m_PanelStack.Remove(panel);
            m_PanelConfigMap.Remove(name);

            if (panel.gameObject != null)
            {
                Object.Destroy(panel.gameObject);
            }

            Debug.Log($"[UIManager] 面板已销毁：{name}");
        }

        /// <summary>
        /// 关闭同层级的其他面板（用于独占面板）
        /// </summary>
        private void ClosePanelsInSameLayer(CanvasLayer layer, string exceptPanelName)
        {
            for (int i = m_PanelStack.Count - 1; i >= 0; i--)
            {
                var panel = m_PanelStack[i];
                var cfg = GetPanelConfig(panel.PanelId);
                if (panel.PanelId != exceptPanelName && cfg != null && cfg.canvasLayer == layer)
                {
                    ClosePanel(panel.PanelId);
                }
            }
        }

        /// <summary>
        /// 暂停指定层级以下的所有面板
        /// </summary>
        private void PausePanelsBelow(CanvasLayer layer)
        {
            int targetLayer = (int)layer;
            foreach (var panel in m_PanelStack)
            {
                var cfg = GetPanelConfig(panel.PanelId);
                if (cfg != null && (int)cfg.canvasLayer < targetLayer && panel.IsOpen)
                {
                    panel.OnPause();
                }
            }
        }

        /// <summary>
        /// 恢复指定层级以下的所有面板
        /// </summary>
        private void ResumePanelsBelow(CanvasLayer layer)
        {
            int targetLayer = (int)layer;
            for (int i = m_PanelStack.Count - 1; i >= 0; i--)
            {
                var panel = m_PanelStack[i];
                var cfg = GetPanelConfig(panel.PanelId);
                if (cfg != null && (int)cfg.canvasLayer < targetLayer && panel.IsOpen)
                {
                    panel.OnResume();
                    break; // 只恢复最上面一个被暂停的
                }
            }
        }

        #endregion
    }
}
