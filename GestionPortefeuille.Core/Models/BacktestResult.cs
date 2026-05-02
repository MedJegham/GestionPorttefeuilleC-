namespace GestionPortefeuille.Core.Models;

public class BacktestResult
{
    public int AssetId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int SmaPeriod { get; set; }
    public decimal InitialCash { get; set; }
    public decimal FinalStrategyEquity { get; set; }
    public decimal FinalBuyHoldEquity { get; set; }
    public int TradeCount { get; set; }
    public string RuleDescription { get; set; } = "Achat si cours > SMA, vente si cours < SMA.";
}
