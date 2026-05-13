-- Food items with a running phosphorus total for each day.
SELECT
    consumed_at::date AS intake_date,
    consumed_at,
    food_name,
    phosphorus_mg,
    SUM(phosphorus_mg) OVER (
        PARTITION BY person_id, consumed_at::date
        ORDER BY consumed_at, food_phosphorus_intake_id
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
    ) AS running_daily_phosphorus_mg
FROM food_phosphorus_intake
ORDER BY consumed_at::date DESC, consumed_at, food_phosphorus_intake_id;

-- Daily phosphorus totals.
SELECT
    consumed_at::date AS intake_date,
    SUM(phosphorus_mg) AS total_phosphorus_mg
FROM food_phosphorus_intake
GROUP BY consumed_at::date
ORDER BY intake_date DESC;
