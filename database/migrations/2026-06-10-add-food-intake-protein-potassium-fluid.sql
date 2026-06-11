ALTER TABLE public.food_phosphorus_intake
    ADD COLUMN IF NOT EXISTS protein_g numeric(8, 2) NULL,
    ADD COLUMN IF NOT EXISTS potassium_mg integer NULL,
    ADD COLUMN IF NOT EXISTS fluid_ml integer NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'food_phosphorus_intake_protein_check'
    ) THEN
        ALTER TABLE public.food_phosphorus_intake
            ADD CONSTRAINT food_phosphorus_intake_protein_check CHECK (protein_g IS NULL OR protein_g >= 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'food_phosphorus_intake_potassium_check'
    ) THEN
        ALTER TABLE public.food_phosphorus_intake
            ADD CONSTRAINT food_phosphorus_intake_potassium_check CHECK (potassium_mg IS NULL OR potassium_mg >= 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'food_phosphorus_intake_fluid_check'
    ) THEN
        ALTER TABLE public.food_phosphorus_intake
            ADD CONSTRAINT food_phosphorus_intake_fluid_check CHECK (fluid_ml IS NULL OR fluid_ml >= 0);
    END IF;
END $$;

CREATE OR REPLACE FUNCTION public.sp_insert_food_phosphorus_intake(
    p_person_id bigint,
    p_food_name character varying,
    p_phosphorus_mg integer,
    p_calories integer,
    p_sodium_mg integer,
    p_protein_g numeric,
    p_potassium_mg integer,
    p_fluid_ml integer,
    p_binders integer,
    p_consumed_at timestamp,
    p_notes text,
    p_serving_description character varying,
    p_estimated_by_ai boolean,
    p_ai_provider character varying,
    p_ai_confidence character varying,
    p_source_notes text,
    p_entered_by character varying
)
RETURNS bigint
LANGUAGE plpgsql
AS $function$
DECLARE
    v_food_phosphorus_intake_id bigint;
BEGIN
    IF p_food_name IS NULL OR LENGTH(TRIM(p_food_name)) = 0 THEN
        RAISE EXCEPTION 'Food item is required';
    END IF;

    IF p_phosphorus_mg IS NULL OR p_phosphorus_mg < 0 THEN
        RAISE EXCEPTION 'Phosphorus must be a non-negative amount in mg';
    END IF;

    IF p_calories IS NOT NULL AND p_calories < 0 THEN
        RAISE EXCEPTION 'Calories must be a non-negative whole number';
    END IF;

    IF p_sodium_mg IS NOT NULL AND p_sodium_mg < 0 THEN
        RAISE EXCEPTION 'Sodium must be a non-negative amount in mg';
    END IF;

    IF p_protein_g IS NOT NULL AND p_protein_g < 0 THEN
        RAISE EXCEPTION 'Protein must be a non-negative amount in grams';
    END IF;

    IF p_potassium_mg IS NOT NULL AND p_potassium_mg < 0 THEN
        RAISE EXCEPTION 'Potassium must be a non-negative amount in mg';
    END IF;

    IF p_fluid_ml IS NOT NULL AND p_fluid_ml < 0 THEN
        RAISE EXCEPTION 'Fluid must be a non-negative amount in ml';
    END IF;

    IF p_binders IS NULL OR p_binders < 0 THEN
        RAISE EXCEPTION 'Binders must be a non-negative whole number';
    END IF;

    INSERT INTO food_phosphorus_intake (
        person_id,
        food_name,
        phosphorus_mg,
        calories,
        sodium_mg,
        protein_g,
        potassium_mg,
        fluid_ml,
        binders,
        consumed_at,
        notes,
        serving_description,
        estimated_by_ai,
        ai_provider,
        ai_confidence,
        source_notes
    )
    VALUES (
        p_person_id,
        TRIM(p_food_name),
        p_phosphorus_mg,
        p_calories,
        p_sodium_mg,
        p_protein_g,
        p_potassium_mg,
        p_fluid_ml,
        p_binders,
        COALESCE(p_consumed_at, CURRENT_TIMESTAMP),
        p_notes,
        p_serving_description,
        COALESCE(p_estimated_by_ai, false),
        p_ai_provider,
        p_ai_confidence,
        p_source_notes
    )
    RETURNING food_phosphorus_intake_id INTO v_food_phosphorus_intake_id;

    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'food_phosphorus_intake',
        v_food_phosphorus_intake_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'food_name', p_food_name,
            'phosphorus_mg', p_phosphorus_mg,
            'calories', p_calories,
            'sodium_mg', p_sodium_mg,
            'protein_g', p_protein_g,
            'potassium_mg', p_potassium_mg,
            'fluid_ml', p_fluid_ml,
            'binders', p_binders,
            'consumed_at', p_consumed_at,
            'notes', p_notes,
            'serving_description', p_serving_description,
            'estimated_by_ai', p_estimated_by_ai,
            'ai_provider', p_ai_provider,
            'ai_confidence', p_ai_confidence,
            'source_notes', p_source_notes
        )
    );

    RETURN v_food_phosphorus_intake_id;
END;
$function$;
