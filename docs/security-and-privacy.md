# Security and Privacy

## Scope

DailyVitals processes health-related information. This document describes current controls and known gaps; it does not claim HIPAA compliance, medical-device certification, or production security approval.

## Sensitive Data

Treat the following as sensitive:

- Vital readings, weight, exercise, nutrition, fluid intake, and kidney labs
- Food descriptions and AI coaching history
- Login identifiers and password hashes
- PostgreSQL credentials
- OpenAI API keys
- Raw AI requests and responses

Do not place real records or secrets in issues, screenshots, documentation, test fixtures, or commits.

## Current Controls

### Database access

- SQL commands use Npgsql parameters for user-provided values.
- Person-owned operations are generally scoped by `person_id`.
- Database credentials are read from configuration rather than embedded in service source code.

### Password storage

Database-backed local login passwords are derived with PBKDF2 and a per-password salt. Plaintext passwords should never be written to the database or logs.

### Browser session

The web client stores a small login-session record in browser `sessionStorage` or, when Remember this device is selected, `localStorage` with a 30-day expiration. This is a local application convenience, not a server-validated authentication ticket.

### Data protection

ASP.NET Core Data Protection keys are persisted under the web application's `App_Data/Keys` directory. Production deployments must protect and persist this directory outside an ephemeral application image.

### AI boundary

- AI calls use bearer-token authentication over HTTPS.
- The API key is never included in the prompt or stored AI review.
- Strict structured outputs reduce parsing ambiguity.
- The coach receives calculated facts and explicit medical-safety constraints.

## Known Gaps

Before any public or multi-user deployment, address these items:

- Replace the development configuration-login fallback with a production identity provider or secure server-authenticated session.
- Add authorization enforcement at a server/API boundary rather than relying primarily on page state.
- Move every secret to environment-specific secret management.
- Encrypt database connections and storage according to the hosting environment.
- Define AI consent, retention, deletion, and provider-governance policies.
- Add rate limits, account lockout, security logging, and alerting.
- Add cross-person authorization tests for every service operation.
- Establish backup encryption, restore testing, and incident-response procedures.
- Review dependency and container images continuously for vulnerabilities.

## Secret Management

For local work, prefer `OPENAI_API_KEY` over committing a key to `App.config`. Production deployments should use the host's secret manager and inject values at runtime.

Never commit:

- `sk-...` API keys
- PostgreSQL passwords or full production connection strings
- Real production login fallback credentials
- Exported Data Protection keys

If a secret is committed, remove it from active configuration, rotate it immediately, and then address repository history according to the team's incident process.

## AI Data Governance

The AI Coach stores a full local copy of each received response for auditability. This improves traceability but increases retained sensitive data. A production policy should define:

- Who may generate and view reviews
- Which fields may be sent to the provider
- How long snapshots and responses are retained
- How a person requests deletion or export
- Whether provider-side retention meets organizational requirements
- How model and prompt changes are evaluated and approved

## Logging and Screenshots

Avoid logging full prompts, API responses, passwords, keys, or connection strings in production. Portfolio screenshots should use synthetic data and follow the [Screenshot Guide](screenshots/README.md).
