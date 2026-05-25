using UnityEngine;
using System.Collections;
using System.Diagnostics.Contracts;

public class NotiConst
{
    // Controller层消息通知

    /// <summary> 启动框架 </summary>
    public const string START_UP = "StartUp";

    /// <summary> 派发信息 </summary>
    public const string DISPATCH_MESSAGE = "DispatchMessage";

    // View层消息通知

    /// <summary> 更新消息 </summary>
    public const string UPDATE_MESSAGE = "UpdateMessage";

    /// <summary> 更新解包 </summary>
    public const string UPDATE_EXTRACT = "UpdateExtract";

    /// <summary> 更新下载 </summary>
    public const string UPDATE_DOWNLOAD = "UpdateDownload";

    /// <summary> 更新进度 </summary>
    public const string UPDATE_PROGRESS = "UpdateProgress";

    /// <summary> 加载进度 </summary>
    public const string LOAD_RPOGRESS = "LoadProgress";

    #region LoginUI
    /// <summary> 选择服务器 </summary>
    public const string SERVER_SELECT = "SelectServer";

    /// <summary> 设置服务器成功 </summary>
    public const string SUCCESS_SELECTSERVER = "SuccessSelectServer";

    /// <summary> 登录成功 </summary>
    //public const string SUCCESS_LOGIN = "SuccessLogin";

    /// <summary> 账号登陆确定 </summary>
    public const string SUCCESS_LOGIN_NAMEANDPASSWORD = "SuccessLoginNameAndPassword";

    #endregion end_LoginUI

    #region RandomEventPanel
    public const string RANDOMEVENTPANEL_SELECTMAP = "RandomEventPanel_selectMap";
    #endregion

    #region Dice
    public const string DICE_ROLL_STARTED = "DiceRollStarted";
    public const string DICE_ROLL_COMPLETED = "DiceRollCompleted";
    public const string DICE_ROLL_ILLEGAL = "DiceRollIllegal";
    public const string DICE_ROLL = "DICE_ROLL";
    #endregion

    #region UI
    public const string UI_OPEN_PANEL = "UIOpenPanel";
    public const string UI_CLOSE_PANEL = "UIClosePanel";
    public const string UI_CLOSE_TOP_PANEL = "UICloseTopPanel";
    public const string UI_CLEAR_ALL_PANELS = "UIClearAllPanels";
    public const string UI_PANEL_OPENED = "UIPanelOpened";
    public const string UI_PANEL_CLOSED = "UIPanelClosed";
    #endregion
}
