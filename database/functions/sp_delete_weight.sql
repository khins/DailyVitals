CREATE OR REPLACE FUNCTION sp_delete_weight (
    p_weight_id  bigint,
    p_entered_by text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    -- Log the record BEFORE deletion
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    SELECT
        'weight',
        weight_id,
        'DELETE',
        p_entered_by,
        jsonb_build_object(
            'person_id', person_id,
            'weight_value', weight_value,
            'weight_unit', weight_unit,
            'reading_time', reading_time,
            'notes', notes
        )
    FROM weight
    WHERE weight_id = p_weight_id;

    -- Delete the record
    DELETE FROM weight
    WHERE weight_id = p_weight_id;
END;
$$;
