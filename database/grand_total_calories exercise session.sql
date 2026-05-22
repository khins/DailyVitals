SELECT
    COALESCE(SUM(e.calories_expended), 0) AS grand_total_calories
FROM public.exercise_session e;
