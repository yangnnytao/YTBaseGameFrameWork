using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 资源管理器 —— 统一资源加载入口，封装底层加载器切换逻辑。
    /// 
    /// 设计说明：
    /// 1. 通过 AppConst.UseAddressables 或构建时宏切换底层加载器。
    /// 2. 上层代码（UIManager、CfgToolManager 等）统一通过 ResourceManager.Loader 访问。
    /// 3. 微信小游戏默认使用 Addressables（分包加载），编辑器默认使用 Resources（快速迭代）。
    /// </summary>
    public class ResourceManager : Singleton<ResourceManager>, IManagerInterface
    {
        public static ResourceManager Instance => GetInstance();

        /// <summary>当前使用的资源加载器</summary>
        public IResourceLoader Loader { get; private set; }

        public override void InitDataM()
        {
            base.InitDataM();
            // 根据平台选择加载器
#if UNITY_EDITOR
            Loader = ResourcesLoader.Instance;
            Debug.Log("[ResourceManager] 编辑器模式：使用 ResourcesLoader");
#elif WEIXIN_MINIGAME || UNITY_WEBGL
            Loader = AddressablesLoader.Instance;
            Debug.Log("[ResourceManager] 微信小游戏/WebGL：使用 AddressablesLoader");
#else
            // 移动端默认使用 Addressables（支持热更）
            Loader = AddressablesLoader.Instance;
            Debug.Log("[ResourceManager] 移动端：使用 AddressablesLoader");
#endif
        }

        public override void DestroyM()
        {
            Loader = null;
            base.DestroyM();
        }

        public void RegisterMsg()
        {
            // 当前无消息需要注册
        }

        public void ClearData()
        {
            // 当前无数据需要清空
        }
    }
}
