CREATE OR REPLACE FUNCTION sp_update_weight (
    p_weight_id     bigint,
    p_weight_value  numeric,
    p_weight_unit   text,
    p_reading_time  timestamp,
    p_notes         text,
    p_entered_by    text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    -- Log BEFORE update
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
        'UPDATE',
        p_entered_by,
        jsonb_build_object(
            'before', jsonb_build_object(
                'weight_value', weight_value,
                'weight_unit', weight_unit,
                'reading_time', reading_time,
                'notes', notes
            ),
            'after', jsonb_build_object(
                'weight_value', p_weight_value,
                'weight_unit', p_weight_unit,
                'reading_time', p_reading_time,
                'notes', p_notes
            )
        )
    FROM weight
    WHERE weight_id = p_weight_id;

    -- Update record
    UPDATE weight
    SET
        weight_value = p_weight_value,
        weight_unit  = p_weight_unit,
        reading_time = p_reading_time,
        notes        = p_notes
    WHERE weight_id = p_weight_id;
END;
$$;
