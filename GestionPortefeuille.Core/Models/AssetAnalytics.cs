using GestionPortefeuille.Core.Enums;

namespace GestionPortefeuille.Core.Models;

public class AssetAnalytics
{
    public int AssetId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal SimpleMovingAverage { get; set; }
    public decimal VolatilityPercent { get; set; }
    public TrendSignal Signal { get; set; }
    public decimal DistanceToSmaPercent { get; set; }

    /// <summary>Ratio de Sharpe simplifie annualise: (rendement journalier moyen / ecart-type) * sqrt(252).</summary>
    public decimal SharpeLikeAnnualized { get; set; }

    /// <summary>Drawdown maximal sur la serie de prix (valeur positive en %).</summary>
    public decimal MaxDrawdownPercent { get; set; }
}
