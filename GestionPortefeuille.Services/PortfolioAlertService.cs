using GestionPortefeuille.Core.Interfaces;
using GestionPortefeuille.Core.Models;
using GestionPortefeuille.Core.Options;
using Microsoft.Extensions.Options;

namespace GestionPortefeuille.Services;

public class PortfolioAlertService(
    IPortfolioService portfolioService,
    IAnalyticsService analyticsService,
    IOptions<AlertOptions> options) : IPortfolioAlertService
{
    public async Task<IReadOnlyList<PortfolioAlert>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        var o = options.Value;
        var alerts = new List<PortfolioAlert>();
        var snapshot = await portfolioService.GetSnapshotAsync();
        var analytics = await analyticsService.GetAssetAnalyticsAsync();

        if (snapshot.InitialBudget > 0 && snapshot.GainLoss < 0)
        {
            var lossPct = -snapshot.GainLoss / snapshot.InitialBudget * 100m;
            if (lossPct >= o.LossPercentOfInitial)
            {
                alerts.Add(new PortfolioAlert
                {
                    Severity = AlertSeverity.Danger,
                    Title = "Perte significative",
                    Message = $"La perte atteint {lossPct:N1} % du budget initial (seuil {o.LossPercentOfInitial} %)."
                });
            }
        }

        if (snapshot.InitialBudget > 0)
        {
            var liqPct = snapshot.AvailableCash / snapshot.InitialBudget * 100m;
            if (liqPct < o.LowLiquidityPercentOfInitial)
            {
                alerts.Add(new PortfolioAlert
                {
                    Severity = AlertSeverity.Warning,
                    Title = "Liquidite basse",
                    Message = $"La liquidite disponible represente {liqPct:N1} % du budget initial (seuil {o.LowLiquidityPercentOfInitial} %)."
                });
            }
        }

        foreach (var a in analytics.Where(x => x.VolatilityPercent >= o.HighVolatilityPercent))
        {
            alerts.Add(new PortfolioAlert
            {
                Severity = AlertSeverity.Warning,
                Title = $"Volatilite elevee — {a.Symbol}",
                Message = $"Volatilite annualisee approximative {a.VolatilityPercent:N1} % (seuil {o.HighVolatilityPercent} %)."
            });
        }

        return alerts;
    }
}
