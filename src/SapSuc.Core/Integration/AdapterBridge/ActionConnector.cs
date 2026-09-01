using Adapter;

namespace SuccessFactorsLike.Integration.AdapterBridge;

internal sealed class ActionConnector : Connector
{
    private readonly Action _action;

    public ActionConnector(Action action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public override void GetData()
    {
        _action();
    }
}
