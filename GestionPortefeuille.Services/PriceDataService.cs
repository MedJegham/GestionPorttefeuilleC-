using System.Globalization;
using System.Text.Json;
using GestionPortefeuille.Core.Enums;
using GestionPortefeuille.Core.Interfaces;
using GestionPortefeuille.Core.Options;
using GestionPortefeuille.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GestionPortefeuille.Services;

public class PriceDataService(
    HttpClient httpClient,
    AppDbContext dbContext,
    IMemoryCache cache,
    IOptions<PriceDataOptions> options,
    ILogger<PriceDataService> logger) : IPriceDataService
{
    private static readonly Dictionary<string, string> CryptoGeckoIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTC"] = "bitcoin",
        ["ETH"] = "ethereum",
        ["SOL"] = "solana",
        ["ADA"] = "cardano",
        ["DOT"] = "polkadot"
    };

    public async Task<(int UpdatedCount, string Message)> RefreshMarketPricesAsync(CancellationToken cancellationToken = default)
    {
        var opt = options.Value;
        var mode = (opt.Mode ?? "Simulated").Trim();

        logger.LogInformation("Demarrage RefreshMarketPricesAsync (mode={Mode})", mode);

        if (string.Equals(mode, "Simulated", StringComparison.OrdinalIgnoreCase))
        {
            return (0, "Mode simule: les cours ne sont pas mis a jour via API (utilisez l'import CSV ou la saisie manuelle).");
        }

        if (string.Equals(mode, "None", StringComparison.OrdinalIgnoreCase))
        {
            return (0, "Mode None: rafraichissement desactive.");
        }

        if (!string.Equals(mode, "Api", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Mode inconnu pour PriceData: {Mode}", opt.Mode);
            return (0, $"Mode inconnu '{opt.Mode}'. Utilisez Simulated, Api ou None.");
        }

        var assets = await dbContext.Assets.ToListAsync(cancellationToken);
        if (assets.Count == 0)
        {
            logger.LogInformation("Aucun actif a mettre a jour");
            return (0, "Aucun actif a mettre a jour.");
        }

        var updated = 0;
        var cacheMinutes = Math.Max(1, opt.CacheMinutes);

        var cryptoByGecko = assets
            .Where(a => a.AssetType == AssetType.Crypto)
            .Select(a => (Asset: a, GeckoId: CryptoGeckoIds.GetValueOrDefault(a.Symbol)))
            .Where(x => x.GeckoId is not null)
            .GroupBy(x => x.GeckoId!)
            .ToList();

        foreach (var group in cryptoByGecko)
        {
            var id = group.Key;
            var cacheKey = $"gecko:{id}";
            if (!cache.TryGetValue<decimal>(cacheKey, out var usd))
            {
                var url = $"https://api.coingecko.com/api/v3/simple/price?ids={Uri.EscapeDataString(id)}&vs_currencies=usd";
                logger.LogDebug("Appel CoinGecko pour {GeckoId}", id);
                using var response = await httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("CoinGecko a retourne {StatusCode} pour {GeckoId}", (int)response.StatusCode, id);
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                if (!doc.RootElement.TryGetProperty(id, out var idNode) ||
                    !idNode.TryGetProperty("usd", out var usdNode) ||
                    usdNode.ValueKind != JsonValueKind.Number)
                {
                    logger.LogWarning("Reponse CoinGecko invalide pour {GeckoId}", id);
                    continue;
                }

                usd = usdNode.GetDecimal();
                cache.Set(cacheKey, usd, TimeSpan.FromMinutes(cacheMinutes));
            }

            foreach (var item in group)
            {
                item.Asset.CurrentPrice = decimal.Round(usd, 4);
                updated++;
            }
        }

        var key = opt.FinnhubApiKey?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(key))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return (updated, updated > 0
                ? $"{updated} cours mis a jour (crypto). Ajoutez FinnhubApiKey pour les actions."
                : "Aucun cours crypto mis a jour. Ajoutez FinnhubApiKey pour les actions.");
        }

        foreach (var asset in assets.Where(a => a.AssetType == AssetType.Stock))
        {
            var sym = asset.Symbol.Trim().ToUpperInvariant();
            var cacheKey = $"finnhub:{sym}";
            if (cache.TryGetValue<decimal>(cacheKey, out var price))
            {
                asset.CurrentPrice = decimal.Round(price, 4);
                updated++;
                continue;
            }

            var url = $"https://finnhub.io/api/v1/quote?symbol={Uri.EscapeDataString(sym)}&token={Uri.EscapeDataString(key)}";
            logger.LogDebug("Appel Finnhub pour {Symbol}", sym);
            using var response = await httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Finnhub a retourne {StatusCode} pour {Symbol}", (int)response.StatusCode, sym);
                continue;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!doc.RootElement.TryGetProperty("c", out var c) || c.ValueKind != JsonValueKind.Number)
            {
                logger.LogWarning("Reponse Finnhub invalide pour {Symbol}", sym);
                continue;
            }

            var p = c.GetDecimal();
            if (p <= 0)
            {
                logger.LogDebug("Prix Finnhub <= 0 pour {Symbol}, ignore", sym);
                continue;
            }

            cache.Set(cacheKey, p, TimeSpan.FromMinutes(cacheMinutes));
            asset.CurrentPrice = decimal.Round(p, 4);
            updated++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("RefreshMarketPricesAsync termine: {Count} cours mis a jour", updated);
        return (updated, $"{updated} cours mis a jour via API.");
    }

    public async Task<(int UpdatedCount, string? Error)> ImportPricesFromCsvAsync(string csvText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(csvText))
        {
            return (0, "CSV vide.");
        }

        var lines = csvText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            return (0, "Aucune ligne.");
        }

        var sep = lines[0].Contains(';') ? ';' : ',';
        var start = 0;
        var header = lines[0].Split(sep);
        if (header.Length >= 2 &&
            (header[0].Contains("Sym", StringComparison.OrdinalIgnoreCase) ||
             header[0].Contains("symbol", StringComparison.OrdinalIgnoreCase)))
        {
            start = 1;
        }

        var assets = await dbContext.Assets.ToListAsync(cancellationToken);
        var bySymbol = assets.ToDictionary(a => a.Symbol, StringComparer.OrdinalIgnoreCase);
        var updated = 0;

        for (var i = start; i < lines.Length; i++)
        {
            var parts = lines[i].Split(sep, StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var sym = parts[0].Trim().ToUpperInvariant();
            if (!bySymbol.TryGetValue(sym, out var asset))
            {
                continue;
            }

            if (!decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                !decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.CurrentCulture, out price))
            {
                continue;
            }

            if (price <= 0)
            {
                continue;
            }

            asset.CurrentPrice = decimal.Round(price, 4);
            updated++;
        }

        if (updated == 0)
        {
            logger.LogWarning("Import CSV: aucune ligne reconnue ({Lines} lignes parsees)", lines.Length - start);
            return (0, "Aucune ligne reconnue (format: Symbole;Prix ou Symbol,Price).");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Import CSV termine: {Count} prix mis a jour", updated);
        return (updated, null);
    }
}
