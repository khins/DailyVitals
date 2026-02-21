CREATE OR REPLACE FUNCTION sp_get_bp_trend(
    p_person_id BIGINT,
    p_start_date TIMESTAMP,
    p_end_date TIMESTAMP
)
RETURNS TABLE (
    reading_time TIMESTAMP,
    systolic INT,
    diastolic INT
)
LANGUAGE sql
AS $$
    SELECT reading_time,
           systolic,
           diastolic
    FROM blood_pressure
    WHERE person_id = p_person_id
      AND reading_time >= p_start_date
      AND reading_time <  p_end_date
    ORDER BY reading_time;
$$;
