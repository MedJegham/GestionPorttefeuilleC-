using System.ComponentModel.DataAnnotations;
using GestionPortefeuille.Core.Enums;

namespace GestionPortefeuille.Core.Entities;

public class Asset
{
    public int Id { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 2)]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AssetType AssetType { get; set; }

    [Range(0.0001, 1_000_000)]
    public decimal CurrentPrice { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<PriceHistory> PriceHistories { get; set; } = new List<PriceHistory>();
}
