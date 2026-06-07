using System;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// Canvas 层级枚举（与 CanvasManager 的 4 主层 + 3 子层对应）
    /// </summary>
    public enum CanvasLayer
    {
        // 4 主层
        Background = 0,     // 背景层（场景底图、天空盒）
        Main = 1,           // 主界面层（主菜单、HUD、主面板）
        Popup = 2,          // 弹窗层（设置、背包、提示框）
        Top = 3,            // 顶层（Loading、Toast、网络提示）

        // 3 子层（嵌入在主层内，用于同层内的局部叠加）
        SubLayer1 = 10,     // 子层 1
        SubLayer2 = 11,     // 子层 2
        SubLayer3 = 12,     // 子层 3
    }

    /// <summary>
    /// 面板动画类型
    /// </summary>
    public enum PanelAnimType
    {
        None = 0,           // 无动画
        Fade = 1,           // 淡入淡出
        Scale = 2,          // 缩放弹入弹出
        SlideLeft = 3,      // 从左滑入
        SlideRight = 4,     // 从右滑入
        SlideUp = 5,        // 从下往上滑入
        SlideDown = 6,      // 从上往下滑入
    }

    /// <summary>
    /// 单条面板配置数据
    /// </summary>
    [Serializable]
    public class PanelConfigEntry
    {
        /// <summary>面板唯一标识名（代码中引用，与预制体名一致）</summary>
        public string panelId;

        /// <summary>预制体在 Resources 下的路径（不含 Resources/ 前缀，如 "Prefabs/UI/DiceTestPanel"）</summary>
        public string prefabPath;

        /// <summary>所属 Canvas 层级</summary>
        public CanvasLayer canvasLayer;

        /// <summary>Animator 打开动画 Trigger 名称（为空则不播放）</summary>
        public string openAnimationTrigger;

        /// <summary>Animator 关闭动画 Trigger 名称（为空则不播放）</summary>
        public string closeAnimationTrigger;

        /// <summary>是否独占面板（打开时自动关闭同层级其他非独占面板）</summary>
        public bool isExclusive;

        /// <summary>是否缓存（关闭后不移除，仅隐藏，下次直接复用）</summary>
        public bool isCache;

        /// <summary>打开时是否暂停下层面板（调用下层 OnPause）</summary>
        public bool pauseBelow;

        /// <summary>是否响应返回按钮（Android 物理返回键）</summary>
        public bool handleBackButton;
    }

    /// <summary>
    /// 面板配置表 —— ScriptableObject，存放于 Resources/Config/ 下
    /// 运行时通过 UIManager 加载并建立 name → entry 索引
    /// </summary>
    [CreateAssetMenu(fileName = "PanelConfig", menuName = "YGZFrameWork/PanelConfig")]
    public class PanelConfigData : ScriptableObject
    {
        [Header("面板配置列表")]
        public PanelConfigEntry[] panels;
    }
}
