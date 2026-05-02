using GestionPortefeuille.Core.Interfaces;
using GestionPortefeuille.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionPortefeuille.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPortfolioServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AlertOptions>(configuration.GetSection(AlertOptions.SectionName));
        services.Configure<PriceDataOptions>(configuration.GetSection(PriceDataOptions.SectionName));

        services.AddMemoryCache();
        services.AddHttpClient<IPriceDataService, PriceDataService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GestionPortefeuille/1.0 (demo)");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IPortfolioAlertService, PortfolioAlertService>();
        return services;
    }
}
