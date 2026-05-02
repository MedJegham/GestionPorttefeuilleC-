namespace GestionPortefeuille.Core.Entities;

/// <summary>Point quotidien de valeur totale du portefeuille (cash + positions au cours du jour).</summary>
public class PortfolioValuePoint
{
    public int Id { get; set; }

    /// <summary>Date UTC (minuit) du releve.</summary>
    public DateTime Date { get; set; }

    public decimal TotalValue { get; set; }
}
