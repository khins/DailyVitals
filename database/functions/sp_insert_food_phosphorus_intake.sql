CREATE OR REPLACE FUNCTION public.sp_insert_food_phosphorus_intake(
    p_person_id bigint,
    p_food_name character varying,
    p_phosphorus_mg integer,
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

    INSERT INTO food_phosphorus_intake (
        person_id,
        food_name,
        phosphorus_mg,
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
