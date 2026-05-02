using System.ComponentModel.DataAnnotations;
using GestionPortefeuille.Core.Enums;

namespace GestionPortefeuille.Core.Entities;

public class Transaction
{
    public int Id { get; set; }

    [Range(1, int.MaxValue)]
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Range(0.0000001, 10_000_000)]
    public decimal Quantity { get; set; }

    [Range(0.0001, 1_000_000)]
    public decimal UnitPrice { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [Range(0, 1_000_000_000)]
    public decimal TotalAmount { get; set; }
}
