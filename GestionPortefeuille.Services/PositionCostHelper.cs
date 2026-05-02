using GestionPortefeuille.Core.Entities;
using GestionPortefeuille.Core.Enums;

namespace GestionPortefeuille.Services;

/// <summary>
/// Cout moyen pondere: a l'achat on recalcule le CMP; a la vente le CMP unitaire reste identique
/// sur les titres restants (methode courante pour actions).
/// </summary>
public static class PositionCostHelper
{
    public static (decimal Quantity, decimal UnitCost) FromTransactions(IEnumerable<Transaction> transactions)
    {
        var ordered = transactions
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
            .ToList();

        decimal qty = 0;
        decimal unitCost = 0;

        foreach (var t in ordered)
        {
            if (t.Type == TransactionType.Buy)
            {
                var addQty = t.Quantity;
                var cost = t.TotalAmount;
                var newQty = qty + addQty;
                if (newQty <= 0)
                {
                    qty = 0;
                    unitCost = 0;
                    continue;
                }

                unitCost = (qty * unitCost + cost) / newQty;
                qty = newQty;
            }
            else
            {
                qty -= t.Quantity;
                if (qty <= 0)
                {
                    qty = 0;
                    unitCost = 0;
                }
            }
        }

        return (qty, unitCost);
    }
}
