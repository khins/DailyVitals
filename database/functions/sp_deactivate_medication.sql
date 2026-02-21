CREATE OR REPLACE PROCEDURE sp_deactivate_medication (
    p_medication_id bigint,
    p_entered_by text
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE medication
    SET is_active = false,
        updated_at = CURRENT_TIMESTAMP
    WHERE medication_id = p_medication_id;

    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'medication',
        p_medication_id,
        'DEACTIVATE',
        p_entered_by,
        jsonb_build_object('is_active', false)
    );
END;
$$;
