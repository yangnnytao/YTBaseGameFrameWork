using YGZFrameWork;

/// <summary>
/// 示例：新增配置表时，只需创建此文件，零改任何现有代码
/// Source Generator 编译时自动发现并注册到 CfgToolRegistry
/// </summary>
public class ItemCfgData : CfgBase<int>
{
    public string name;
    public int type;      // 1=武器 2=防具 3=消耗品
    public int quality;   // 1=白 2=绿 3=蓝 4=紫 5=橙
    public int stackMax;
}

public class ItemCfgTool : CfgToolBase<int, ItemCfgData>
{
    protected override string mTableName => "cfg_item";

    private static ItemCfgTool _instance;
    public static ItemCfgTool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ItemCfgTool();
                CfgToolManager.Instance.NewCfgTool(ECfgToolType.cfg_Item, _instance);
            }
            return _instance;
        }
    }
}
