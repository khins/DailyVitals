# Demo Mode

## Purpose

Demo Mode provides a safe, portfolio-ready account that can explore DailyVitals without changing data or consuming OpenAI API credits.

Default credentials:

- Email: `demo@activevitals.app`
- Password: `Demo123!`

These credentials are intentionally public and must never be reused for a personal or administrative account.

## Configuration

Demo Mode is configured in `DailyVitals.Web/appsettings.json`:

```json
{
  "DemoMode": {
    "Enabled": true,
    "UserName": "demo@activevitals.app",
    "Password": "Demo123!"
  }
}
```

The configured password is converted to the same PBKDF2 password hash used by normal database-backed logins. The plaintext value is not stored in PostgreSQL.

Set `DemoMode:Enabled` to `false` in an environment-specific configuration to disable account creation and seeding.

## Synthetic Data

At application startup, `DemoAccountSeeder` ensures a dedicated `Demo Patient` person and repopulates its data when the seed version or calendar date changes. Dates are relative to `CURRENT_DATE`, keeping weekly and monthly screens useful.

The seed includes:

- 30 blood-pressure readings
- 30 glucose readings
- 30 weight readings
- 15 exercise sessions
- 30 days of food entries with varied sodium and phosphorus patterns
- 30 days of fluid entries
- Four monthly kidney-lab panels
- Personal nutrition and fluid goals
- Renal-friendly and restricted food examples
- One saved AI Nutrition Coach review

The saved coach review uses synthetic content and the model label `demo-seeded`. Demo users cannot generate or refresh AI responses.

## Read-Only Enforcement

Read-only behavior is enforced at several levels:

1. `login_user.is_demo` identifies the account in PostgreSQL.
2. `LocalLoginSession` restores the demo flag from the database rather than trusting browser storage.
3. Pages display a persistent Demo Mode banner and disable write commands.
4. Save, delete, profile, food-estimate, and coach-generation handlers reject demo actions on the server.
5. A PostgreSQL trigger rejects inserts, updates, and deletes for any table containing the demo `person_id`.
6. Password recovery and email changes reject the demo account.

The startup seeder uses a transaction-local `dailyvitals.allow_demo_write` setting to refresh synthetic records before read-only protection resumes.

## Verification

After startup, confirm:

- The demo login has `is_demo = true`.
- The configured password validates against its PBKDF2 hash.
- Each major feature has synthetic records.
- `demo_seed_state.anchor_date` is today.
- A direct write for the demo person fails with `Demo Mode is read-only.`
- A normal account can still create and update its own records.

## Operational Notes

- Seed data is isolated by a dedicated `person_id` and contains no real health information.
- Startup logs a warning and continues if demo initialization fails, preserving access for normal accounts.
- Increment `SeedVersion` in `DemoAccountSeeder` whenever the synthetic dataset shape changes and must be rebuilt.
- New person-owned tables receive protection the next time the seeder runs because triggers are attached dynamically to tables containing `person_id`.
