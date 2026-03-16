# Currency Converter SOAP Service

A SOAP/WSDL web service built with ASP.NET Core and CoreWCF that converts currencies using European Central Bank (ECB) daily exchange rates.

## Features

- **SOAP endpoint** with auto-generated WSDL at `/CurrencyConverterService.svc`
- **Three operations:** `ConvertCurrency`, `GetSupportedCurrencies`, `GetRateMetadata`
- **ECB integration** — fetches daily exchange rates automatically
- **Background refresh** — rates update daily after ECB publication (~16:15 CET)
- **mTLS authentication** — client certificate authentication for service-to-service calls
- **Health check** at `/health`
- **Snapshot persistence** — survives restarts via local JSON file

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Lindrit14/currency-converter.git
cd currency-converter
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Run the service

**HTTP (development):**

```bash
dotnet run --launch-profile http
```

The service will be available at `http://localhost:5140`.

**HTTPS:**

```bash
dotnet run --launch-profile https
```

The service will be available at `https://localhost:7061`.

### 4. Access the WSDL

Once running, the WSDL is published at:

```
http://localhost:5140/CurrencyConverterService.svc?wsdl
```

or via HTTPS:

```
https://localhost:7061/CurrencyConverterService.svc?wsdl
```

## Running Tests

```bash
dotnet test
```

This runs unit tests (domain logic, handlers, XML parsing) and integration tests (SOAP endpoint, snapshot persistence).

## Configuration

Configuration is in `appsettings.json`:

| Section | Key | Description |
|---------|-----|-------------|
| `Ecb` | `FeedUrl` | ECB daily rates XML feed URL |
| `Ecb` | `RefreshHourCet` / `RefreshMinuteCet` | When to refresh rates (default: 16:15 CET) |
| `Ecb` | `SnapshotFilePath` | Path for persisted rate snapshot |
| `Security` | `RequireMutualTls` | Enable/disable client certificate requirement |
| `Security` | `MaxRequestBodySize` | Max request body in bytes (default: 65536) |
| `Security` | `AllowedClientCertificateThumbprints` | Trusted client certificate thumbprints |

For local development, mTLS can be disabled by setting `RequireMutualTls` to `false` in `appsettings.Development.json`.

## Docker

```bash
docker build -t currency-converter .
docker run -p 8080:8080 currency-converter
```

The service will be available at `http://localhost:8080`.

## Project Structure

```
├── Contracts/           # SOAP service contract and data models
├── Application/         # Use-case handlers
├── Domain/              # Core domain logic (conversion, exchange rates)
├── Infrastructure/      # ECB client, XML parsing, persistence, background jobs
├── Hosting/             # SOAP setup, authentication, health checks
├── Configuration/       # Options classes
├── currency-converter.Tests/  # xUnit test suite
├── Program.cs           # Application entry point
└── Dockerfile
```

## SOAP Operations

### ConvertCurrency

Converts an amount from one currency to another using ECB cross-rates.

- **Input:** `Amount`, `FromCurrency`, `ToCurrency`
- **Output:** `OriginalAmount`, `ConvertedAmount`, `ExchangeRate`, `FromCurrency`, `ToCurrency`, `RateDate`, `Stale`

### GetSupportedCurrencies

Returns all currency codes available from the ECB feed.

### GetRateMetadata

Returns rate source info, last refresh time, rate date, and staleness indicator.
