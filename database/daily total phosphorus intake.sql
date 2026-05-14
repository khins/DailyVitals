-- Using a 0.075 efficiency (60mg bound per 800mg pill)
WITH binder_settings AS (
    SELECT 
        (3 * 800 * 0.075) AS phos_bound_per_meal_mg
),
calculated_intake AS (
    SELECT
        food_phosphorus_intake_id,
        consumed_at::date AS intake_date,
        consumed_at,
        food_name,
        phosphorus_mg,
        -- Calculate net absorption per item, ensuring it doesn't drop below zero
        GREATEST(phosphorus_mg - (SELECT phos_bound_per_meal_mg FROM binder_settings), 0) AS net_item_phos_mg
    FROM food_phosphorus_intake
)
SELECT
    intake_date,
    consumed_at,
    food_name,
    phosphorus_mg AS raw_phos_mg,
    net_item_phos_mg,
    -- This provides your true running total of what your body actually absorbed
    SUM(net_item_phos_mg) OVER (
        PARTITION BY intake_date 
        ORDER BY consumed_at, food_phosphorus_intake_id
    ) AS running_net_daily_mg
FROM calculated_intake
ORDER BY intake_date DESC, consumed_at ASC;

-- Daily phosphorus totals.
WITH binder_dose AS (
    SELECT
        3 AS renvela_binder_count,
        800 AS renvela_binder_mg,
        -- Estimate that each 800 mg tablet binds about 60 mg of phosphorus.
        0.075 AS binding_efficiency
)
SELECT
    fpi.consumed_at::date AS intake_date,
    SUM(fpi.phosphorus_mg) AS total_phosphorus_mg,
    bd.renvela_binder_count,
    bd.renvela_binder_mg,
    (bd.renvela_binder_count * bd.renvela_binder_mg * bd.binding_efficiency) AS estimated_phos_bound_mg,
    GREATEST(SUM(fpi.phosphorus_mg) - (bd.renvela_binder_count * bd.renvela_binder_mg * bd.binding_efficiency), 0) AS net_absorbed_phos_mg
FROM food_phosphorus_intake fpi
CROSS JOIN binder_dose bd
GROUP BY
    fpi.consumed_at::date,
    bd.renvela_binder_count,
    bd.renvela_binder_mg,
    bd.binding_efficiency
ORDER BY intake_date DESC;
