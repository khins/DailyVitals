# Local Development

## Prerequisites

- Windows for the WPF client
- .NET 10 SDK
- PostgreSQL
- Git
- An OpenAI API key with API billing enabled for AI features

The Blazor web project can be developed independently of the WPF client.

## Clone and Restore

```powershell
git clone https://github.com/khins/DailyVitals.git
cd DailyVitals
dotnet restore DailyVitals.slnx
```

## Database Setup

Create a local PostgreSQL database and apply the baseline DDL:

```powershell
createdb -U postgres dailyvitals
psql -U postgres -d dailyvitals -f "database/one complete DDL script.sql"
```

Apply the scripts in `database/migrations` in filename order. Review a migration before running it against an existing database and take a backup first.

Some newer services create supporting tables or columns when first used. This behavior helps local upgrades, but the long-term direction is to represent every schema change as a migration.

## Local Configuration

The shared data layer reads a connection string named `DailyVitals` through `System.Configuration.ConfigurationManager`. Keep machine-specific values out of commits.

Use this shape in the client project's local `App.config`, substituting your own values:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <connectionStrings>
    <add name="DailyVitals"
         connectionString="Host=localhost;Port=5432;Database=dailyvitals;Username=postgres;Password=YOUR_LOCAL_PASSWORD" />
  </connectionStrings>
  <appSettings>
    <add key="OpenAiModel" value="gpt-5.4-mini" />
  </appSettings>
</configuration>
```

Prefer an environment variable for the OpenAI key:

```powershell
$env:OPENAI_API_KEY = "YOUR_API_KEY"
$env:OPENAI_MODEL = "gpt-5.4-mini"
```

The code also supports an `OpenAiApiKey` application setting for local use, but API keys must never be committed.

The web login can be seeded for local development through `DailyVitalsLogin:UserName` and `DailyVitalsLogin:Password` configuration. Use development-only values and do not carry this fallback into production.

## Build

```powershell
dotnet build DailyVitals.slnx
```

If a running web process locks its normal output files, validate to a temporary output directory:

```powershell
dotnet build DailyVitals.Web\DailyVitals.Web.csproj -o "$env:TEMP\DailyVitalsWebBuild"
```

## Run the Web Client

```powershell
dotnet run --project DailyVitals.Web\DailyVitals.Web.csproj
```

Use the local URL printed by ASP.NET Core. Sign in with the development login connected to your local person record.

## Run the WPF Client

```powershell
dotnet run --project DailyVitalsApp\DailyVitals.App.csproj
```

## AI Feature Checks

The food Estimate button and AI Nutrition Coach require the OpenAI key. Before testing with real records, understand that the selected food description or calculated weekly nutrition snapshot is transmitted to OpenAI.

Useful local checks:

1. Estimate a clearly described serving and confirm all nutrient fields populate.
2. Verify the renal rating and reason appear in Food Notes.
3. Generate a weekly coach review and confirm logged-day counts match Reports.
4. Refresh the review and confirm a new `nutrition_coach_review` row is added.
5. Remove the API key temporarily and confirm the UI reports a useful configuration error.

## Troubleshooting

### PostgreSQL password authentication failed

Confirm the host, port, database, username, and password in the active client's generated configuration. PostgreSQL instances on ports `5432` and `5433` are different servers and may have different credentials.

### OpenAI quota error

API billing and ChatGPT subscriptions are separate. Confirm the API project has active billing, available credits or budget, and access to the configured model.

### Local configuration appears in Git status

Do not commit it. Keep the file local, or move sensitive values to environment variables or an approved secret provider before deployment.
