using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace YGZFrameWork
{
    /// <summary>
    /// 游戏管理器
    /// </summary>
    public class GameManager : ManagerMono<GameManager>
    {
        /// <summary> 初始化游戏管理器 </summary>
        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        /// <summary> 初始化 </summary>
        void Init()
        {
            DontDestroyOnLoad(gameObject);  //防止销毁自己

            //ljs 暂时注销，等AB部署完后开放
            ////从Resource目录加载资源 加载
            //GUIManager.Instance.ShowUIFromResource<LoadingUI>("LoadingUI", false);
            //CheckExtractResource(); //释放资源
            OnInitialize();//ljs add AB版本后记得关闭

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = AppConst.GameFrameRate;
        }

        void OnInitialize()
        {
            GameApp.Instance.Initialize();
        }
    }
}