using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YGZFrameWork;

public class ManagerOfManager : Singleton<ManagerOfManager>
{
    public override void InitDataM()
    {
        CfgToolManager.Instance.Init();
        //GameDataManager.Instance.InitDataM();
    }

    public override void DestroyM()
    {
        CfgToolManager.Instance.Destroy();
        //GameDataManager.Instance.DestroyM();
    }
}
