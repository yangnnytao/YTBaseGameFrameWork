using UnityEngine;

/// <summary>
/// 面板配置数据
/// ScriptableObject，定义每个面板的预制体路径、Canvas 层级、动画类型、是否独占、是否缓存。
/// </summary>
[CreateAssetMenu(fileName = "PanelConfig", menuName = "YGZFrameWork/PanelConfig")]
public class PanelConfigData : ScriptableObject
{
    [Header("基础配置")]
    [Tooltip("面板唯一标识")]
    public string panelId;

    [Tooltip("面板预制体")]
    public GameObject panelPrefab;

    [Tooltip("挂载的 Canvas 层级名称")]
    public string canvasLayer = "Main";

    [Header("行为配置")]
    [Tooltip("是否独占面板（打开时关闭同层级其他面板）")]
    public bool isExclusive = false;

    [Tooltip("是否缓存（关闭后隐藏而非销毁）")]
    public bool isCache = true;

    [Header("动画配置")]
    [Tooltip("打开动画 Trigger 名称")]
    public string openAnimationTrigger = "Open";

    [Tooltip("关闭动画 Trigger 名称")]
    public string closeAnimationTrigger = "Close";

    [Tooltip("是否使用 CanvasGroup 渐变")]
    public bool useFade = true;

    [Tooltip("渐变持续时间")]
    public float fadeDuration = 0.2f;
}
