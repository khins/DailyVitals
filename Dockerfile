# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build

WORKDIR /src

# Copy project files first for better restore caching
COPY DailyVitals.Domain/DailyVitals.Domain.csproj DailyVitals.Domain/
COPY DailyVitals.Data/DailyVitals.Data.csproj DailyVitals.Data/
COPY DailyVitals.Web/DailyVitals.Web.csproj DailyVitals.Web/

RUN dotnet restore DailyVitals.Web/DailyVitals.Web.csproj

# Copy the remaining source
COPY . .

RUN dotnet publish DailyVitals.Web/DailyVitals.Web.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

# The Linux SDK publish currently omits the Blazor browser runtime from the
# output even though it is present in the restored ASP.NET internal-assets
# package. Copy it explicitly, then fail the image build if it is unavailable.
RUN set -eu; \
    runtime_asset="$(find /root/.nuget/packages/microsoft.aspnetcore.app.internal.assets \
        -type f \
        -path '*/_framework/blazor.web.js' \
        | sort -V \
        | tail -n 1)"; \
    test -n "$runtime_asset"; \
    mkdir -p /app/publish/wwwroot/_framework; \
    cp "$runtime_asset" /app/publish/wwwroot/_framework/blazor.web.js; \
    test -s /app/publish/wwwroot/_framework/blazor.web.js

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0.10 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet DailyVitals.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]
