SELECT 
    result_month,
    phosphorus,
    -- Running total of Phosphorus across all recorded months
    SUM(phosphorus) OVER (
        PARTITION BY person_id 
        ORDER BY result_month 
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
    ) AS cumulative_phosphorus,    
    potassium,
    -- Running total of Potassium across all recorded months
    SUM(potassium) OVER (
        PARTITION BY person_id 
        ORDER BY result_month 
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
    ) AS cumulative_potassium,    
    albumin,
    creatinine,
    bun
FROM public.kidney_lab_result
WHERE person_id = 4 -- Replace with your actual person_id
ORDER BY result_month ASC;