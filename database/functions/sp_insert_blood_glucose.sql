CREATE OR REPLACE FUNCTION sp_insert_blood_glucose (
    p_person_id     bigint,
    p_glucose_value int,
    p_reading_time  timestamp,
    p_notes         text
)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    v_glucose_id bigint;
BEGIN
    INSERT INTO blood_glucose
        (person_id, glucose_value, reading_time, notes)
    VALUES
        (p_person_id, p_glucose_value, p_reading_time, p_notes)
    RETURNING glucose_id
    INTO v_glucose_id;

    RETURN v_glucose_id;
END;
$$;
