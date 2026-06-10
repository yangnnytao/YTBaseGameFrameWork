using System;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 资源加载接口 —— 统一封装 Resources.Load / AssetBundle / EditorAssetDatabase 等多种加载方式。
    /// 
    /// 设计意图：
    /// 1. 上层代码（UIManager、CfgToolManager 等）只依赖此接口，不关心具体加载实现。
    /// 2. 通过 AppConst 或构建时配置切换底层加载器，无需修改业务代码。
    /// 3. 支持同步加载（返回 T）和异步加载（回调/Coroutine）。
    /// 4. 支持卸载策略控制（常驻/立即释放/延迟释放）。
    /// 5. IL2CPP 兼容，零反射。
    /// </summary>
    public interface IResourceLoader
    {
        /// <summary>同步加载资源</summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径（不含扩展名）</param>
        /// <param name="unloadType">卸载策略（默认延迟卸载）</param>
        /// <returns>加载的资源实例，失败返回 null</returns>
        T Load<T>(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate) where T : UnityEngine.Object;

        /// <summary>异步加载资源（回调方式）</summary>
        void LoadAsync<T>(string path, Action<T> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate) where T : UnityEngine.Object;

        /// <summary>加载并实例化 GameObject（同步）</summary>
        GameObject LoadPrefab(string path, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate);

        /// <summary>异步加载并实例化 GameObject</summary>
        void LoadPrefabAsync(string path, Action<GameObject> onComplete, ResourceUnloadType unloadType = ResourceUnloadType.UnloadLate);

        /// <summary>释放资源引用</summary>
        void Release(UnityEngine.Object asset);

        /// <summary>预加载资源（可选实现）</summary>
        void Preload<T>(string path) where T : UnityEngine.Object;
    }
}
