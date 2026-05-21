using GestionPortefeuille.Core.Entities;
using GestionPortefeuille.Core.Enums;
using GestionPortefeuille.Core.Interfaces;
using GestionPortefeuille.Core.Models;
using GestionPortefeuille.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionPortefeuille.Services;

public class TransactionService(AppDbContext dbContext, IPortfolioService portfolioService) : ITransactionService
{
    public Task<List<Transaction>> GetAllAsync(int? assetId = null)
    {
        var query = dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Asset)
            .OrderByDescending(t => t.Date)
            .AsQueryable();

        if (assetId.HasValue)
        {
            query = query.Where(t => t.AssetId == assetId.Value);
        }

        return query.ToListAsync();
    }

    public async Task<PagedResult<Transaction>> GetPagedAsync(
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending,
        int? assetId,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Transactions
            .AsNoTracking()
            .Include(t => t.Asset)
            .AsQueryable();

        if (assetId.HasValue)
        {
            query = query.Where(t => t.AssetId == assetId.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        query = (sortBy ?? "date").ToLowerInvariant() switch
        {
            "symbol" => sortDescending
                ? query.OrderByDescending(t => t.Asset!.Symbol)
                : query.OrderBy(t => t.Asset!.Symbol),
            "amount" => sortDescending
                ? query.OrderByDescending(t => t.TotalAmount)
                : query.OrderBy(t => t.TotalAmount),
            "type" => sortDescending
                ? query.OrderByDescending(t => t.Type)
                : query.OrderBy(t => t.Type),
            _ => sortDescending
                ? query.OrderByDescending(t => t.Date)
                : query.OrderBy(t => t.Date)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Transaction>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<(bool Success, string Message)> CreateAsync(Transaction transaction)
    {
        var asset = await dbContext.Assets.FirstOrDefaultAsync(a => a.Id == transaction.AssetId);
        if (asset is null)
        {
            return (false, "Actif introuvable.");
        }

        var budget = await dbContext.PortfolioBudgets.FirstOrDefaultAsync();
        if (budget is null)
        {
            budget = new PortfolioBudget
            {
                InitialBudget = 10_000m,
                AvailableCash = 10_000m
            };
            dbContext.PortfolioBudgets.Add(budget);
        }

        transaction.TotalAmount = transaction.Quantity * transaction.UnitPrice;
        transaction.Date = transaction.Date == default ? DateTime.UtcNow : transaction.Date;

        if (transaction.Type == TransactionType.Buy)
        {
            if (budget.AvailableCash < transaction.TotalAmount)
            {
                return (false, "Liquidite insuffisante pour cet achat.");
            }
            budget.AvailableCash -= transaction.TotalAmount;
        }
        else
        {
            var quantityOwned = await GetOwnedQuantityAsync(transaction.AssetId);
            if (quantityOwned < transaction.Quantity)
            {
                return (false, "Quantite insuffisante pour cette vente.");
            }
            budget.AvailableCash += transaction.TotalAmount;
        }

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();
        await TouchPortfolioHistoryAsync();
        return (true, "Transaction enregistree.");
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var transaction = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id);
        if (transaction is null)
        {
            return false;
        }

        var budget = await dbContext.PortfolioBudgets.FirstOrDefaultAsync();
        if (budget is not null)
        {
            if (transaction.Type == TransactionType.Buy)
            {
                budget.AvailableCash += transaction.TotalAmount;
            }
            else
            {
                budget.AvailableCash = Math.Max(0, budget.AvailableCash - transaction.TotalAmount);
            }
        }

        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync();
        await TouchPortfolioHistoryAsync();
        return true;
    }

    private async Task TouchPortfolioHistoryAsync()
    {
        var snap = await portfolioService.GetSnapshotAsync();
        await portfolioService.RecordDailyPortfolioValueAsync(snap.TotalValue);
    }

    private async Task<decimal> GetOwnedQuantityAsync(int assetId)
    {
        var buys = await dbContext.Transactions
            .Where(t => t.AssetId == assetId && t.Type == TransactionType.Buy)
            .SumAsync(t => t.Quantity);

        var sells = await dbContext.Transactions
            .Where(t => t.AssetId == assetId && t.Type == TransactionType.Sell)
            .SumAsync(t => t.Quantity);

        return buys - sells;
    }
}
