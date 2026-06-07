using UnityEngine;
using System;
using System.Collections;
using YGZFrameWork;

public class DiceRollAnimation : MonoBehaviour
{
    public enum AnimationMode { Model3D = 0, Sprite2D = 1 }

    [Header("动画配置")]
    [SerializeField] private AnimationMode m_AnimationMode = AnimationMode.Model3D;
    [SerializeField] private float m_AnimationDuration = 1.2f;

    [Header("3D 模式")]
    [SerializeField] private Transform m_DiceModelTransform;
    [SerializeField] private Vector3 m_RotationSpeed = new Vector3(720f, 720f, 0f);
    [SerializeField] private bool m_EnableBounce = true;
    [SerializeField] private float m_BounceHeight = 0.5f;

    [Header("2D 模式")]
    [SerializeField] private UnityEngine.UI.Text m_DiceNumberText;
    [SerializeField] private SpriteRenderer m_DiceSpriteRenderer;
    [SerializeField] private Sprite[] m_DiceFaceSprites; // 6张，索引0=点数1

    private bool m_IsAnimating = false;
    private int m_TargetResult = 1;
    private Action<int> m_OnCompleteCallback;
    private Vector3 m_OriginalPosition;

    public bool IsAnimating => m_IsAnimating;

    private void Awake()
    {
        if (m_DiceModelTransform != null)
            m_OriginalPosition = m_DiceModelTransform.localPosition;
    }

    public void PlayRollAnimation(int targetResult, Action<int> onComplete = null)
    {
        if (m_IsAnimating) { Debug.LogWarning("动画播放中，忽略重复调用"); return; }

        m_TargetResult = Mathf.Clamp(targetResult, 1, 6);
        m_OnCompleteCallback = onComplete;
        m_IsAnimating = true;

        switch (m_AnimationMode)
        {
            case AnimationMode.Model3D: StartCoroutine(Play3DRollAnimation()); break;
            case AnimationMode.Sprite2D: StartCoroutine(Play2DRollAnimation()); break;
        }
    }

    private IEnumerator Play3DRollAnimation()
    {
        float elapsed = 0f;
        while (elapsed < m_AnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / m_AnimationDuration;
            float speedMul = 1f - Mathf.Pow(t, 3f);
            m_DiceModelTransform.Rotate(m_RotationSpeed * speedMul * Time.deltaTime, Space.Self);

            if (m_EnableBounce)
            {
                float bounce = Mathf.Sin(t * Mathf.PI * 4f) * m_BounceHeight * (1f - t);
                m_DiceModelTransform.localPosition = m_OriginalPosition + Vector3.up * bounce;
            }
            yield return null;
        }
        SetDiceFinalRotation(m_TargetResult);
        m_DiceModelTransform.localPosition = m_OriginalPosition;
        FinishAnimation();
    }

    private IEnumerator Play2DRollAnimation()
    {
        float elapsed = 0f, nextChange = 0f, interval = 0.05f;
        while (elapsed < m_AnimationDuration)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= nextChange)
            {
                UpdateDiceDisplay(UnityEngine.Random.Range(1, 7));
                interval = Mathf.Lerp(0.05f, 0.35f, elapsed / m_AnimationDuration);
                nextChange = elapsed + interval;
            }
            yield return null;
        }
        UpdateDiceDisplay(m_TargetResult);
        FinishAnimation();
    }

    private void UpdateDiceDisplay(int number)
    {
        if (m_DiceNumberText != null) m_DiceNumberText.text = number.ToString();
        if (m_DiceSpriteRenderer != null && m_DiceFaceSprites != null && m_DiceFaceSprites.Length >= 6)
            m_DiceSpriteRenderer.sprite = m_DiceFaceSprites[Mathf.Clamp(number - 1, 0, 5)];
    }

    private void SetDiceFinalRotation(int result)
    {
        Vector3[] finals = {
            new Vector3(0,0,0), new Vector3(90,0,0), new Vector3(0,0,90),
            new Vector3(0,0,-90), new Vector3(-90,0,0), new Vector3(180,0,0)
        };
        m_DiceModelTransform.rotation = Quaternion.Euler(finals[Mathf.Clamp(result - 1, 0, 5)]);
    }

    private void FinishAnimation()
    {
        m_IsAnimating = false;
        m_OnCompleteCallback?.Invoke(m_TargetResult);
        m_OnCompleteCallback = null;
    }
}
