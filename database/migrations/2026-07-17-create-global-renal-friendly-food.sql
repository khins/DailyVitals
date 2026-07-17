CREATE TABLE IF NOT EXISTS public.renal_friendly_food
(
    renal_friendly_food_id bigserial PRIMARY KEY,
    food_name varchar(200) NOT NULL,
    category varchar(100),
    serving_description varchar(150),
    phosphorus_mg numeric(10,2),
    sodium_mg numeric(10,2),
    potassium_mg numeric(10,2),
    protein_g numeric(10,2),
    calories numeric(10,2),
    renal_rating varchar(30),
    guidance_notes text,
    source_notes text,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    CONSTRAINT renal_friendly_food_food_name_not_blank
        CHECK (length(btrim(food_name)) > 0),
    CONSTRAINT renal_friendly_food_phosphorus_nonnegative
        CHECK (phosphorus_mg IS NULL OR phosphorus_mg >= 0),
    CONSTRAINT renal_friendly_food_sodium_nonnegative
        CHECK (sodium_mg IS NULL OR sodium_mg >= 0),
    CONSTRAINT renal_friendly_food_potassium_nonnegative
        CHECK (potassium_mg IS NULL OR potassium_mg >= 0),
    CONSTRAINT renal_friendly_food_protein_nonnegative
        CHECK (protein_g IS NULL OR protein_g >= 0),
    CONSTRAINT renal_friendly_food_calories_nonnegative
        CHECK (calories IS NULL OR calories >= 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_renal_friendly_food_name_serving
    ON public.renal_friendly_food
    (lower(btrim(food_name)), lower(btrim(COALESCE(serving_description, ''))));

CREATE INDEX IF NOT EXISTS ix_renal_friendly_food_active_category_name
    ON public.renal_friendly_food (is_active, category, food_name);

CREATE INDEX IF NOT EXISTS ix_renal_friendly_food_rating
    ON public.renal_friendly_food (renal_rating)
    WHERE is_active = true;

-- Preserve the existing catalog while removing person-specific duplication.
-- The legacy table remains intact during this first migration for rollback and
-- for any legacy foreign keys that still reference it.
DO $migration$
BEGIN
    IF to_regclass('public.renal_diet_food') IS NOT NULL THEN
        INSERT INTO public.renal_friendly_food
        (
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
            updated_at
        )
        SELECT DISTINCT ON
        (
            lower(btrim(rdf.food_name)),
            lower(btrim(COALESCE(rdf.serving_size, '')))
        )
            btrim(rdf.food_name),
            rfc.category_name,
            NULLIF(btrim(rdf.serving_size), ''),
            rdf.phosphorus_mg,
            rdf.sodium_mg,
            rdf.potassium_mg,
            rdf.protein_g,
            rdf.calories,
            CASE WHEN rdf.allowed THEN 'Preferred' ELSE 'Limit' END,
            rdf.restriction_notes,
            'Migrated from the legacy renal_diet_food catalog.',
            true,
            rdf.created_at,
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
        ON CONFLICT DO NOTHING;
    END IF;
END
$migration$;

COMMENT ON TABLE public.renal_friendly_food IS
    'Shared renal-friendly food reference catalog available to every My Active Vitals user.';

COMMENT ON COLUMN public.renal_friendly_food.source_notes IS
    'Reference provenance or source context; not user-specific entry notes.';
