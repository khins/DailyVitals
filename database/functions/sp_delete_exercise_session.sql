CREATE OR REPLACE PROCEDURE sp_delete_exercise_session (
    p_exercise_session_id bigint,
    p_entered_by text
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_deleted_record jsonb;
BEGIN
    -- Capture record before delete
    SELECT to_jsonb(es)
    INTO v_deleted_record
    FROM exercise_session es
    WHERE es.exercise_session_id = p_exercise_session_id;

    -- Delete the record
    DELETE FROM exercise_session
    WHERE exercise_session_id = p_exercise_session_id;

    -- Log deletion
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'exercise_session',
        p_exercise_session_id,
        'DELETE',
        p_entered_by,
        v_deleted_record
    );
END;
$$;
