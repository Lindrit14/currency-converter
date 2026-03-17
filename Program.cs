using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Options;
using currency_converter.Configuration;
using currency_converter.Contracts;
using currency_converter.Hosting.HealthChecks;
using currency_converter.Hosting.Security;
using currency_converter.Hosting.Soap;

var builder = WebApplication.CreateBuilder(args);
var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ClientCertificateMode = securityOptions.RequireMutualTls
            ? ClientCertificateMode.RequireCertificate
            : ClientCertificateMode.AllowCertificate;
    });
});

// Application Insights telemetry (only when a connection string is configured)
var aiConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(aiConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

// CoreWCF services
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();

// Currency converter services (DI, handlers, infrastructure, background jobs)
builder.Services.AddCurrencyConverterServices(builder.Configuration);

// mTLS authentication
builder.Services.AddCertificateAuth(builder.Configuration);
builder.Services.AddCertificateForwarding(options =>
{
    options.CertificateHeader = "X-ARR-ClientCert";
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<RateSnapshotHealthCheck>("exchange-rates");

// Forwarded headers (Azure App Service / reverse proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Forwarded headers must come first so scheme/host are correct for HTTPS redirect and WSDL URLs
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCertificateForwarding();
app.UseAuthentication();
app.UseAuthorization();

if (securityOptions.RequireMutualTls)
{
    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/CurrencyConverterService.svc", StringComparison.OrdinalIgnoreCase),
        branch => branch.Use(async (context, next) =>
        {
            if (!(context.User.Identity?.IsAuthenticated ?? false))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next();
        }));
}

// Health check endpoint
app.MapHealthChecks("/health").AllowAnonymous();

// Apply configured max request body size globally.
app.Use(async (context, next) =>
{
    var maxRequestBodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (maxRequestBodySizeFeature is not null && !maxRequestBodySizeFeature.IsReadOnly)
    {
        var configuredSize = app.Services.GetRequiredService<IOptions<SecurityOptions>>().Value.MaxRequestBodySize;
        maxRequestBodySizeFeature.MaxRequestBodySize = configuredSize;
    }

    await next();
});

// Resolve base address for CoreWCF WSDL generation
// In Azure: use WEBSITE_HOSTNAME; locally: use configured URLs
var baseAddress = ResolveBaseAddress(builder.Configuration);
var useHttps = baseAddress.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

// CoreWCF SOAP endpoint
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<CurrencyConverterSoapService>(options =>
    {
        options.DebugBehavior.IncludeExceptionDetailInFaults = app.Environment.IsDevelopment();
        options.BaseAddresses.Add(baseAddress);
    });

    serviceBuilder.AddServiceEndpoint<CurrencyConverterSoapService, ICurrencyConverterService>(
        BuildSoapBinding(useHttps),
        "/CurrencyConverterService.svc");

    var metadata = app.Services.GetRequiredService<CoreWCF.Description.ServiceMetadataBehavior>();
    metadata.HttpGetEnabled = !useHttps;
    metadata.HttpsGetEnabled = useHttps;
});

app.Run();

static Uri ResolveBaseAddress(IConfiguration configuration)
{
    // Azure App Service sets WEBSITE_HOSTNAME (e.g. "myapp.azurewebsites.net")
    var azureHost = configuration["WEBSITE_HOSTNAME"];
    if (!string.IsNullOrEmpty(azureHost))
        return new Uri($"https://{azureHost}");

    // Local development: choose HTTPS URL when multiple URLs are configured.
    var configuredUrls = configuration["ASPNETCORE_URLS"] ?? configuration["urls"];
    if (!string.IsNullOrWhiteSpace(configuredUrls))
    {
        var candidateUris = configuredUrls
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null)
            .Where(static uri => uri is not null)
            .Select(static uri => uri!)
            .ToArray();

        var httpsUri = candidateUris.FirstOrDefault(static uri => uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        if (httpsUri is not null)
            return httpsUri;

        var httpUri = candidateUris.FirstOrDefault(static uri => uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));
        if (httpUri is not null)
            return httpUri;
    }

    return new Uri("https://localhost:7061");
}

static BasicHttpBinding BuildSoapBinding(bool useTransportSecurity)
{
    var binding = new BasicHttpBinding();
    if (useTransportSecurity)
    {
        binding.Security.Mode = CoreWCF.Channels.BasicHttpSecurityMode.Transport;
        binding.Security.Transport.ClientCredentialType = CoreWCF.HttpClientCredentialType.Certificate;
    }
    return binding;
}

// Make the implicit Program class accessible for integration tests
public partial class Program { }
