using System.ComponentModel.DataAnnotations;

namespace GestionPortefeuille.Core.Entities;

public class PriceHistory
{
    public int Id { get; set; }

    [Required]
    public int AssetId { get; set; }
    public Asset? Asset { get; set; }

    public DateTime Date { get; set; }

    [Range(0.0001, 1_000_000)]
    public decimal ClosePrice { get; set; }
}
