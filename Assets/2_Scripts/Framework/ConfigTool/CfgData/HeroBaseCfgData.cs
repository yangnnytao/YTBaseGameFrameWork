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
                Register(_instance);  // 自注册到全局注册表
                CfgToolManager.Instance.NewCfgTool(ECfgToolType.cfg_HeroBase, _instance);
            }
            return _instance;
        }
    }

    // 保留旧名称兼容，指向新属性
    public static HeroBaseCfgTool mInstance => Instance;
}
