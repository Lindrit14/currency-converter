using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using currency_converter.Configuration;

namespace currency_converter.Infrastructure.Ecb;

public sealed class EcbClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EcbOptions _options;
    private readonly ILogger<EcbClient> _logger;

    public EcbClient(
        IHttpClientFactory httpClientFactory,
        IOptions<EcbOptions> options,
        ILogger<EcbClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> FetchRatesXmlAsync(CancellationToken cancellationToken = default)
    {
        using var client = _httpClientFactory.CreateClient("Ecb");
        _logger.LogInformation("Fetching ECB rates from {Url}", _options.FeedUrl);

        var response = await client.GetAsync(_options.FeedUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
