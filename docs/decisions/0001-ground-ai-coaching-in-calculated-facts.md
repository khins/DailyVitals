# ADR 0001: Ground AI Coaching in Calculated Facts

- Status: Accepted
- Date: 2026-06-26

## Context

DailyVitals needs to summarize nutrition patterns in approachable language. Statements such as "phosphorus was within goal on five of six logged days" must remain numerically correct, distinguish missing days from successful days, and respect person-specific goals.

Asking a language model to infer totals directly from a list of records would make numeric behavior harder to test and could produce inconsistent compliance claims. Kidney-care guidance also requires clear boundaries around medication, phosphorus binders, and treatment advice.

## Decision

Application code builds a deterministic seven-day `NutritionCoachSnapshot` containing:

- Logged-day coverage
- Personal goals
- Nutrient averages
- Days meeting limits or targets
- Largest nutrient sources
- Binder count as non-calculated context

The snapshot is sent to the OpenAI Responses API with a strict output schema. The model writes only the headline, summary, wins, focus areas, suggested actions, and care-team note.

Every received provider response is stored in an append-only record with the snapshot, raw response, parsed result, model, HTTP status, outcome, and timestamp.

## Consequences

### Positive

- Numeric claims can be unit tested independently of the model.
- Missing days are represented honestly.
- Prompt changes do not alter source calculations.
- Stored snapshots and responses support troubleshooting and audit review.
- Medication and binder safeguards are explicit.

### Negative

- Application code must evolve when new metrics are added.
- Persisting full responses increases sensitive-data retention.
- Strict schemas require coordinated model, parser, and UI changes.
- Source categories remain only as reliable as the recorded food names until a controlled classification model is added.

## Alternatives Considered

### Send raw food records and ask the model to calculate the report

Rejected because compliance counts would be less deterministic, harder to test, and more likely to vary between generations.

### Generate a fully deterministic text report without AI

Viable for baseline reporting, but less useful for concise synthesis and practical wording across several nutrient patterns. Deterministic report cards remain available alongside the coach.

### Allow the model to estimate binder-adjusted phosphorus

Rejected because binder effectiveness depends on medication, dose, timing, meal composition, and clinical context not safely available to the model.
