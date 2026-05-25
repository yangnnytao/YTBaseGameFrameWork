using System;
using UnityEngine;
using YGZFrameWork;

/// <summary>
/// UI 面板基类
/// 所有 UI 面板的基类，带生命周期和动画接口。
/// 继承 MonoBehaviour，由 UIManager 统一调度。
/// </summary>
public abstract class BasePanel : Base, IView
{
    [HideInInspector] public string PanelId;
    [HideInInspector] public PanelConfigData Config;

    // 动画组件缓存
    private Animator _animator;
    private CanvasGroup _canvasGroup;

    public bool IsOpen => gameObject.activeSelf;

    protected virtual void Awake()
    {
        _animator = GetComponent<Animator>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    #region 生命周期（由 UIManager 调用）

    /// <summary>面板打开时调用</summary>
    public virtual void OnOpen(object param)
    {
        gameObject.SetActive(true);
        PlayOpenAnimation();
    }

    /// <summary>面板关闭时调用</summary>
    public virtual void OnClose()
    {
        PlayCloseAnimation(() =>
        {
            gameObject.SetActive(false);
        });
    }

    /// <summary>面板被暂停（上层有全屏面板覆盖）</summary>
    public virtual void OnPause()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>面板恢复（上层全屏面板关闭）</summary>
    public virtual void OnResume()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>返回键/返回按钮触发</summary>
    public virtual void OnBackButton()
    {
        UIManager.Instance.ClosePanel(PanelId);
    }

    #endregion

    #region 显示/隐藏

    /// <summary>显示面板（不触发 OnOpen）</summary>
    public virtual void Show()
    {
        gameObject.SetActive(true);
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>隐藏面板（不触发 OnClose）</summary>
    public virtual void Hide()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region 动画

    /// <summary>播放打开动画</summary>
    protected virtual void PlayOpenAnimation(Action onComplete = null)
    {
        if (_animator != null && Config != null)
        {
            _animator.SetTrigger(Config.openAnimationTrigger);
        }
        onComplete?.Invoke();
    }

    /// <summary>播放关闭动画</summary>
    protected virtual void PlayCloseAnimation(Action onComplete = null)
    {
        if (_animator != null && Config != null)
        {
            _animator.SetTrigger(Config.closeAnimationTrigger);
        }
        onComplete?.Invoke();
    }

    #endregion

    #region 便捷方法

    /// <summary>关闭自己</summary>
    protected void CloseSelf()
    {
        if (!string.IsNullOrEmpty(PanelId))
        {
            UIManager.Instance.ClosePanel(PanelId);
        }
    }

    /// <summary>打开另一个面板</summary>
    protected void OpenPanel(string panelId, object param = null)
    {
        UIManager.Instance.OpenPanel(panelId, param);
    }

    public abstract void OnMessage(IMessage message);

    #endregion
}
