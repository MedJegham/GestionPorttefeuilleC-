using GestionPortefeuille.Core.Entities;
using GestionPortefeuille.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionPortefeuille.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.PortfolioBudgets.AnyAsync())
        {
            return;
        }

        db.PortfolioBudgets.Add(new PortfolioBudget
        {
            InitialBudget = 10_000m,
            AvailableCash = 10_000m
        });

        db.Assets.AddRange(
            new Asset { Symbol = "AAPL", Name = "Apple Inc.", AssetType = AssetType.Stock, CurrentPrice = 185m },
            new Asset { Symbol = "MSFT", Name = "Microsoft", AssetType = AssetType.Stock, CurrentPrice = 420m },
            new Asset { Symbol = "BTC", Name = "Bitcoin", AssetType = AssetType.Crypto, CurrentPrice = 62_000m },
            new Asset { Symbol = "ETH", Name = "Ethereum", AssetType = AssetType.Crypto, CurrentPrice = 3_200m }
        );

        await db.SaveChangesAsync();
    }
}
