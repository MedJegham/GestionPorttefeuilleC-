using GestionPortefeuille.Core.Entities;
using GestionPortefeuille.Core.Enums;
using GestionPortefeuille.Core.Interfaces;
using GestionPortefeuille.Core.Models;
using GestionPortefeuille.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionPortefeuille.Services;

public class AnalyticsService(AppDbContext dbContext) : IAnalyticsService
{
    private const int TradingDaysPerYear = 252;

    public async Task<List<AssetAnalytics>> GetAssetAnalyticsAsync(int smaPeriod = 20)
    {
        var assets = await dbContext.Assets.AsNoTracking().OrderBy(a => a.Symbol).ToListAsync();
        var analytics = new List<AssetAnalytics>();

        foreach (var asset in assets)
        {
            var history = await dbContext.PriceHistories
                .AsNoTracking()
                .Where(p => p.AssetId == asset.Id)
                .OrderBy(p => p.Date)
                .ToListAsync();

            if (history.Count < 2)
            {
                analytics.Add(new AssetAnalytics
                {
                    AssetId = asset.Id,
                    Symbol = asset.Symbol,
                    CurrentPrice = asset.CurrentPrice,
                    SimpleMovingAverage = asset.CurrentPrice,
                    VolatilityPercent = 0,
                    Signal = TrendSignal.Neutral,
                    SharpeLikeAnnualized = 0,
                    MaxDrawdownPercent = 0
                });
                continue;
            }

            var ordered = history;
            var tailForSma = ordered.TakeLast(Math.Min(smaPeriod, ordered.Count)).Select(x => x.ClosePrice).ToList();
            var sma = tailForSma.Average();
            var dailyVol = CalculateDailyVolatility(ordered);
            var distance = sma == 0 ? 0 : (asset.CurrentPrice - sma) / sma * 100;

            var signal = distance switch
            {
                > 1m => TrendSignal.Bullish,
                < -1m => TrendSignal.Bearish,
                _ => TrendSignal.Neutral
            };

            var returns = BuildDailyReturns(ordered);
            var sharpe = CalculateSharpeLikeAnnualized(returns);
            var maxDd = CalculateMaxDrawdownPercent(ordered.Select(h => h.ClosePrice).ToList());

            analytics.Add(new AssetAnalytics
            {
                AssetId = asset.Id,
                Symbol = asset.Symbol,
                CurrentPrice = asset.CurrentPrice,
                SimpleMovingAverage = decimal.Round(sma, 4),
                VolatilityPercent = decimal.Round(dailyVol * (decimal)Math.Sqrt(TradingDaysPerYear) * 100, 2),
                Signal = signal,
                DistanceToSmaPercent = decimal.Round(distance, 2),
                SharpeLikeAnnualized = decimal.Round(sharpe, 3),
                MaxDrawdownPercent = decimal.Round(maxDd, 2)
            });
        }

        return analytics;
    }

    public async Task EnsurePriceHistorySeedAsync(int days = 90)
    {
        var assets = await dbContext.Assets.AsNoTracking().ToListAsync();
        if (assets.Count == 0)
        {
            return;
        }

        var startDate = DateTime.UtcNow.Date.AddDays(-days);

        foreach (var asset in assets)
        {
            var hasHistory = await dbContext.PriceHistories.AnyAsync(p => p.AssetId == asset.Id);
            if (hasHistory)
            {
                continue;
            }

            var seed = asset.Symbol.Aggregate(17, (acc, ch) => acc * 23 + ch);
            var random = new Random(seed);
            var price = Math.Max(1, (double)asset.CurrentPrice);

            for (var i = 0; i < days; i++)
            {
                var changeBand = asset.AssetType == AssetType.Crypto ? 0.07 : 0.03;
                var dailyReturn = (random.NextDouble() * 2 - 1) * changeBand;
                price = Math.Max(0.2, price * (1 + dailyReturn));

                dbContext.PriceHistories.Add(new PriceHistory
                {
                    AssetId = asset.Id,
                    Date = startDate.AddDays(i),
                    ClosePrice = decimal.Round((decimal)price, 4)
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<BacktestResult?> RunSmaTrendBacktestAsync(int assetId, int smaPeriod = 20, decimal initialCash = 10_000m, CancellationToken cancellationToken = default)
    {
        var asset = await dbContext.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assetId, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var closes = await dbContext.PriceHistories
            .AsNoTracking()
            .Where(p => p.AssetId == assetId)
            .OrderBy(p => p.Date)
            .Select(p => p.ClosePrice)
            .ToListAsync(cancellationToken);

        if (closes.Count < smaPeriod + 1)
        {
            return null;
        }

        var startIdx = smaPeriod - 1;
        var firstClose = closes[startIdx];
        if (firstClose <= 0)
        {
            return null;
        }

        var bhShares = initialCash / firstClose;

        decimal cash = initialCash;
        decimal stratShares = 0;
        var tradeCount = 0;

        for (var i = smaPeriod; i < closes.Count; i++)
        {
            var window = closes.GetRange(i - smaPeriod, smaPeriod);
            var sma = window.Average();
            var price = closes[i];
            if (price <= 0)
            {
                continue;
            }

            if (price > sma && stratShares == 0 && cash > 0)
            {
                stratShares = cash / price;
                cash = 0;
                tradeCount++;
            }
            else if (price < sma && stratShares > 0)
            {
                cash = stratShares * price;
                stratShares = 0;
                tradeCount++;
            }
        }

        var last = closes[^1];
        var finalStrategy = cash + stratShares * last;
        var finalBh = bhShares * last;

        return new BacktestResult
        {
            AssetId = asset.Id,
            Symbol = asset.Symbol,
            SmaPeriod = smaPeriod,
            InitialCash = initialCash,
            FinalStrategyEquity = decimal.Round(finalStrategy, 2),
            FinalBuyHoldEquity = decimal.Round(finalBh, 2),
            TradeCount = tradeCount
        };
    }

    private static decimal CalculateDailyVolatility(List<PriceHistory> orderedHistory)
    {
        var returns = BuildDailyReturns(orderedHistory);
        if (returns.Count <= 1)
        {
            return 0;
        }

        var mean = returns.Average();
        var variance = returns.Sum(r => (r - mean) * (r - mean)) / (returns.Count - 1);
        return (decimal)Math.Sqrt((double)variance);
    }

    private static List<decimal> BuildDailyReturns(List<PriceHistory> orderedHistory)
    {
        var returns = new List<decimal>();
        for (var i = 1; i < orderedHistory.Count; i++)
        {
            var prev = orderedHistory[i - 1].ClosePrice;
            var current = orderedHistory[i].ClosePrice;
            if (prev <= 0)
            {
                continue;
            }

            returns.Add((current - prev) / prev);
        }

        return returns;
    }

    private static decimal CalculateSharpeLikeAnnualized(List<decimal> dailyReturns)
    {
        if (dailyReturns.Count <= 1)
        {
            return 0;
        }

        var mean = dailyReturns.Average();
        var variance = dailyReturns.Sum(r => (r - mean) * (r - mean)) / (dailyReturns.Count - 1);
        var std = (decimal)Math.Sqrt((double)variance);
        if (std <= 0)
        {
            return 0;
        }

        var dailySharpe = mean / std;
        return dailySharpe * (decimal)Math.Sqrt(TradingDaysPerYear);
    }

    private static decimal CalculateMaxDrawdownPercent(List<decimal> prices)
    {
        if (prices.Count < 2)
        {
            return 0;
        }

        decimal peak = prices[0];
        decimal maxDd = 0;
        foreach (var p in prices)
        {
            if (p > peak)
            {
                peak = p;
            }

            if (peak <= 0)
            {
                continue;
            }

            var dd = (p - peak) / peak;
            if (dd < maxDd)
            {
                maxDd = dd;
            }
        }

        return Math.Abs(maxDd) * 100;
    }
}
