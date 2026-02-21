CREATE OR REPLACE FUNCTION sp_delete_blood_glucose (
    p_glucose_id bigint,
    p_entered_by text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    -- Log BEFORE delete (so data is preserved)
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    SELECT
        'blood_glucose',
        glucose_id,
        'DELETE',
        p_entered_by,
        jsonb_build_object(
            'person_id', person_id,
            'glucose_value', glucose_value,
            'reading_time', reading_time,
            'notes', notes
        )
    FROM blood_glucose
    WHERE glucose_id = p_glucose_id;

    -- Delete record
    DELETE FROM blood_glucose
    WHERE glucose_id = p_glucose_id;
END;
$$;
