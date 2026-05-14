-- Food items with a running phosphorus total for each day.
WITH binder_dose AS (
    SELECT
        3 AS renvela_binder_count,
        800 AS renvela_binder_mg,
        -- Estimate that each 800 mg tablet binds about 60 mg of phosphorus.
        0.075 AS binding_efficiency
),
running_food_phosphorus AS (
    SELECT
        food_phosphorus_intake_id,
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
)
SELECT
    rfp.intake_date,
    rfp.consumed_at,
    rfp.food_name,
    rfp.phosphorus_mg,
    rfp.running_daily_phosphorus_mg,
    (bd.renvela_binder_count * bd.renvela_binder_mg * bd.binding_efficiency) AS estimated_phos_bound_mg,
    GREATEST(rfp.running_daily_phosphorus_mg - (bd.renvela_binder_count * bd.renvela_binder_mg * bd.binding_efficiency), 0) AS net_absorbed_phos_mg
FROM running_food_phosphorus rfp
CROSS JOIN binder_dose bd
ORDER BY rfp.intake_date DESC, rfp.consumed_at, rfp.food_phosphorus_intake_id;

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
