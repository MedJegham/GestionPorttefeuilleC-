namespace GestionPortefeuille.Core.Interfaces;

public interface IPriceDataService
{
    /// <summary>Rafraichit les cours selon le mode (Api / Simulated / None).</summary>
    Task<(int UpdatedCount, string Message)> RefreshMarketPricesAsync(CancellationToken cancellationToken = default);

    /// <summary>Import CSV: lignes Symbol,Prix ou Symbole,Prix (separateur , ou ;).</summary>
    Task<(int UpdatedCount, string? Error)> ImportPricesFromCsvAsync(string csvText, CancellationToken cancellationToken = default);
}
