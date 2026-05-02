namespace GestionPortefeuille.Core.Options;

public class PriceDataOptions
{
    public const string SectionName = "PriceData";

    /// <summary>Simulated (defaut), Api (Finnhub + CoinGecko), None.</summary>
    public string Mode { get; set; } = "Simulated";

    public string FinnhubApiKey { get; set; } = string.Empty;

    public int CacheMinutes { get; set; } = 15;
}
