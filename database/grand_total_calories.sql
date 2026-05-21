WITH exercise_data AS (
    SELECT 
        e.exercise_session_id,
        p.person_id,
        p.first_name,
        e.exercise_type_id,
        e.start_time,
        e.duration_minutes,
        e.intensity,
        e.notes,
        e.created_at,
        e.calories_expended,
        et.exercise_name
    FROM public.exercise_session e
    INNER JOIN person p
        ON e.person_id = p.person_id
    INNER JOIN exercise_type et
        ON et.exercise_type_id = e.exercise_type_id
)
SELECT
    exercise_session_id,
    person_id,
    first_name,
    exercise_type_id,
    exercise_name,
    start_time,
    duration_minutes,
    intensity,
    notes,
    created_at,
    calories_expended,
    SUM(calories_expended) OVER (
        PARTITION BY person_id
        ORDER BY start_time
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
    ) AS running_calories_total,
    SUM(calories_expended) OVER (
        PARTITION BY person_id
    ) AS grand_total_calories
FROM exercise_data
ORDER BY exercise_session_id desc;