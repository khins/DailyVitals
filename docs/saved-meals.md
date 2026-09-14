# Saved meals

The Nutrition page includes a Saved meals panel. Choose **Save as meal**, name the template, choose a meal type, and select logged foods by date. The dialog lists all selected foods before saving. Saved templates remain visible after reopening Nutrition; expand a template to see its foods and servings.

This first increment saves and displays templates. Logging an entire template, editing templates, and deleting templates are future increments. The existing individual-food Add to Today action is unchanged.

## Database setup

Apply `database/migrations/20260914_saved_meals.sql` to the application's PostgreSQL database before using this feature. The application does not apply this migration automatically.

Saved meals belong to one person. Saving snapshots the selected entries in a single SQL statement, checks that every selected entry belongs to that person and still exists, and rejects duplicate names (case-insensitive). The snapshot preserves nullable nutrients, serving descriptions, and AI provenance. It excludes binders, diary timestamps, diary IDs, and personal entry notes. Saving never inserts nutrition or fluid diary entries. Demo Mode cannot save templates.

## Verification

- Full solution build passes with zero warnings and errors.
- Database persistence and interactive browser checks require a migrated test database and have not been run.
- On a synthetic test account, save multiple foods as a breakfast, reload Nutrition, and expand the saved meal to verify servings.
- Verify blank names, duplicate names, and empty selections cannot save.
- Select foods across two dates and remove a selected food before saving.
- Confirm diary counts and totals are unchanged, and that another account cannot see the meal.
- Confirm a deleted or other-person entry ID cannot produce a partial template.
