-- Run this query against the LOCAL DailyVitals database in pgAdmin.
-- It returns one cell named production_import_sql. Copy that cell into a new
-- production query window, review it, and execute it against production.
--
-- The generated import is:
--   * wrapped in a transaction;
--   * deduplicated by normalized food name and serving;
--   * safe for quotes and line breaks in text values; and
--   * repeatable because matching rows are updated instead of duplicated.

WITH source_foods AS
(
    SELECT DISTINCT ON
    (
        lower(btrim(rdf.food_name)),
        lower(btrim(COALESCE(rdf.serving_size, '')))
    )
        btrim(rdf.food_name) AS food_name,
        rfc.category_name AS category,
        NULLIF(btrim(rdf.serving_size), '') AS serving_description,
        rdf.phosphorus_mg,
        rdf.sodium_mg,
        rdf.potassium_mg,
        rdf.protein_g,
        rdf.calories,
        CASE WHEN rdf.allowed THEN 'Preferred' ELSE 'Limit' END AS renal_rating,
        rdf.restriction_notes AS guidance_notes,
        'Imported from the local renal_diet_food catalog.'::text AS source_notes,
        true AS is_active,
        COALESCE(rdf.created_at, CURRENT_TIMESTAMP) AS created_at,
        rdf.updated_at
    FROM public.renal_diet_food rdf
    LEFT JOIN public.renal_food_category rfc
        ON rfc.category_id = rdf.category_id
    WHERE length(btrim(rdf.food_name)) > 0
    ORDER BY
        lower(btrim(rdf.food_name)),
        lower(btrim(COALESCE(rdf.serving_size, ''))),
        rdf.allowed DESC,
        rdf.updated_at DESC NULLS LAST,
        rdf.renal_food_id DESC
),
insert_statements AS
(
    SELECT
        food_name,
        serving_description,
        format(
            'INSERT INTO public.renal_friendly_food (food_name, category, serving_description, phosphorus_mg, sodium_mg, potassium_mg, protein_g, calories, renal_rating, guidance_notes, source_notes, is_active, created_at, updated_at) VALUES (%L, %L, %L, %L, %L, %L, %L, %L, %L, %L, %L, %L, %L, %L) ON CONFLICT (lower(btrim(food_name)), lower(btrim(COALESCE(serving_description, %L)))) DO UPDATE SET category = EXCLUDED.category, phosphorus_mg = EXCLUDED.phosphorus_mg, sodium_mg = EXCLUDED.sodium_mg, potassium_mg = EXCLUDED.potassium_mg, protein_g = EXCLUDED.protein_g, calories = EXCLUDED.calories, renal_rating = EXCLUDED.renal_rating, guidance_notes = EXCLUDED.guidance_notes, source_notes = EXCLUDED.source_notes, is_active = EXCLUDED.is_active, updated_at = CURRENT_TIMESTAMP;',
            food_name,
            category,
            serving_description,
            phosphorus_mg,
            sodium_mg,
            potassium_mg,
            protein_g,
            calories,
            renal_rating,
            guidance_notes,
            source_notes,
            is_active,
            created_at,
            updated_at,
            ''
        ) AS statement
    FROM source_foods
)
SELECT
    'BEGIN;' || E'\n\n' ||
    COALESCE(
        string_agg(statement, E'\n' ORDER BY lower(food_name), lower(COALESCE(serving_description, ''))),
        '-- No source foods were found.'
    ) ||
    E'\n\nCOMMIT;' AS production_import_sql
FROM insert_statements;
