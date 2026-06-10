namespace YGZFrameWork
{
    /// <summary>
    /// 资源卸载策略枚举 —— 控制资源加载后的生命周期管理。
    /// 
    /// 使用场景：
    /// - UnLoadNone：常驻资源（如主界面 UI、全局配置），程序运行期间不卸载。
    /// - UnLoadImmediately：一次性资源（如弹窗、临时特效），使用后立即释放。
    /// - UnloadLate：延迟卸载（如战斗单位、场景元素），切场景或手动调用 GC 时统一清理。
    /// </summary>
    public enum ResourceUnloadType
    {
        /// <summary>不卸载 —— 常驻内存，程序退出时统一释放</summary>
        UnLoadNone = 0,

        /// <summary>立即卸载 —— 使用完毕后立即释放资源引用</summary>
        UnLoadImmediately = 1,

        /// <summary>延迟卸载 —— 标记为待清理，统一 GC 时释放</summary>
        UnloadLate = 2
    }
}
