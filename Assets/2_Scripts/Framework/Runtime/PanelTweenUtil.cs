using System;
using System.Collections;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 面板 Tween 动画工具 —— 基于 Coroutine 的轻量插值动画。
    /// 不依赖任何第三方 Tween 库，IL2CPP / 微信小游戏完全兼容。
    /// </summary>
    public static class PanelTweenUtil
    {
        /// <summary>缓动函数委托</summary>
        public delegate float EaseFunction(float t);

        /// <summary>线性缓动</summary>
        public static float Linear(float t) => t;

        /// <summary> easeInOutQuad </summary>
        public static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
        }

        /// <summary> easeOutBack </summary>
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>
        /// 执行 Tween 动画（通用）
        /// </summary>
        /// <param name="mono">用于启动 Coroutine 的 MonoBehaviour</param>
        /// <param name="duration">动画时长（秒）</param>
        /// <param name="onUpdate">每帧回调（参数 t 为 0~1 的进度）</param>
        /// <param name="onComplete">完成回调</param>
        /// <param name="ease">缓动函数，默认 EaseInOutQuad</param>
        public static Coroutine Tween(MonoBehaviour mono, float duration, Action<float> onUpdate, Action onComplete = null, EaseFunction ease = null)
        {
            if (mono == null || !mono.gameObject.activeInHierarchy)
            {
                onComplete?.Invoke();
                return null;
            }
            return mono.StartCoroutine(TweenCoroutine(duration, onUpdate, onComplete, ease ?? EaseInOutQuad));
        }

        private static IEnumerator TweenCoroutine(float duration, Action<float> onUpdate, Action onComplete, EaseFunction ease)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                onUpdate?.Invoke(ease(t));
                yield return null;
            }
            onUpdate?.Invoke(1f);
            onComplete?.Invoke();
        }

        #region 面板专用 Tween 动画

        /// <summary>淡入动画（CanvasGroup.alpha 0→1）</summary>
        public static Coroutine FadeIn(MonoBehaviour mono, CanvasGroup cg, float duration, Action onComplete = null)
        {
            if (cg == null)
            {
                onComplete?.Invoke();
                return null;
            }
            cg.alpha = 0f;
            return Tween(mono, duration, t => cg.alpha = t, onComplete);
        }

        /// <summary>淡出动画（CanvasGroup.alpha 1→0）</summary>
        public static Coroutine FadeOut(MonoBehaviour mono, CanvasGroup cg, float duration, Action onComplete = null)
        {
            if (cg == null)
            {
                onComplete?.Invoke();
                return null;
            }
            return Tween(mono, duration, t => cg.alpha = 1f - t, onComplete);
        }

        /// <summary>缩放弹入（localScale 0→1）</summary>
        public static Coroutine ScaleIn(MonoBehaviour mono, Transform target, float duration, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return null;
            }
            target.localScale = Vector3.zero;
            return Tween(mono, duration, t => target.localScale = Vector3.one * EaseOutBack(t), onComplete);
        }

        /// <summary>缩放弹出（localScale 1→0）</summary>
        public static Coroutine ScaleOut(MonoBehaviour mono, Transform target, float duration, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return null;
            }
            return Tween(mono, duration, t => target.localScale = Vector3.one * (1f - t), onComplete);
        }

        /// <summary>滑入（从屏幕外滑入到目标位置）</summary>
        public static Coroutine SlideIn(MonoBehaviour mono, RectTransform target, Vector2 from, float duration, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return null;
            }
            Vector2 to = target.anchoredPosition;
            target.anchoredPosition = from;
            return Tween(mono, duration, t => target.anchoredPosition = Vector2.Lerp(from, to, EaseInOutQuad(t)), onComplete);
        }

        /// <summary>滑出（从当前位置滑出到屏幕外）</summary>
        public static Coroutine SlideOut(MonoBehaviour mono, RectTransform target, Vector2 to, float duration, Action onComplete = null)
        {
            if (target == null)
            {
                onComplete?.Invoke();
                return null;
            }
            Vector2 from = target.anchoredPosition;
            return Tween(mono, duration, t => target.anchoredPosition = Vector2.Lerp(from, to, EaseInOutQuad(t)), onComplete);
        }

        #endregion
    }
}
