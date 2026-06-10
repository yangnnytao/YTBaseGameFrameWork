namespace YGZFrameWork
{
    /// <summary>
    /// 资源分类枚举 —— 用于按类型管理资源加载、缓存和卸载。
    /// 
    /// 设计意图：
    /// 1. 便于按类型批量加载/卸载（如切场景时卸载所有战斗资源）。
    /// 2. 统计各类资源的内存占用和加载次数。
    /// 3. 配置不同类型资源的默认卸载策略和缓存策略。
    /// </summary>
    public enum ResourceCategory
    {
        /// <summary>未分类</summary>
        None = 0,

        /// <summary>UI 资源 —— 面板、按钮、图标等</summary>
        UI = 1,

        /// <summary>角色/怪物模型 —— 战斗单位、NPC</summary>
        Character = 2,

        /// <summary>特效 —— 技能特效、弹道、环境特效</summary>
        Effect = 3,

        /// <summary>音频 —— BGM、音效、语音</summary>
        Audio = 4,

        /// <summary>场景 —— 场景预制体、场景元素</summary>
        Scene = 5,

        /// <summary>图集/Sprite —— UI 图集、角色立绘</summary>
        Atlas = 6,

        /// <summary>动画 —— AnimationClip、Timeline</summary>
        Animation = 7,

        /// <summary>配置表 —— JSON、XML、ScriptableObject</summary>
        Config = 8,

        /// <summary>字体 —— TTF、OTF、BitmapFont</summary>
        Font = 9,

        /// <summary>Shader/Material</summary>
        Shader = 10,

        /// <summary>视频 —— 过场动画、CG</summary>
        Video = 11,

        /// <summary>Spine/Live2D 模型</summary>
        Spine = 12,

        /// <summary>TileMap/瓦片地图</summary>
        TileMap = 13,

        /// <summary>其他</summary>
        Other = 99
    }
}
