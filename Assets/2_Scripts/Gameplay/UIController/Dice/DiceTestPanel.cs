using UnityEngine;
using UnityEngine.UI;
using YGZFrameWork;
using System.Collections.Generic;

/// <summary>
/// 骰子测试面板 —— 用于验证 DiceManager + DiceRollAnimation 的 UI 测试界面
/// 继承 BasePanel，通过 UIManager 打开
/// </summary>
public class DiceTestPanel : BasePanel
{
    [Header("UI 引用")]
    [SerializeField] private Button m_RollButton;           // 投骰按钮
    [SerializeField] private Button m_ForceRollButton;       // 强制投骰按钮（设固定点数）
    [SerializeField] private Text m_ResultText;              // 结果显示文本
    [SerializeField] private Text m_StateText;             // 状态显示文本
    [SerializeField] private DiceRollAnimation m_DiceAnim;   // 骰子动画组件引用（场景中挂好）

    [Header("强制投骰设置")]
    [SerializeField] private int m_ForceDiceValue = 6;       // 强制投骰的点数（1~6）

    private int m_LastResult = 0;

    protected override void Awake()
    {
        base.Awake();
        // 绑定按钮事件
        if (m_RollButton != null)
            m_RollButton.onClick.AddListener(OnRollButtonClicked);
        if (m_ForceRollButton != null)
            m_ForceRollButton.onClick.AddListener(OnForceRollButtonClicked);
    }

    public override void OnOpen(object data)
    {
        base.OnOpen(data);
        // 注册骰子相关消息监听
        RegisterMessage(this, new List<string>
        {
            NotiConst.DICE_ROLL_STARTED,
            NotiConst.DICE_ROLL_COMPLETED,
            NotiConst.DICE_ROLL_ILLEGAL
        });

        UpdateStateText("就绪");
        m_ResultText.text = "点击投骰";
    }

    public override void OnClose()
    {
        base.OnClose();
        // 移除按钮监听
        if (m_RollButton != null)
            m_RollButton.onClick.RemoveListener(OnRollButtonClicked);
        if (m_ForceRollButton != null)
            m_ForceRollButton.onClick.RemoveListener(OnForceRollButtonClicked);
    }

    public override void OnMessage(IMessage msg)
    {
        switch (msg.Name)
        {
            case NotiConst.DICE_ROLL_STARTED:
                int pending = (int)msg.Body;
                UpdateStateText($"投掷中... (预生成: {pending})");
                // 驱动动画
                if (m_DiceAnim != null)
                {
                    m_DiceAnim.PlayRollAnimation(pending, OnRollAnimationFinished);
                }
                break;

            case NotiConst.DICE_ROLL_COMPLETED:
                int final = (int)msg.Body;
                m_LastResult = final;
                m_ResultText.text = $"结果: {final}";
                UpdateStateText("已停止");
                Debug.Log($"[DiceTestPanel] 投骰完成，结果: {final}");
                break;

            case NotiConst.DICE_ROLL_ILLEGAL:
                string reason = (string)msg.Body;
                UpdateStateText($"非法操作!");
                Debug.LogWarning($"[DiceTestPanel] {reason}");
                break;
        }
    }

    /// <summary> 正常投骰按钮点击 </summary>
    private void OnRollButtonClicked()
    {
        // 方式1: 通过 Facade 命令触发（推荐，走框架流程）
        //AppFacade.Instance.SendMessageCommand(NotiConst.DICE_ROLL);

        // 方式2: 直接调用（调试用，绕过命令模式）
        DiceManager.Instance.Roll();
    }

    /// <summary> 强制投骰按钮点击（跳过动画，直接设点数） </summary>
    private void OnForceRollButtonClicked()
    {
        int value = Mathf.Clamp(m_ForceDiceValue, 1, 6);
        DiceManager.Instance.SetResultDirectly(value);
        m_ResultText.text = $"强制结果: {value}";
        UpdateStateText("已停止 (强制)");
        Debug.Log($"[DiceTestPanel] 强制设点数: {value}");
    }

    /// <summary> 动画完成回调 </summary>
    private void OnRollAnimationFinished(int result)
    {
        // 通知 DiceManager 动画结束，确认最终结果
        DiceManager.Instance.OnRollAnimationFinished(result);
    }

    /// <summary> 更新状态文本 </summary>
    private void UpdateStateText(string state)
    {
        if (m_StateText != null)
            m_StateText.text = $"状态: {state}";
    }

    /// <summary> 获取最后一次投骰结果（外部调用） </summary>
    public int GetLastResult() => m_LastResult;
}
