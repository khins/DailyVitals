ALTER TABLE public.exercise_session
ADD COLUMN IF NOT EXISTS calories_expended numeric(8, 2) NULL;

ALTER TABLE public.exercise_session
DROP CONSTRAINT IF EXISTS exercise_session_calories_expended_check;

ALTER TABLE public.exercise_session
ADD CONSTRAINT exercise_session_calories_expended_check
CHECK (calories_expended IS NULL OR calories_expended >= 0);

DROP PROCEDURE IF EXISTS public.sp_insert_exercise_session(
    bigint,
    bigint,
    timestamp without time zone,
    decimal,
    character varying,
    text,
    text);

CREATE OR REPLACE PROCEDURE public.sp_insert_exercise_session(
    IN p_person_id bigint,
    IN p_exercise_type_id bigint,
    IN p_start_time timestamp without time zone,
    IN p_duration_minutes decimal,
    IN p_calories_expended decimal,
    IN p_intensity character varying,
    IN p_notes text,
    IN p_entered_by text)
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
        calories_expended,
        intensity,
        notes
    )
    VALUES (
        p_person_id,
        p_exercise_type_id,
        p_start_time,
        p_duration_minutes,
        p_calories_expended,
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
            'calories_expended', p_calories_expended,
            'intensity', p_intensity,
            'notes', p_notes
        )
    );
END;
$procedure$;

DROP FUNCTION IF EXISTS public.sp_get_exercise_history(bigint);

CREATE OR REPLACE FUNCTION public.sp_get_exercise_history(p_person_id bigint)
RETURNS TABLE(
    exercise_session_id bigint,
    exercise_type_id bigint,
    exercise_name text,
    start_time timestamp without time zone,
    duration_minutes decimal,
    calories_expended decimal,
    intensity text,
    notes text)
LANGUAGE sql
AS $function$
    SELECT
        es.exercise_session_id,
        es.exercise_type_id,
        et.exercise_name,
        es.start_time,
        es.duration_minutes,
        es.calories_expended,
        es.intensity,
        es.notes
    FROM exercise_session es
    JOIN exercise_type et
      ON et.exercise_type_id = es.exercise_type_id
    WHERE es.person_id = p_person_id
    ORDER BY es.start_time DESC;
$function$;
