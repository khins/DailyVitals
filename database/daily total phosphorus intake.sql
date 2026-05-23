-- Using a 0.075 efficiency (60mg bound per 800mg pill)
WITH binder_settings AS (
    SELECT 
        800 AS binder_mg,
        0.075 AS binding_efficiency
),
calculated_intake AS (
    SELECT
        food_phosphorus_intake_id,
        consumed_at::date AS intake_date,
        consumed_at,
        food_name,
        phosphorus_mg,
        binders,
        -- Calculate net absorption per item, ensuring it doesn't drop below zero
        GREATEST(phosphorus_mg - (COALESCE(binders, 0) * bs.binder_mg * bs.binding_efficiency), 0) AS net_item_phos_mg
    FROM food_phosphorus_intake
    CROSS JOIN binder_settings bs
)
SELECT
    intake_date,
    consumed_at,
    food_name,
    phosphorus_mg AS raw_phos_mg,
    binders,
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
        800 AS renvela_binder_mg,
        -- Estimate that each 800 mg tablet binds about 60 mg of phosphorus.
        0.075 AS binding_efficiency
)
SELECT
    fpi.consumed_at::date AS intake_date,
    SUM(fpi.phosphorus_mg) AS total_phosphorus_mg,
    SUM(COALESCE(fpi.binders, 0)) AS renvela_binder_count,
    bd.renvela_binder_mg,
    (SUM(COALESCE(fpi.binders, 0)) * bd.renvela_binder_mg * bd.binding_efficiency) AS estimated_phos_bound_mg,
    GREATEST(SUM(fpi.phosphorus_mg) - (SUM(COALESCE(fpi.binders, 0)) * bd.renvela_binder_mg * bd.binding_efficiency), 0) AS net_absorbed_phos_mg
FROM food_phosphorus_intake fpi
CROSS JOIN binder_dose bd
GROUP BY
    fpi.consumed_at::date,
    bd.renvela_binder_mg,
    bd.binding_efficiency
ORDER BY intake_date DESC;
