CREATE OR REPLACE PROCEDURE public.sp_delete_food_phosphorus_intake(
    p_food_phosphorus_intake_id bigint,
    p_entered_by character varying
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_deleted_record jsonb;
BEGIN
    SELECT to_jsonb(fpi)
    INTO v_deleted_record
    FROM food_phosphorus_intake fpi
    WHERE fpi.food_phosphorus_intake_id = p_food_phosphorus_intake_id;

    DELETE FROM food_phosphorus_intake
    WHERE food_phosphorus_intake_id = p_food_phosphorus_intake_id;

    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'food_phosphorus_intake',
        p_food_phosphorus_intake_id,
        'DELETE',
        p_entered_by,
        v_deleted_record
    );
END;
$procedure$;
