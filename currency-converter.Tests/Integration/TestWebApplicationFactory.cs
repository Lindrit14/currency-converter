using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using currency_converter.Domain;
using currency_converter.Infrastructure.BackgroundJobs;
using currency_converter.Infrastructure.Persistence;

namespace currency_converter.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set a URL so CoreWCF can resolve its base address in tests
        builder.UseSetting("urls", "http://localhost:5140");

        // Disable Application Insights in tests (empty = skipped in Program.cs)
        builder.UseSetting("ApplicationInsights:ConnectionString", "");

        // Disable mTLS and cert-based auth for integration tests
        builder.UseSetting("Security:RequireMutualTls", "false");
        builder.UseSetting("Security:EnableCertificateRevocationCheck", "false");
        builder.UseSetting("Security:AllowedClientCertificateThumbprints:0", "");

        builder.ConfigureServices(services =>
        {
            // Remove the real background service so it doesn't call ECB
            var descriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(RatesRefreshService));
            if (descriptor is not null)
                services.Remove(descriptor);

            // Replace authentication with a test scheme that auto-succeeds
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

            // Override authorization to use the Test scheme
            services.AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder("Test")
                    .RequireAuthenticatedUser()
                    .Build())
                .SetFallbackPolicy(new AuthorizationPolicyBuilder("Test")
                    .RequireAuthenticatedUser()
                    .Build());
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Seed the cache after the host is built
        var cache = host.Services.GetRequiredService<RateSnapshotCache>();
        var rates = new Dictionary<CurrencyCode, decimal>
        {
            [new CurrencyCode("USD")] = 1.1m,
            [new CurrencyCode("GBP")] = 0.85m,
            [new CurrencyCode("JPY")] = 160.0m,
        };
        cache.Update(new ExchangeRateSnapshot(
            DateOnly.FromDateTime(DateTime.UtcNow),
            rates,
            DateTime.UtcNow));

        return host;
    }
}
