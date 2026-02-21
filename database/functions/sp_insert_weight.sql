CREATE OR REPLACE FUNCTION sp_insert_weight (
    p_person_id     bigint,
    p_weight_value  numeric(6,2),
    p_weight_unit   varchar,
    p_reading_time  timestamp,
    p_notes         text,
    p_entered_by    text
)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    v_weight_id bigint;
BEGIN
    -- Insert weight record
    INSERT INTO public.weight
        (person_id, weight_value, weight_unit, reading_time, notes)
    VALUES
        (p_person_id, p_weight_value, p_weight_unit, p_reading_time, p_notes)
    RETURNING weight_id
    INTO v_weight_id;

    -- Log the data entry
    INSERT INTO public.data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'weight',
        v_weight_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'person_id', p_person_id,
            'weight_value', p_weight_value,
            'weight_unit', p_weight_unit,
            'reading_time', p_reading_time,
            'notes', p_notes
        )
    );

    RETURN v_weight_id;
END;
$$;
