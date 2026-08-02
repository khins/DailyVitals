ALTER TABLE public.kidney_lab_result
    DROP CONSTRAINT IF EXISTS kidney_lab_result_month_start_check;

COMMENT ON COLUMN public.kidney_lab_result.result_month IS
    'Lab collection date. The legacy column name is retained for compatibility.';

\i '../functions/sp_insert_kidney_lab_result_by_date.sql'
\i '../functions/sp_update_kidney_lab_result_by_date.sql'
