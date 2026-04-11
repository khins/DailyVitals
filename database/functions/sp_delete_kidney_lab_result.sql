CREATE OR REPLACE FUNCTION public.sp_delete_kidney_lab_result(
    p_kidney_lab_result_id bigint,
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
        'DELETE',
        p_entered_by,
        jsonb_build_object(
            'person_id', person_id,
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
        )
    FROM kidney_lab_result
    WHERE kidney_lab_result_id = p_kidney_lab_result_id;

    DELETE FROM kidney_lab_result
    WHERE kidney_lab_result_id = p_kidney_lab_result_id;
END;
$$;
