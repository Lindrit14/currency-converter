Yes — for **.NET**, the language you would normally use is **C#**. Microsoft documents C# as the programming language, while .NET is the platform/runtime around it. ([Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/overview?utm_source=chatgpt.com))

For **your part only** — the Currency Converter SOAP service — I would build it like this:

- **C#**
- **ASP.NET Core** as the host
- SOAPCore for the SOAP/WSDL endpoint
- **IHttpClientFactory** for calling the ECB feed
- **BackgroundService / IHostedService** for the daily refresh job
- **Options pattern** for config
- **ASP.NET Core authentication** for caller authentication
- **xUnit** for tests

## The architecture for your service

Keep the service very small.

### 1. SOAP endpoint layer

This is the public face of your service.

Use **CoreWCF** with a small SOAP contract such as:

- `ConvertCurrency(amount, fromCurrency, toCurrency)`
- `GetSupportedCurrencies()`
- `GetRateMetadata()`

CoreWCF can generate and publish WSDL, and the metadata/WSDL exposure is enabled through `AddServiceModelMetadata()`. ([CoreWCF](https://corewcf.github.io/blog/2022/04/26/wsdl))

### 2. Application layer

This orchestrates use cases, for example:

- `ConvertCurrencyHandler`
- `GetSupportedCurrenciesHandler`
- `GetRateMetadataHandler`

No XML parsing here, no HTTP details here, no SOAP details here.

### 3. Domain layer

This is the important part and should stay pure.

Classes like:

- `ExchangeRateSnapshot`
- `CurrencyCode`
- `ConversionService`

The cross-rate formula stays here:

[convertedAmount = amount \times \frac{toRate}{fromRate}]

because ECB publishes rates against **EUR** as the base currency. The ECB reference-rate page explicitly states that all listed currencies are quoted against the euro. ([European Central Bank](https://www.ecb.europa.eu/stats/policy_and_exchange_rates/euro_reference_exchange_rates/html/index.en.html))

### 4. Infrastructure layer

This handles:

- downloading the ECB XML
- parsing the XML
- caching the latest snapshot
- persisting the snapshot locally
- scheduled refresh
- logging

For the HTTP call, use `IHttpClientFactory`; Microsoft recommends it as the standard way to create and manage `HttpClient` instances with DI, logging, and configuration. For the refresh task, use a hosted background service. ([Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory))

## The external data source

Your service should fetch the ECB XML from the official daily feed over **HTTPS**, not by scraping HTML. The ECB says the euro reference rates are usually updated around **16:00 CET every working day**, and they are published for information purposes only. ([European Central Bank](https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml?utm_source=chatgpt.com))

So the runtime behavior should be:

- fetch once on startup
- refresh once daily after publication time, for example at 16:15 CET
- if refresh fails, keep serving the last successful snapshot
- mark the snapshot as stale if needed

This is exactly the kind of background work ASP.NET Core hosted services are meant for. ([European Central Bank](https://www.ecb.europa.eu/stats/policy_and_exchange_rates/euro_reference_exchange_rates/html/index.en.html))

## Authentication

explicitly **authentication, not authorization**.

ASP.NET Core defines authentication as determining identity, while authorization is deciding access. So for your service, the first question is only: **is the Central Car Rental Service really the caller → Yeas it is supposed to be** 

For this project in .NET, I would recommend:

### Best practical choice

**HTTPS + client certificate authentication (mTLS)**

Why this is the best fit:

- it is strong service-to-service authentication
- it keeps credentials out of the SOAP body
- it is well supported by ASP.NET Core
- it is simpler and cleaner than complicated SOAP-level WS-Security setups

**Use mTLS unless the assignment explicitly says “authenticate with username/password in SOAP headers.”**

## Security plan from day one

Your service should have these rules:

- **HTTPS only**
- request body size limit
- strict currency-code validation
- strict amount validation
- no hardcoded secrets
- structured logging
- sanitized SOAP faults
- rate limiting for bad requests or brute-force attempts

ASP.NET Core has built-in guidance and middleware for authentication, secret handling, HTTPS, and rate limiting. Microsoft’s docs recommend storing development secrets outside source code with Secret Manager, and ASP.NET Core includes rate-limiting middleware.

## Keep persistence simple

You do **not** need a database unless your assignment explicitly requires one.

For this service, I would use:

- in-memory cache for current runtime use
- one local JSON file as the persisted last successful snapshot

That gives you:

- fast lookups
- simple restart recovery
- less complexity
- easier testing

For a student project, that is the right tradeoff.

## Clean project structure

Something like this:

```
CurrencyConverterService/
  Contracts/
    ICurrencyConverterService.cs
    Models/

  Application/
    ConvertCurrency/
    GetSupportedCurrencies/
    GetRateMetadata/

  Domain/
    ExchangeRateSnapshot.cs
    ConversionService.cs
    CurrencyCode.cs

  Infrastructure/
    Ecb/
      EcbClient.cs
      EcbXmlParser.cs
    Persistence/
      SnapshotStore.cs
    BackgroundJobs/
      RatesRefreshService.cs

  Hosting/
    Soap/
      SoapFaultMapper.cs
      ServiceRegistration.cs
    Security/
      CertificateAuthenticationSetup.cs

  Configuration/
    EcbOptions.cs
    SecurityOptions.cs

  Program.cs
```

## SOAP contract I would expose

Keep it tiny.

### `ConvertCurrency`

Input:

- amount
- fromCurrency
- toCurrency

Output:

- originalAmount
- convertedAmount
- exchangeRate
- fromCurrency
- toCurrency
- rateDate
- stale

### `GetSupportedCurrencies`

Output:

- currency codes
- rateDate

### `GetRateMetadata`

Output:

- rateDate
- source = ECB
- lastRefreshUtc
- stale

That is enough. No need to build an enterprise FX platform.

## Testing strategy

This service should be test-heavy in the domain and light in infrastructure.

### Unit tests

Most important:

- EUR → USD
- USD → EUR
- JPY → GBP cross-rate
- same-currency conversion
- invalid currency
- zero / negative amount
- stale snapshot logic
- rounding rules

### Integration tests

Also important:

- SOAP request returns correct SOAP response
- invalid input returns SOAP fault
- auth failure returns auth fault
- mocked ECB XML refresh works
- restart with saved snapshot works

### Contract tests

Since another service calls you:

- WSDL is published
- request/response schema stays stable
- faults are predictable

## What I would actually choose

This is the concrete setup I would use:

- **Language:** C#
- **Runtime:** .NET 8 LTS
- **Host:** ASP.NET Core
- **SOAP/WSDL:** CoreWCF
- **Auth:** HTTPS + client certificate authentication inherited from ASP.NET Core
- **HTTP to ECB:** `IHttpClientFactory`
- **Refresh:** `BackgroundService`
- **Config:** Options pattern
- **Secrets in dev:** Secret Manager
- **Tests:** xUnit