# DailyVitals

DailyVitals is a personal health-tracking application with a kidney-care focus. It supports blood pressure, glucose, weight, exercise, nutrition, fluid intake, renal-friendly foods, and kidney lab tracking across a Blazor web application and a Windows WPF client.

The project also includes an AI-assisted nutrition workflow. Food estimates return structured nutrient data and a renal-friendly rating, while the Reports page can generate a grounded weekly nutrition review from calculations performed by the application.

## Technology

- .NET 10
- Blazor Interactive Server
- Windows Presentation Foundation (WPF)
- PostgreSQL with Npgsql
- OpenAI Responses API with strict structured outputs

## Solution Layout

| Project | Responsibility |
| --- | --- |
| `DailyVitals.Domain` | Shared domain models and contracts |
| `DailyVitals.Data` | PostgreSQL access, application services, and AI integrations |
| `DailyVitals.Web` | Blazor web user interface |
| `DailyVitalsApp` | Windows WPF user interface |
| `database` | Baseline DDL, table scripts, functions, and incremental migrations |
| `analytics` | Analytics notebooks and related exploration |
| `tools` | One-off support utilities |

## Documentation

- [Architecture](docs/architecture.md)
- [Database Design](docs/database-design.md)
- [Local Development](docs/local-development.md)
- [AI Nutrition Coach](docs/ai-nutrition-coach.md)
- [Demo Mode](docs/demo-mode.md)
- [Security and Privacy](docs/security-and-privacy.md)
- [Testing Strategy](docs/testing-strategy.md)
- [Deployment](docs/deployment.md)
- [Engineering Decisions](docs/decisions/README.md)

## Important Notice

DailyVitals is intended for personal tracking and awareness. It is not a medical device and does not provide diagnosis or treatment advice. Nutrition goals, medication, phosphorus binders, dialysis treatment, and other clinical decisions should be managed with a qualified care team.

See [Local Development](docs/local-development.md) to configure and run the application.
