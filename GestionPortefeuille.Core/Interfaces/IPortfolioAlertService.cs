using GestionPortefeuille.Core.Models;

namespace GestionPortefeuille.Core.Interfaces;

public interface IPortfolioAlertService
{
    Task<IReadOnlyList<PortfolioAlert>> GetAlertsAsync(CancellationToken cancellationToken = default);
}
