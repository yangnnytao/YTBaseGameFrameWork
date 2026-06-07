using YGZFrameWork;

public class DiceRollCommand : ControllerCommand
{
    public override void Execute(IMessage message)
    {
        DiceManager.Instance.Roll();
    }
}
