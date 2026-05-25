using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YGZFrameWork;

public class ManagerOfManager : Singleton<ManagerOfManager>
{
    public override void InitDataM()
    {
        CfgToolManager.Instance.Init();
        CanvasManager.Instance.InitDataM();
        UIManager.Instance.InitDataM();
        //GameDataManager.Instance.InitDataM();
        UIManager.Instance.OpenPanel("DiceTestPanel");
    }

    public override void DestroyM()
    {
        CfgToolManager.Instance.Destroy();
        CanvasManager.Instance.DestroyM();
        UIManager.Instance.DestroyM();
        //GameDataManager.Instance.DestroyM();
       
    }
}
