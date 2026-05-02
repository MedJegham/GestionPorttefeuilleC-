using GestionPortefeuille.Core.Entities;
using GestionPortefeuille.Core.Interfaces;
using GestionPortefeuille.Core.Models;
using GestionPortefeuille.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionPortefeuille.Services;

public class AssetService(AppDbContext dbContext) : IAssetService
{
    public async Task<List<Asset>> GetAllAsync(string? search = null, string? typeFilter = null)
    {
        var query = BuildFilteredQuery(search, typeFilter);
        return await query.OrderBy(a => a.Symbol).ToListAsync();
    }

    public async Task<PagedResult<Asset>> GetPagedAsync(
        string? search,
        string? typeFilter,
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = BuildFilteredQuery(search, typeFilter);
        var total = await query.CountAsync(cancellationToken);

        query = (sortBy ?? "symbol").ToLowerInvariant() switch
        {
            "name" => sortDescending
                ? query.OrderByDescending(a => a.Name)
                : query.OrderBy(a => a.Name),
            "type" => sortDescending
                ? query.OrderByDescending(a => a.AssetType)
                : query.OrderBy(a => a.AssetType),
            "price" => sortDescending
                ? query.OrderByDescending(a => a.CurrentPrice)
                : query.OrderBy(a => a.CurrentPrice),
            _ => sortDescending
                ? query.OrderByDescending(a => a.Symbol)
                : query.OrderBy(a => a.Symbol)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Asset>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private IQueryable<Asset> BuildFilteredQuery(string? search, string? typeFilter)
    {
        var query = dbContext.Assets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();
            query = query.Where(a =>
                a.Symbol.ToLower().Contains(normalized) ||
                a.Name.ToLower().Contains(normalized));
        }

        if (!string.IsNullOrWhiteSpace(typeFilter) && Enum.TryParse<Core.Enums.AssetType>(typeFilter, true, out var parsedType))
        {
            query = query.Where(a => a.AssetType == parsedType);
        }

        return query;
    }

    public Task<Asset?> GetByIdAsync(int id) =>
        dbContext.Assets.FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Asset> CreateAsync(Asset asset)
    {
        asset.Symbol = asset.Symbol.Trim().ToUpperInvariant();
        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync();
        return asset;
    }

    public async Task<bool> UpdateAsync(Asset asset)
    {
        var existing = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == asset.Id);
        if (existing is null)
        {
            return false;
        }

        existing.Symbol = asset.Symbol.Trim().ToUpperInvariant();
        existing.Name = asset.Name.Trim();
        existing.AssetType = asset.AssetType;
        existing.CurrentPrice = asset.CurrentPrice;
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == id);
        if (existing is null)
        {
            return false;
        }

        dbContext.Assets.Remove(existing);
        await dbContext.SaveChangesAsync();
        return true;
    }
}
