using GestionPortefeuille.Core.Enums;

namespace GestionPortefeuille.Core.Models;

public class PortfolioSnapshot
{
    public decimal InitialBudget { get; set; }
    public decimal AvailableCash { get; set; }
    public decimal InvestedAmount { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal TotalValue { get; set; }
    public decimal GainLoss { get; set; }
    public decimal RoiPercent { get; set; }
    public List<HoldingSummary> Holdings { get; set; } = new();
    public Dictionary<AssetType, decimal> AllocationByType { get; set; } = new();
}
