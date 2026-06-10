namespace YGZFrameWork
{
    /// <summary>
    /// 资源加载模式枚举 —— 定义框架支持的三种底层加载策略。
    /// </summary>
    public enum ResourceLoadMode
    {
        /// <summary>
        /// Resources 加载模式 —— 基于 Unity Resources.Load。
        /// 适用于：编辑器调试、默认小包（只含必要资源）、快速迭代。
        /// 特点：零依赖、无热更能力、包体受 Resources 目录大小限制。
        /// </summary>
        Resources,

        /// <summary>
        /// AssetBundle 加载模式 —— 基于原生 Unity AssetBundle。
        /// 适用于：原生 Android/iOS/PC 完整包、支持热更新、分包下载。
        /// 特点：需自行管理 AB 包依赖、缓存、解压路径，灵活性最高。
        /// </summary>
        AssetBundle,

        /// <summary>
        /// Addressables 加载模式 —— 基于 Unity Addressables System。
        /// 适用于：需要官方封装的热更、远程 Catalog、分组管理。
        /// 特点：依赖 Addressables 包，配置在 Addressables Groups 中维护。
        /// </summary>
        Addressables
    }
}
