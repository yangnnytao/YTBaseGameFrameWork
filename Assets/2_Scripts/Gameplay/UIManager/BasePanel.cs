using System;
using UnityEngine;
using YGZFrameWork;

/// <summary>
/// UI 面板基类
/// 所有 UI 面板的基类，带生命周期和统一动画接口（Animator + Tween 双模式）。
/// 继承 MonoBehaviour，由 UIManager 统一调度。
/// </summary>
public abstract class BasePanel : Base, IView
{
    [HideInInspector] public string PanelId;
    [HideInInspector] public PanelConfigEntry Config;

    // 动画组件缓存
    private Animator _animator;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;

    // 当前正在播放的 Tween Coroutine（用于中断）
    private Coroutine _currentTween;

    public bool IsOpen => gameObject != null && gameObject.activeSelf;

    protected virtual void Awake()
    {
        _animator = GetComponent<Animator>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
    }

    #region 生命周期（由 UIManager 调用）

    /// <summary>面板打开时调用（动画完成后执行 onComplete）</summary>
    public virtual void OnOpen(object param, Action onComplete = null)
    {
        gameObject.SetActive(true);
        PlayOpenAnimation(onComplete);
    }

    /// <summary>面板关闭时调用（动画完成后执行 onComplete）</summary>
    public virtual void OnClose(Action onComplete = null)
    {
        PlayCloseAnimation(() =>
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
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

    /// <summary>显示面板（不触发 OnOpen 动画）</summary>
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

    /// <summary>隐藏面板（不触发 OnClose 动画）</summary>
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

    #region 统一动画接口（Animator + Tween 双模式）

    /// <summary>播放打开动画（根据 Config.animMode 自动选择实现）</summary>
    public virtual void PlayOpenAnimation(Action onComplete = null)
    {
        StopCurrentTween();

        if (Config == null)
        {
            onComplete?.Invoke();
            return;
        }

        switch (Config.animMode)
        {
            case PanelAnimMode.Animator:
                PlayAnimatorOpen(onComplete);
                break;
            case PanelAnimMode.Tween:
                PlayTweenOpen(onComplete);
                break;
            default:
                onComplete?.Invoke();
                break;
        }
    }

    /// <summary>播放关闭动画（根据 Config.animMode 自动选择实现）</summary>
    public virtual void PlayCloseAnimation(Action onComplete = null)
    {
        StopCurrentTween();

        if (Config == null)
        {
            onComplete?.Invoke();
            return;
        }

        switch (Config.animMode)
        {
            case PanelAnimMode.Animator:
                PlayAnimatorClose(onComplete);
                break;
            case PanelAnimMode.Tween:
                PlayTweenClose(onComplete);
                break;
            default:
                onComplete?.Invoke();
                break;
        }
    }

    #endregion

    #region Animator 动画实现

    private void PlayAnimatorOpen(Action onComplete)
    {
        if (_animator != null && !string.IsNullOrEmpty(Config.openAnimationTrigger))
        {
            _animator.SetTrigger(Config.openAnimationTrigger);
            // Animator 动画时长不可控，使用配置时长或默认延迟
            float delay = Config.tweenDuration > 0f ? Config.tweenDuration : 0.3f;
            StartCoroutine(WaitDelay(delay, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private void PlayAnimatorClose(Action onComplete)
    {
        if (_animator != null && !string.IsNullOrEmpty(Config.closeAnimationTrigger))
        {
            _animator.SetTrigger(Config.closeAnimationTrigger);
            float delay = Config.tweenDuration > 0f ? Config.tweenDuration : 0.3f;
            StartCoroutine(WaitDelay(delay, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private System.Collections.IEnumerator WaitDelay(float delay, Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
    }

    #endregion

    #region Tween 动画实现

    private void PlayTweenOpen(Action onComplete)
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        float duration = Config.tweenDuration > 0f ? Config.tweenDuration : 0.3f;

        switch (Config.tweenAnimType)
        {
            case PanelAnimType.Fade:
                _currentTween = PanelTweenUtil.FadeIn(this, _canvasGroup, duration, onComplete);
                break;
            case PanelAnimType.Scale:
                _currentTween = PanelTweenUtil.ScaleIn(this, transform, duration, onComplete);
                break;
            case PanelAnimType.SlideLeft:
                _currentTween = PanelTweenUtil.SlideIn(this, _rectTransform, new Vector2(-Screen.width, 0), duration, onComplete);
                break;
            case PanelAnimType.SlideRight:
                _currentTween = PanelTweenUtil.SlideIn(this, _rectTransform, new Vector2(Screen.width, 0), duration, onComplete);
                break;
            case PanelAnimType.SlideUp:
                _currentTween = PanelTweenUtil.SlideIn(this, _rectTransform, new Vector2(0, -Screen.height), duration, onComplete);
                break;
            case PanelAnimType.SlideDown:
                _currentTween = PanelTweenUtil.SlideIn(this, _rectTransform, new Vector2(0, Screen.height), duration, onComplete);
                break;
            default:
                onComplete?.Invoke();
                break;
        }
    }

    private void PlayTweenClose(Action onComplete)
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
        }

        float duration = Config.tweenDuration > 0f ? Config.tweenDuration : 0.3f;

        switch (Config.tweenAnimType)
        {
            case PanelAnimType.Fade:
                _currentTween = PanelTweenUtil.FadeOut(this, _canvasGroup, duration, onComplete);
                break;
            case PanelAnimType.Scale:
                _currentTween = PanelTweenUtil.ScaleOut(this, transform, duration, onComplete);
                break;
            case PanelAnimType.SlideLeft:
                _currentTween = PanelTweenUtil.SlideOut(this, _rectTransform, new Vector2(-Screen.width, 0), duration, onComplete);
                break;
            case PanelAnimType.SlideRight:
                _currentTween = PanelTweenUtil.SlideOut(this, _rectTransform, new Vector2(Screen.width, 0), duration, onComplete);
                break;
            case PanelAnimType.SlideUp:
                _currentTween = PanelTweenUtil.SlideOut(this, _rectTransform, new Vector2(0, Screen.height), duration, onComplete);
                break;
            case PanelAnimType.SlideDown:
                _currentTween = PanelTweenUtil.SlideOut(this, _rectTransform, new Vector2(0, -Screen.height), duration, onComplete);
                break;
            default:
                onComplete?.Invoke();
                break;
        }
    }

    private void StopCurrentTween()
    {
        if (_currentTween != null)
        {
            StopCoroutine(_currentTween);
            _currentTween = null;
        }
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
