using GestionPortefeuille.Core.Entities;
using GestionPortefeuille.Core.Enums;
using GestionPortefeuille.Services;

namespace GestionPortefeuille.Tests;

public class PositionCostHelperTests
{
    private static Transaction Buy(int id, decimal qty, decimal unitPrice, DateTime date)
        => new()
        {
            Id = id,
            Type = TransactionType.Buy,
            Quantity = qty,
            UnitPrice = unitPrice,
            TotalAmount = qty * unitPrice,
            Date = date
        };

    private static Transaction Sell(int id, decimal qty, decimal unitPrice, DateTime date)
        => new()
        {
            Id = id,
            Type = TransactionType.Sell,
            Quantity = qty,
            UnitPrice = unitPrice,
            TotalAmount = qty * unitPrice,
            Date = date
        };

    [Fact]
    public void FromTransactions_AucuneTransaction_RetourneZero()
    {
        var (qty, unitCost) = PositionCostHelper.FromTransactions(Array.Empty<Transaction>());

        Assert.Equal(0m, qty);
        Assert.Equal(0m, unitCost);
    }

    [Fact]
    public void FromTransactions_UnSeulAchat_CmpEgalPrixAchat()
    {
        var transactions = new[]
        {
            Buy(1, qty: 10m, unitPrice: 100m, date: new DateTime(2026, 1, 1))
        };

        var (qty, unitCost) = PositionCostHelper.FromTransactions(transactions);

        Assert.Equal(10m, qty);
        Assert.Equal(100m, unitCost);
    }

    [Fact]
    public void FromTransactions_DeuxAchatsPrixDifferents_CalculeCmpPondere()
    {
        // 10 @ 100 puis 10 @ 200 => CMP = (10*100 + 10*200) / 20 = 150
        var transactions = new[]
        {
            Buy(1, qty: 10m, unitPrice: 100m, date: new DateTime(2026, 1, 1)),
            Buy(2, qty: 10m, unitPrice: 200m, date: new DateTime(2026, 1, 2))
        };

        var (qty, unitCost) = PositionCostHelper.FromTransactions(transactions);

        Assert.Equal(20m, qty);
        Assert.Equal(150m, unitCost);
    }

    [Fact]
    public void FromTransactions_VentePartielle_CmpReste()
    {
        // Achat 10 @ 100 puis vente 4 @ 200 => le CMP des titres restants reste 100
        var transactions = new[]
        {
            Buy(1, qty: 10m, unitPrice: 100m, date: new DateTime(2026, 1, 1)),
            Sell(2, qty: 4m, unitPrice: 200m, date: new DateTime(2026, 1, 2))
        };

        var (qty, unitCost) = PositionCostHelper.FromTransactions(transactions);

        Assert.Equal(6m, qty);
        Assert.Equal(100m, unitCost);
    }

    [Fact]
    public void FromTransactions_VenteTotale_RemetQuantiteEtCmpAZero()
    {
        var transactions = new[]
        {
            Buy(1, qty: 10m, unitPrice: 100m, date: new DateTime(2026, 1, 1)),
            Sell(2, qty: 10m, unitPrice: 200m, date: new DateTime(2026, 1, 2))
        };

        var (qty, unitCost) = PositionCostHelper.FromTransactions(transactions);

        Assert.Equal(0m, qty);
        Assert.Equal(0m, unitCost);
    }

    [Fact]
    public void FromTransactions_RachatApresVenteTotale_CmpRecalculeSurNouvelAchat()
    {
        // 10 @ 100, vente totale, puis rachat 5 @ 250 => CMP = 250 sur 5 titres
        var transactions = new[]
        {
            Buy(1, qty: 10m, unitPrice: 100m, date: new DateTime(2026, 1, 1)),
            Sell(2, qty: 10m, unitPrice: 200m, date: new DateTime(2026, 1, 2)),
            Buy(3, qty: 5m, unitPrice: 250m, date: new DateTime(2026, 1, 3))
        };

        var (qty, unitCost) = PositionCostHelper.FromTransactions(transactions);

        Assert.Equal(5m, qty);
        Assert.Equal(250m, unitCost);
    }

    [Fact]
    public void FromTransactions_TransactionsDansLeMauvaisOrdre_TrieParDateAvantCalcul()
    {
        // Memes transactions que le test pondere mais fournies a l'envers
        var transactions = new[]
        {
            Buy(2, qty: 10m, unitPrice: 200m, date: new DateTime(2026, 1, 2)),
            Buy(1, qty: 10m, unitPrice: 100m, date: new DateTime(2026, 1, 1))
        };

        var (qty, unitCost) = PositionCostHelper.FromTransactions(transactions);

        Assert.Equal(20m, qty);
        Assert.Equal(150m, unitCost);
    }

    [Fact]
    public void FromTransactions_VenteSuperieureAuStock_RemetEtatAZero()
    {
        // Vente non couverte : la position descend a 0, le CMP est remis a 0
        var transactions = new[]
        {
            Buy(1, qty: 5m, unitPrice: 100m, date: new DateTime(2026, 1, 1)),
            Sell(2, qty: 10m, unitPrice: 150m, date: new DateTime(2026, 1, 2))
        };

        var (qty, unitCost) = PositionCostHelper.FromTransactions(transactions);

        Assert.Equal(0m, qty);
        Assert.Equal(0m, unitCost);
    }
}
