CREATE OR REPLACE PROCEDURE sp_update_medication (
    p_medication_id bigint,
    p_dosage_mg numeric,
    p_dosage_form text,
    p_take_morning boolean,
    p_take_noon boolean,
    p_take_evening boolean,
    p_take_bedtime boolean,
    p_instructions text,
    p_prescribed_by text
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE medication
    SET dosage_mg = p_dosage_mg,
        dosage_form = p_dosage_form,
        take_morning = p_take_morning,
        take_noon = p_take_noon,
        take_evening = p_take_evening,
        take_bedtime = p_take_bedtime,
        instructions = p_instructions,
        prescribed_by = p_prescribed_by,
        updated_at = CURRENT_TIMESTAMP
    WHERE medication_id = p_medication_id;
END;
$$;
