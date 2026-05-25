using YGZFrameWork;

/// <summary>
/// UI 命令 — Facade 命令模式
/// 将 Facade 字符串消息映射到 UIManager 操作。
/// </summary>
public class OpenPanelCommand : ICommand
{
    public void Execute(IMessage message)
    {
        if (message.Body is string panelId)
        {
            UIManager.GetInstance().OpenPanel(panelId);
        }
    }
}

public class ClosePanelCommand : ICommand
{
    public void Execute(IMessage message)
    {
        if (message.Body is string panelId)
        {
            UIManager.GetInstance().ClosePanel(panelId);
        }
    }
}

public class CloseTopPanelCommand : ICommand
{
    public void Execute(IMessage message)
    {
        UIManager.GetInstance().CloseTopPanel();
    }
}

public class ClearAllPanelsCommand : ICommand
{
    public void Execute(IMessage message)
    {
        UIManager.GetInstance().ClearAllPanels();
    }
}
