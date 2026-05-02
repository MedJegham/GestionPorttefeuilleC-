using System.ComponentModel.DataAnnotations;

namespace GestionPortefeuille.Core.Entities;

public class PortfolioBudget
{
    public int Id { get; set; }

    [Range(0, 1_000_000_000)]
    public decimal InitialBudget { get; set; }

    [Range(0, 1_000_000_000)]
    public decimal AvailableCash { get; set; }
}
