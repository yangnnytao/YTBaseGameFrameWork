using YGZFrameWork;

/// <summary>
/// 示例：新增配置表时，只需创建此文件
/// 并在 CfgToolRegistry 中加一行 (typeof(XxxCfgTool), () => XxxCfgTool.Instance)
/// </summary>
public class ItemCfgData : CfgBase<int>
{
    public string name;
    public int type;      // 1=武器 2=防具 3=消耗品
    public int quality;   // 1=白 2=绿 3=蓝 4=紫 5=橙
    public int maxNum;//最大数量
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
                // 自注册和向 CfgToolManager 注册已在基类构造中自动完成
            }
            return _instance;
        }
    }
}
