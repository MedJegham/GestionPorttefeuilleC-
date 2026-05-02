using GestionPortefeuille.Core.Entities;
using GestionPortefeuille.Core.Enums;
using GestionPortefeuille.Core.Interfaces;
using GestionPortefeuille.Core.Models;
using GestionPortefeuille.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionPortefeuille.Services;

public class PortfolioService(AppDbContext dbContext) : IPortfolioService
{
    public async Task<PortfolioSnapshot> GetSnapshotAsync()
    {
        var budget = await GetBudgetAsync();
        var assets = await dbContext.Assets.AsNoTracking().ToListAsync();
        var transactions = await dbContext.Transactions.AsNoTracking().ToListAsync();

        var holdings = new List<HoldingSummary>();
        foreach (var asset in assets)
        {
            var assetTransactions = transactions.Where(t => t.AssetId == asset.Id).ToList();
            var (qty, unitCost) = PositionCostHelper.FromTransactions(assetTransactions);
            if (qty <= 0)
            {
                continue;
            }

            var costBasis = qty * unitCost;
            var marketValue = qty * asset.CurrentPrice;

            holdings.Add(new HoldingSummary
            {
                AssetId = asset.Id,
                AssetType = asset.AssetType,
                Symbol = asset.Symbol,
                Name = asset.Name,
                Quantity = qty,
                AverageBuyPrice = unitCost,
                CostBasis = costBasis,
                CurrentPrice = asset.CurrentPrice,
                MarketValue = marketValue,
                GainLoss = marketValue - costBasis
            });
        }

        var portfolioValue = holdings.Sum(h => h.MarketValue);
        foreach (var holding in holdings)
        {
            holding.AllocationPercent = portfolioValue <= 0 ? 0 : holding.MarketValue / portfolioValue * 100;
        }

        var allocationByType = Enum.GetValues<AssetType>()
            .ToDictionary(t => t, t => holdings.Where(h => h.AssetType == t).Sum(h => h.MarketValue));
        foreach (var key in allocationByType.Keys.ToList())
        {
            allocationByType[key] = portfolioValue <= 0 ? 0 : allocationByType[key] / portfolioValue * 100;
        }

        var totalValue = budget.AvailableCash + portfolioValue;
        var gainLoss = totalValue - budget.InitialBudget;
        var roi = budget.InitialBudget <= 0 ? 0 : gainLoss / budget.InitialBudget * 100;

        return new PortfolioSnapshot
        {
            InitialBudget = budget.InitialBudget,
            AvailableCash = budget.AvailableCash,
            InvestedAmount = portfolioValue,
            PortfolioValue = portfolioValue,
            TotalValue = totalValue,
            GainLoss = gainLoss,
            RoiPercent = roi,
            Holdings = holdings.OrderByDescending(h => h.MarketValue).ToList(),
            AllocationByType = allocationByType
        };
    }

    public async Task<PortfolioBudget> GetBudgetAsync()
    {
        var budget = await dbContext.PortfolioBudgets.FirstOrDefaultAsync();
        if (budget is null)
        {
            budget = new PortfolioBudget
            {
                InitialBudget = 10_000m,
                AvailableCash = 10_000m
            };
            dbContext.PortfolioBudgets.Add(budget);
            await dbContext.SaveChangesAsync();
        }

        return budget;
    }

    public async Task UpdateBudgetAsync(decimal initialBudget, decimal availableCash)
    {
        var budget = await GetBudgetAsync();
        budget.InitialBudget = initialBudget;
        budget.AvailableCash = availableCash;
        await dbContext.SaveChangesAsync();
    }

    public async Task RecordDailyPortfolioValueAsync(decimal totalValue, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var existing = await dbContext.PortfolioValuePoints
            .FirstOrDefaultAsync(p => p.Date == today, cancellationToken);

        if (existing is null)
        {
            dbContext.PortfolioValuePoints.Add(new PortfolioValuePoint
            {
                Date = today,
                TotalValue = totalValue
            });
        }
        else
        {
            existing.TotalValue = totalValue;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PortfolioValuePoint>> GetPortfolioValueHistoryAsync(int maxPoints = 365, CancellationToken cancellationToken = default)
    {
        var chunk = await dbContext.PortfolioValuePoints
            .AsNoTracking()
            .OrderByDescending(p => p.Date)
            .Take(maxPoints)
            .ToListAsync(cancellationToken);

        chunk.Sort((a, b) => a.Date.CompareTo(b.Date));
        return chunk;
    }
}
