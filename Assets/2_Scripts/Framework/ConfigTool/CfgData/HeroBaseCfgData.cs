using YGZFrameWork;

public class HeroBaseCfgData: CfgBase<int>
{
    public string name;
    public string job;
    public bool isHuman;
    public float baseHp;
    public float baseAtk;
    public float baseDef;
    public float baseSpd;
}

public class HeroBaseCfgTool : CfgToolBase<int, HeroBaseCfgData>
{
    protected override string mTableName => "cfg_heroBase";

    private static HeroBaseCfgTool _instance = null;
    public static HeroBaseCfgTool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new HeroBaseCfgTool();
                // 自注册和向 CfgToolManager 注册已在基类构造中自动完成
            }
            return _instance;
        }
    }

    // 保留旧名称兼容，指向新属性
    public static HeroBaseCfgTool mInstance => Instance;
}
