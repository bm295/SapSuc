using Adapter;

namespace SuccessFactorsLike.Integration.AdapterBridge;

public sealed class AdapterImportExecutor
{
    private readonly TradingDataImporter _importer = new();

    public void Execute(Action action)
    {
        _importer.ImportData(new ActionConnector(action));
    }
}
