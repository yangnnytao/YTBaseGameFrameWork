using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace YGZFrameWork
{
    public class AppConst {
        public const string AppsLogMainTag = "YGZ";

        public const int TimerInterval = 1;
        public const int GameFrameRate = 30;                        //游戏帧频
        public const bool isVertical = true;

        public const string AppName = "BaseGame";               //应用程序名称
        public const string AssetDir = "StreamingAssets";           //素材目录 
        public const string WebUrl = "http://localhost:6688/";      //测试更新地址

        /// <summary>
        /// 资源加载模式（运行时可通过外部配置覆盖）。
        /// 编辑器默认 Resources，移动端完整包默认 AssetBundle。
        /// </summary>
        public static ResourceLoadMode ResourceLoadMode = ResourceLoadMode.AssetBundle;
    }
}