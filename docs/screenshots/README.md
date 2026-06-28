# Screenshot Guide

Screenshots make the portfolio easier to evaluate, but DailyVitals screens can contain sensitive health data. Only capture synthetic demonstration records.

## Portfolio Capture Set

Capture the following views from the read-only demo account. Together they show the
application's core tracking workflows, renal-care focus, responsive design, and AI
features without exposing personal data.

| File | View and state | Portfolio purpose |
| --- | --- | --- |
| `dashboard-overview-desktop.png` | Dashboard at desktop width with seeded summary data visible | Establishes the overall product and navigation |
| `blood-pressure-entry-desktop.png` | Blood pressure entry form and recent readings | Shows a complete health-data entry workflow |
| `blood-pressure-trend-desktop.png` | Blood pressure trend chart and readings-in-range table | Demonstrates longitudinal reporting |
| `nutrition-ai-estimate.png` | Nutrition entry populated with AI-estimated nutrients, serving details, and renal rating | Highlights structured AI output and kidney-care guidance |
| `ai-coach-before-generation.png` | Weekly AI Nutrition Coach ready state before generation | Shows the grounded review workflow and user control |
| `ai-coach-after-generation.png` | Generated or demo-seeded weekly review with nutrient observations | Demonstrates the actionable AI result |
| `nutrient-report-modal.png` | Sodium, phosphorus, protein, or potassium actionable report modal | Shows focused drill-down reporting without crowding the page |
| `fluid-intake-tracking.png` | Fluid entry and recent intake history | Demonstrates dialysis-relevant fluid tracking |
| `kidney-labs-synthetic.png` | Kidney lab entry and history using synthetic values | Shows renal lab tracking and data breadth |
| `mobile-responsive-layout.png` | One representative screen at mobile width | Demonstrates responsive navigation and form layout |

The blood pressure and AI Coach recommendations intentionally use two images each.
Those paired states communicate the workflow more clearly than a single crowded
capture.

## Suggested Captions

- **Dashboard:** A consolidated view of recent vitals, nutrition targets, activity, and fluid intake.
- **Blood pressure:** Record a reading and review changes over time without leaving the feature area.
- **AI nutrition estimate:** Structured nutrient estimates include kidney-relevant nutrients and a renal-friendly rating.
- **AI Nutrition Coach:** Application-calculated weekly facts ground a concise, saved nutrition review.
- **Actionable nutrient report:** Focused reports explain goal progress and identify the foods contributing most to an excess.
- **Fluid intake:** Daily fluid records help place short-term weight changes in context.
- **Kidney labs:** Synthetic lab panels demonstrate longitudinal renal-health tracking.
- **Responsive layout:** Core DailyVitals workflows remain usable on a narrow screen.

## Capture Standards

- Use `1440 x 900` for desktop captures and `390 x 844` for the mobile capture.
- Keep browser zoom at 100% and use consistent operating-system scaling.
- Show the actual working interface rather than mockups.
- Keep text readable and avoid excessive empty space.
- Capture meaningful success, empty, and confirmation states.
- Use PNG for interface screenshots.
- Use the exact filenames in the capture table so documentation links remain stable.
- Hide browser chrome where practical and do not include developer tools.

## Capture Workflow

1. Start DailyVitals locally and sign in with the public Demo Mode credentials documented in [Demo Mode](../demo-mode.md).
2. Confirm the Demo Mode banner is visible and the records are synthetic.
3. Navigate to the view and state named in the capture table.
4. Frame the primary workflow tightly while retaining enough navigation to identify the feature.
5. Review the image against the redaction rules before adding it to this folder.
6. Add approved images to a `Screenshots` section in the root `README.md` using the suggested captions.

## Redaction Rules

Never include:

- Real names or usernames
- Actual medical readings or lab results
- Real food history tied to a person
- Passwords, API keys, connection strings, or browser developer tools showing secrets
- Database server details

Only approved images should be referenced from the root `README.md` or a feature document. Do not publish placeholder or broken image links.
