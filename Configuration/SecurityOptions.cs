namespace currency_converter.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public long MaxRequestBodySize { get; set; } = 64 * 1024; // 64 KB
    public bool RequireMutualTls { get; set; } = true;
    public bool EnableCertificateRevocationCheck { get; set; } = true;
    public string[] AllowedClientCertificateThumbprints { get; set; } = [];
}
