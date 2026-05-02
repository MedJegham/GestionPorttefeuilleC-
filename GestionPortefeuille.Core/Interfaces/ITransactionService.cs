using GestionPortefeuille.Core.Entities;
using GestionPortefeuille.Core.Models;

namespace GestionPortefeuille.Core.Interfaces;

public interface ITransactionService
{
    Task<List<Transaction>> GetAllAsync(int? assetId = null);

    Task<PagedResult<Transaction>> GetPagedAsync(
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending,
        int? assetId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> CreateAsync(Transaction transaction);
    Task<bool> DeleteAsync(int id);
}
