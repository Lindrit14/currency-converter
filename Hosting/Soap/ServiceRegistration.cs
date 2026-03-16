using currency_converter.Application;
using currency_converter.Application.ConvertCurrency;
using currency_converter.Application.GetRateMetadata;
using currency_converter.Application.GetSupportedCurrencies;
using currency_converter.Configuration;
using currency_converter.Infrastructure.BackgroundJobs;
using currency_converter.Infrastructure.Ecb;
using currency_converter.Infrastructure.Persistence;

namespace currency_converter.Hosting.Soap;

public static class ServiceRegistration
{
    public static IServiceCollection AddCurrencyConverterServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<EcbOptions>(configuration.GetSection(EcbOptions.SectionName));
        services.Configure<SecurityOptions>(configuration.GetSection(SecurityOptions.SectionName));

        // Infrastructure - singleton cache
        var cache = new RateSnapshotCache();
        services.AddSingleton(cache);
        services.AddSingleton<IRateSnapshotProvider>(cache);

        // Infrastructure - ECB
        services.AddHttpClient("Ecb");
        services.AddSingleton<EcbClient>();
        services.AddSingleton<SnapshotStore>();

        // Background refresh
        services.AddHostedService<RatesRefreshService>();

        // Application handlers
        services.AddTransient<ConvertCurrencyHandler>();
        services.AddTransient<GetSupportedCurrenciesHandler>();
        services.AddTransient<GetRateMetadataHandler>();

        // SOAP service
        services.AddTransient<CurrencyConverterSoapService>();

        return services;
    }
}
