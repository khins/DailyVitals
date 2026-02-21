CREATE OR REPLACE FUNCTION public.sp_insert_renal_diet_food(p_person_id bigint, p_food_name character varying, p_serving_size character varying, p_calories integer, p_sodium_mg integer, p_potassium_mg integer, p_phosphorus_mg integer, p_protein_g numeric, p_allowed boolean, p_restriction_notes text, p_entered_by character varying)
 RETURNS bigint
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_renal_food_id BIGINT;
BEGIN
    ------------------------------------------------------------------
    -- Validation
    ------------------------------------------------------------------
    IF p_food_name IS NULL OR LENGTH(TRIM(p_food_name)) = 0 THEN
        RAISE EXCEPTION 'Food name is required';
    END IF;

    IF p_calories < 0
       OR p_sodium_mg < 0
       OR p_potassium_mg < 0
       OR p_phosphorus_mg < 0
       OR p_protein_g < 0
    THEN
        RAISE EXCEPTION 'Nutrient values cannot be negative';
    END IF;

    ------------------------------------------------------------------
    -- Insert renal diet food
    ------------------------------------------------------------------
    INSERT INTO renal_diet_food (
        person_id,
        food_name,
        serving_size,
        calories,
        sodium_mg,
        potassium_mg,
        phosphorus_mg,
        protein_g,
        allowed,
        restriction_notes
    )
    VALUES (
        p_person_id,
        p_food_name,
        p_serving_size,
        p_calories,
        p_sodium_mg,
        p_potassium_mg,
        p_phosphorus_mg,
        p_protein_g,
        COALESCE(p_allowed, TRUE),
        p_restriction_notes
    )
    RETURNING renal_food_id INTO v_renal_food_id;

    ------------------------------------------------------------------
    -- Audit log
    ------------------------------------------------------------------
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'renal_diet_food',
        v_renal_food_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'food_name', p_food_name,
            'serving_size', p_serving_size,
            'calories', p_calories,
            'sodium_mg', p_sodium_mg,
            'potassium_mg', p_potassium_mg,
            'phosphorus_mg', p_phosphorus_mg,
            'protein_g', p_protein_g,
            'allowed', p_allowed
        )
    );

    ------------------------------------------------------------------
    -- Return ID
    ------------------------------------------------------------------
    RETURN v_renal_food_id;
END;
$function$
;
