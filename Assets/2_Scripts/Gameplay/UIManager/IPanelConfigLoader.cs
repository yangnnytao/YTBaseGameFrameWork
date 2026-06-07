using System.Collections.Generic;

namespace YGZFrameWork
{
    /// <summary>
    /// UI 面板配置加载器接口契约
    /// 主分支只依赖此接口，不关心具体实现
    /// </summary>
    public interface IPanelConfigLoader
    {
        /// <summary> 加载所有面板配置 </summary>
        void LoadConfigs();

        /// <summary> 按面板名获取配置 </summary>
        PanelConfigEntry GetConfig(string panelName);

        /// <summary> 获取所有已加载的配置 </summary>
        Dictionary<string, PanelConfigEntry> GetAllConfigs();

        /// <summary> 热重载配置 </summary>
        void Reload();
    }
}
