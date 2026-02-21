--drop function public.sp_get_exercise_history;

CREATE OR REPLACE FUNCTION public.sp_get_exercise_history(p_person_id bigint)
 RETURNS TABLE(exercise_session_id bigint, exercise_type_id bigint, exercise_name text, start_time timestamp without time zone, duration_minutes decimal, intensity text, notes text)
 LANGUAGE sql
AS $function$
    SELECT
        es.exercise_session_id,
        es.exercise_type_id,
        et.exercise_name,
        es.start_time,
        es.duration_minutes,
        es.intensity,
        es.notes
    FROM exercise_session es
    JOIN exercise_type et
      ON et.exercise_type_id = es.exercise_type_id
    WHERE es.person_id = p_person_id
    ORDER BY es.start_time DESC;
$function$
;
