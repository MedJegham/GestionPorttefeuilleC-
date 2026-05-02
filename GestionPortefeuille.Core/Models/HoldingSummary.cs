using GestionPortefeuille.Core.Enums;

namespace GestionPortefeuille.Core.Models;

public class HoldingSummary
{
    public int AssetId { get; set; }
    public AssetType AssetType { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    /// <summary>Cout moyen pondere unitaire (CMP) des titres encore detenus.</summary>
    public decimal AverageBuyPrice { get; set; }
    public decimal CostBasis { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MarketValue { get; set; }
    public decimal GainLoss { get; set; }
    public decimal AllocationPercent { get; set; }
}
