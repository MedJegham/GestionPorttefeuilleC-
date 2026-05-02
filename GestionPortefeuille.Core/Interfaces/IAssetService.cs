using GestionPortefeuille.Core.Entities;
using GestionPortefeuille.Core.Models;

namespace GestionPortefeuille.Core.Interfaces;

public interface IAssetService
{
    Task<List<Asset>> GetAllAsync(string? search = null, string? typeFilter = null);

    Task<PagedResult<Asset>> GetPagedAsync(
        string? search,
        string? typeFilter,
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default);

    Task<Asset?> GetByIdAsync(int id);
    Task<Asset> CreateAsync(Asset asset);
    Task<bool> UpdateAsync(Asset asset);
    Task<bool> DeleteAsync(int id);
}
