CREATE OR REPLACE FUNCTION public.sp_update_kidney_lab_result(
    p_kidney_lab_result_id bigint,
    p_result_month date,
    p_albumin numeric,
    p_npcr numeric,
    p_potassium numeric,
    p_wktv numeric,
    p_calcium numeric,
    p_phosphorus numeric,
    p_ipth numeric,
    p_hemoglobin numeric,
    p_glucose numeric,
    p_cholesterol numeric,
    p_triglycerides numeric,
    p_bun numeric,
    p_creatinine numeric,
    p_notes text,
    p_entered_by text
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    SELECT
        'kidney_lab_result',
        kidney_lab_result_id,
        'UPDATE',
        p_entered_by,
        jsonb_build_object(
            'before', jsonb_build_object(
                'result_month', result_month,
                'albumin', albumin,
                'npcr', npcr,
                'potassium', potassium,
                'wktv', wktv,
                'calcium', calcium,
                'phosphorus', phosphorus,
                'ipth', ipth,
                'hemoglobin', hemoglobin,
                'glucose', glucose,
                'cholesterol', cholesterol,
                'triglycerides', triglycerides,
                'bun', bun,
                'creatinine', creatinine,
                'notes', notes
            ),
            'after', jsonb_build_object(
                'result_month', date_trunc('month', p_result_month)::date,
                'albumin', p_albumin,
                'npcr', p_npcr,
                'potassium', p_potassium,
                'wktv', p_wktv,
                'calcium', p_calcium,
                'phosphorus', p_phosphorus,
                'ipth', p_ipth,
                'hemoglobin', p_hemoglobin,
                'glucose', p_glucose,
                'cholesterol', p_cholesterol,
                'triglycerides', p_triglycerides,
                'bun', p_bun,
                'creatinine', p_creatinine,
                'notes', p_notes
            )
        )
    FROM kidney_lab_result
    WHERE kidney_lab_result_id = p_kidney_lab_result_id;

    UPDATE kidney_lab_result
    SET result_month = date_trunc('month', p_result_month)::date,
        albumin = p_albumin,
        npcr = p_npcr,
        potassium = p_potassium,
        wktv = p_wktv,
        calcium = p_calcium,
        phosphorus = p_phosphorus,
        ipth = p_ipth,
        hemoglobin = p_hemoglobin,
        glucose = p_glucose,
        cholesterol = p_cholesterol,
        triglycerides = p_triglycerides,
        bun = p_bun,
        creatinine = p_creatinine,
        notes = p_notes
    WHERE kidney_lab_result_id = p_kidney_lab_result_id;
END;
$$;
