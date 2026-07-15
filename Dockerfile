# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

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

# Do not produce a deployable image if the client runtime required by every
# interactive Razor component was omitted from the publish output.
RUN test -f /app/publish/wwwroot/_framework/blazor.web.js

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet DailyVitals.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]
