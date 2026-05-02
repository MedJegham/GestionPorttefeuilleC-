using GestionPortefeuille.Core.Entities;
using GestionPortefeuille.Core.Models;

namespace GestionPortefeuille.Core.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioSnapshot> GetSnapshotAsync();
    Task<PortfolioBudget> GetBudgetAsync();
    Task UpdateBudgetAsync(decimal initialBudget, decimal availableCash);

    /// <summary>Enregistre ou met a jour la valeur totale du jour (pour courbe historique).</summary>
    Task RecordDailyPortfolioValueAsync(decimal totalValue, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortfolioValuePoint>> GetPortfolioValueHistoryAsync(int maxPoints = 365, CancellationToken cancellationToken = default);
}
