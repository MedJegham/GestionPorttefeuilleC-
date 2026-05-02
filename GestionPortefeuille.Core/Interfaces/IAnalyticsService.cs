using GestionPortefeuille.Core.Models;

namespace GestionPortefeuille.Core.Interfaces;

public interface IAnalyticsService
{
    Task<List<AssetAnalytics>> GetAssetAnalyticsAsync(int smaPeriod = 20);
    Task EnsurePriceHistorySeedAsync(int days = 90);

    Task<BacktestResult?> RunSmaTrendBacktestAsync(int assetId, int smaPeriod = 20, decimal initialCash = 10_000m, CancellationToken cancellationToken = default);
}
