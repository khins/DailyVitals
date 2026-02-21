CREATE OR REPLACE PROCEDURE public.sp_insert_exercise_session(IN p_person_id bigint, IN p_exercise_type_id bigint, IN p_start_time timestamp without time zone, IN p_duration_minutes decimal, IN p_intensity character varying, IN p_notes text, IN p_entered_by text)
 LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_exercise_session_id bigint;
BEGIN
    INSERT INTO exercise_session (
        person_id,
        exercise_type_id,
        start_time,
        duration_minutes,
        intensity,
        notes
    )
    VALUES (
        p_person_id,
        p_exercise_type_id,
        p_start_time,
        p_duration_minutes,
        p_intensity,
        p_notes
    )
    RETURNING exercise_session_id
    INTO v_exercise_session_id;

    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'exercise_session',
        v_exercise_session_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'person_id', p_person_id,
            'exercise_type_id', p_exercise_type_id,
            'start_time', p_start_time,
            'duration_minutes', p_duration_minutes,
            'intensity', p_intensity,
            'notes', p_notes
        )
    );
END;
$procedure$
;
