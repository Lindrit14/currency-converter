FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj and restore
COPY currency-converter.csproj ./
RUN dotnet restore

# Copy source and publish
COPY . ./
RUN dotnet publish -c Release -o /app --self-contained false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Create data directory for snapshot persistence
RUN mkdir -p /app/Data

COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "currency-converter.dll"]
