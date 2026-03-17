using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authorization;
using currency_converter.Configuration;

namespace currency_converter.Hosting.Security;

public static class CertificateAuthenticationSetup
{
    public static IServiceCollection AddCertificateAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var securityOptions = configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();
        var allowedThumbprints = securityOptions.AllowedClientCertificateThumbprints
            .Where(static thumbprint => !string.IsNullOrWhiteSpace(thumbprint))
            .Select(NormalizeThumbprint)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate(options =>
            {
                options.AllowedCertificateTypes = CertificateTypes.Chained;
                options.RevocationMode = securityOptions.EnableCertificateRevocationCheck
                    ? System.Security.Cryptography.X509Certificates.X509RevocationMode.Online
                    : System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
                options.ValidateCertificateUse = true;
                options.ValidateValidityPeriod = true;

                options.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        var clientCertificate = context.ClientCertificate;
                        if (clientCertificate is null)
                        {
                            context.Fail("No client certificate was provided.");
                            return Task.CompletedTask;
                        }

                        if (allowedThumbprints.Count == 0)
                        {
                            context.Fail("No allowed client certificate thumbprints are configured.");
                            return Task.CompletedTask;
                        }

                        var normalizedThumbprint = NormalizeThumbprint(clientCertificate.Thumbprint);
                        if (!allowedThumbprints.Contains(normalizedThumbprint))
                        {
                            context.Fail("Client certificate is not authorized for this service.");
                            return Task.CompletedTask;
                        }

                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, clientCertificate.Subject, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                            new Claim(ClaimTypes.Name, clientCertificate.Subject, ClaimValueTypes.String, context.Options.ClaimsIssuer),
                            new Claim("client_cert_thumbprint", normalizedThumbprint, ClaimValueTypes.String, context.Options.ClaimsIssuer)
                        };

                        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                        context.Success();
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.Fail("Invalid client certificate.");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            if (securityOptions.RequireMutualTls)
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
            }
        });

        return services;
    }

    private static string NormalizeThumbprint(string? thumbprint) =>
        (thumbprint ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
}
