namespace GestionPortefeuille.Core.Options;

public class AlertOptions
{
    public const string SectionName = "Alerts";

    /// <summary>Seuil de perte sur le budget initial (%) pour alerte critique.</summary>
    public decimal LossPercentOfInitial { get; set; } = 10m;

    /// <summary>Liquidite en % du budget initial en dessous duquel on alerte.</summary>
    public decimal LowLiquidityPercentOfInitial { get; set; } = 5m;

    /// <summary>Volatilite annualisee approximative (%) au-dessus de laquelle on alerte.</summary>
    public decimal HighVolatilityPercent { get; set; } = 35m;
}
