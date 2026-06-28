# DailyVitals Documentation

This folder describes how DailyVitals is designed, operated, and extended. The documents favor the behavior implemented in the repository over aspirational architecture.

## Guide

| Document | Purpose |
| --- | --- |
| [Architecture](architecture.md) | Solution boundaries, dependencies, and request flows |
| [Database Design](database-design.md) | PostgreSQL ownership, major tables, and schema evolution |
| [Local Development](local-development.md) | Workstation setup, configuration, database creation, and run commands |
| [AI Nutrition Coach](ai-nutrition-coach.md) | AI data flow, grounding, persistence, and safety constraints |
| [Demo Mode](demo-mode.md) | Public credentials, synthetic data, and read-only enforcement |
| [Security and Privacy](security-and-privacy.md) | Current protections, sensitive-data boundaries, and production gaps |
| [Testing Strategy](testing-strategy.md) | Current verification and recommended automated coverage |
| [Deployment](deployment.md) | Production-readiness checklist and deployment topology |
| [Engineering Decisions](decisions/README.md) | Short records explaining important design choices |
| [Screenshot Guide](screenshots/README.md) | Portfolio screenshot standards and redaction rules |

## Documentation Standard

When behavior changes, update the nearest relevant document in the same pull request. Do not include credentials, connection strings, API keys, real patient information, or personal nutrition records in documentation or screenshots.
