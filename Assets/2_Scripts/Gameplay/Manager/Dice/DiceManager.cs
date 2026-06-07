using System;
using UnityEngine;
using YGZFrameWork;

/// <summary>
/// 骰子管理器
/// 负责骰子状态管理、点数生成、投掷流程控制，并集成到 Facade 与事件总线。
/// </summary>
public class DiceManager : ManagerBase<DiceManager>, IManagerInterface
{
    public static DiceManager Instance => GetInstance();

    public enum DiceState
    {
        Idle = 0,       // 空闲
        Rolling = 1,    // 投掷中
        Stopped = 2     // 已停止
    }

    // 内部事件ID（EventDispatcher int-based）
    public static class DiceEventID
    {
        public const int RollStarted = 1001;
        public const int RollCompleted = 1002;
        public const int IllegalOperation = 1003;
    }

    private DiceState m_CurrentState = DiceState.Idle;
    private int m_CurrentResult = 0;
    private int m_PendingResult = 0;

    public DiceState CurrentState => m_CurrentState;
    public int CurrentResult => m_CurrentResult;
    public int PendingResult => m_PendingResult;

    public override void InitDataM()
    {
        base.InitDataM();
        RegisterMsg();
    }

    public override void DestroyM()
    {
        ClearData();
        base.DestroyM();
    }

    public void RegisterMsg() { }

    public void ClearData()
    {
        m_CurrentState = DiceState.Idle;
        m_CurrentResult = 0;
        m_PendingResult = 0;
    }

    /// <summary>开始投掷骰子。动画结束后需调用 OnRollAnimationFinished 确认。</summary>
    public void Roll()
    {
        if (m_CurrentState == DiceState.Rolling)
        {
            string reason = "[DiceManager] 非法操作：当前正处于投掷中";
            Debug.LogWarning(reason);
            DispatchEvent(DiceEventID.IllegalOperation, reason);
            AppFacade.Instance.SendMessageCommand(NotiConst.DICE_ROLL_ILLEGAL, reason);
            return;
        }

        m_PendingResult = UnityEngine.Random.Range(1, 7);
        m_CurrentState = DiceState.Rolling;
        m_CurrentResult = 0;

        Debug.Log($"[DiceManager] 投骰子开始，预生成结果：{m_PendingResult}");

        // 内部事件 + Facade 消息双广播
        DispatchEvent(DiceEventID.RollStarted, m_PendingResult);
        AppFacade.Instance.SendMessageCommand(NotiConst.DICE_ROLL_STARTED, m_PendingResult);
    }

    /// <summary>动画结束回调入口，确认最终结果。</summary>
    public void OnRollAnimationFinished(int result)
    {
        if (m_CurrentState != DiceState.Rolling)
        {
            Debug.LogWarning($"[DiceManager] 状态异常：{m_CurrentState}，忽略回调");
            return;
        }

        m_CurrentResult = Mathf.Clamp(result, 1, 6);
        m_CurrentState = DiceState.Stopped;
        m_PendingResult = 0;

        Debug.Log($"[DiceManager] 投骰子结束，结果：{m_CurrentResult}");

        DispatchEvent(DiceEventID.RollCompleted, m_CurrentResult);
        AppFacade.Instance.SendMessageCommand(NotiConst.DICE_ROLL_COMPLETED, m_CurrentResult);
    }

    public int GetResult() => m_CurrentResult;
    public bool IsRolling() => m_CurrentState == DiceState.Rolling;

    public void ResetDice()
    {
        m_CurrentState = DiceState.Idle;
        m_CurrentResult = 0;
        m_PendingResult = 0;
    }

    /// <summary>强制设置点数（调试/网络同步/技能效果），跳过动画。</summary>
    public void SetResultDirectly(int result)
    {
        m_CurrentResult = Mathf.Clamp(result, 1, 6);
        m_CurrentState = DiceState.Stopped;
        m_PendingResult = 0;
        AppFacade.Instance.SendMessageCommand(NotiConst.DICE_ROLL_COMPLETED, m_CurrentResult);
    }
}
