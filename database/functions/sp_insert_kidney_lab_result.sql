CREATE OR REPLACE FUNCTION public.sp_insert_kidney_lab_result(
    p_person_id bigint,
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
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    v_id bigint;
BEGIN
    INSERT INTO kidney_lab_result (
        person_id,
        result_month,
        albumin,
        npcr,
        potassium,
        wktv,
        calcium,
        phosphorus,
        ipth,
        hemoglobin,
        glucose,
        cholesterol,
        triglycerides,
        bun,
        creatinine,
        notes
    )
    VALUES (
        p_person_id,
        date_trunc('month', p_result_month)::date,
        p_albumin,
        p_npcr,
        p_potassium,
        p_wktv,
        p_calcium,
        p_phosphorus,
        p_ipth,
        p_hemoglobin,
        p_glucose,
        p_cholesterol,
        p_triglycerides,
        p_bun,
        p_creatinine,
        p_notes
    )
    RETURNING kidney_lab_result_id INTO v_id;

    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'kidney_lab_result',
        v_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'person_id', p_person_id,
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
    );

    RETURN v_id;
END;
$$;
