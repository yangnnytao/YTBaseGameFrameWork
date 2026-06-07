using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YGZFrameWork;
/// <summary>
/// 游戏主流程
/// ljs:创建
/// YT:修改2020-7-24
/// </summary>
public class GameApp : Singleton<GameApp>
{
    /// <summary> 主要的Mono </summary>
    public MonoBehaviour mainMonoBehaviour
    {
        get
        {
            if (!_mainMonoBehaviour)
            {
                GameObject go = new GameObject("_mainMonoBehaviour");
                UnityEngine.Object.DontDestroyOnLoad(go);//防止删除
                _mainMonoBehaviour = go.AddComponent<MonoBehaviour>();
            }
            return _mainMonoBehaviour;
        }
        set
        {
            _mainMonoBehaviour = value;
        }
    }
    private MonoBehaviour _mainMonoBehaviour;

    public delegate void LoadDataProcess(float value_, string loadHint1, string loadHint2_ = "");

    /// <summary> 加载数据进程 </summary>
    public event LoadDataProcess LoadGameDataProcessFunc;

    public delegate void LoadDataDone();
    /// <summary> 加载数据完毕调用 </summary>
    public event LoadDataDone LoadGameDataDoneFunc;


    public GameApp() { }

    private float m_realTime = 0f;//游戏运行时间

    private void RegisterEvent()//注册事件
    {
        LoadGameDataDoneFunc += LoadGameDataDone;
    }

    private void RemoveEvent()//注销事件
    {
        LoadGameDataDoneFunc -= LoadGameDataDone;
    }

    /// <summary> 网络通信连接调用 </summary>
    public void OnNetSocketConnect()
    {
        if (null != LoadGameDataProcessFunc)
            LoadGameDataProcessFunc(0.5f, "网路通讯建立完成");
        Debug.Log("网路通讯建立完成");
        this.LoadGameDataLate();
    }

    public void LoadGameDataLate()
    {
        //本地表格数据
        if (null != LoadGameDataProcessFunc)
        {
            LoadGameDataProcessFunc(1f, "正在初始化本地数据");
            Debug.Log("正在初始化本地数据");
        }
        //TableManager.Instance.InitAllTables();

        //敏感词
        //LoadGameDataProcessFunc(0.7f, "正在加载敏感词库");
        Debug.Log("正在加载敏感词库");
        //Common.StartCoroutine(Utility.loadSensitiveWords());


        //LoadGameDataProcessFunc(1f, "正在准备前端数据");
        Debug.Log("正在准备前端数据");
        TimeManager.Instance.Initialize();
        CanvasManager.Instance.InitDataM();

        if (null != LoadGameDataDoneFunc)
            LoadGameDataDoneFunc();
        RemoveEvent();
#if USE_LOG
            //ScreenLogger.Instance.ShowLog = true;
#endif
    }

    public void LoadGameDataDone()
    {
        //GUIManager.Instance.ShowUI<UILogin>("UILogin", false);
    }

    private void loadDataDone()
    {

    }

    /// <summary> 这里进行整个游戏的初始化 </summary>
    public bool Initialize()
    {
        //从Resource目录加载资源 加载
        //GameMode.ChangeGameMode<LoginMode>();

        RegisterEvent();

        this.LoadGameDataLate();

        return false;
    }

    /// <summary> 更新流程：核心驱动力 </summary>
    public void Update()
    {
        try
        {
            float deltaTime = Time.deltaTime;
            float fixedDeltaTime = Time.fixedDeltaTime;
            float nowTime = Time.realtimeSinceStartup;
            float realDeltaTime = nowTime - m_realTime;
            m_realTime = nowTime;

            //NetClient.Update(Time.deltaTime);//网络更新驱动
            //UpdateComponentManager.GetInstance().update(deltaTime, fixedDeltaTime, realDeltaTime);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    /// <summary> 游戏关闭： 存档数据 </summary>
    public void Terminate()
    {
        //GUIManager.Instance.ClearM();
        //UnitManager.GetInstance().ClearM();
        ManagerOfManager.Instance.DestroyM();
    }

    /// <summary> 游戏暂停 </summary>
    public void ApplicationPause()
    {
    }

    /// <summary> 游戏焦点 </summary>
    public void ApplicationFocus()
    {

    }

    /// <summary> 游戏检查 </summary>
    public void ApplicationEnable()
    {
    }

    /// <summary> 版本更新后数据检查 </summary>
    public void UpdateVesionInfo()
    {

    }
}
