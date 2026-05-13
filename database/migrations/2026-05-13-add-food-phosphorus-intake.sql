CREATE TABLE IF NOT EXISTS public.food_phosphorus_intake (
    food_phosphorus_intake_id bigserial NOT NULL,
    person_id int8 NOT NULL,
    food_name varchar(200) NOT NULL,
    phosphorus_mg integer NOT NULL,
    consumed_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    notes text NULL,
    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT food_phosphorus_intake_pkey PRIMARY KEY (food_phosphorus_intake_id),
    CONSTRAINT food_phosphorus_intake_food_name_check CHECK (length(trim(food_name)) > 0),
    CONSTRAINT food_phosphorus_intake_phosphorus_check CHECK (phosphorus_mg >= 0),
    CONSTRAINT fk_food_phosphorus_intake_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_food_phosphorus_intake_person_date
    ON public.food_phosphorus_intake USING btree (person_id, consumed_at DESC);

CREATE OR REPLACE FUNCTION public.sp_insert_food_phosphorus_intake(
    p_person_id bigint,
    p_food_name character varying,
    p_phosphorus_mg integer,
    p_consumed_at timestamp,
    p_notes text,
    p_entered_by character varying
)
RETURNS bigint
LANGUAGE plpgsql
AS $function$
DECLARE
    v_food_phosphorus_intake_id bigint;
BEGIN
    IF p_food_name IS NULL OR LENGTH(TRIM(p_food_name)) = 0 THEN
        RAISE EXCEPTION 'Food item is required';
    END IF;

    IF p_phosphorus_mg IS NULL OR p_phosphorus_mg < 0 THEN
        RAISE EXCEPTION 'Phosphorus must be a non-negative amount in mg';
    END IF;

    INSERT INTO food_phosphorus_intake (
        person_id,
        food_name,
        phosphorus_mg,
        consumed_at,
        notes
    )
    VALUES (
        p_person_id,
        TRIM(p_food_name),
        p_phosphorus_mg,
        COALESCE(p_consumed_at, CURRENT_TIMESTAMP),
        p_notes
    )
    RETURNING food_phosphorus_intake_id INTO v_food_phosphorus_intake_id;

    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'food_phosphorus_intake',
        v_food_phosphorus_intake_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'food_name', p_food_name,
            'phosphorus_mg', p_phosphorus_mg,
            'consumed_at', p_consumed_at,
            'notes', p_notes
        )
    );

    RETURN v_food_phosphorus_intake_id;
END;
$function$;

CREATE OR REPLACE PROCEDURE public.sp_delete_food_phosphorus_intake(
    p_food_phosphorus_intake_id bigint,
    p_entered_by character varying
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_deleted_record jsonb;
BEGIN
    SELECT to_jsonb(fpi)
    INTO v_deleted_record
    FROM food_phosphorus_intake fpi
    WHERE fpi.food_phosphorus_intake_id = p_food_phosphorus_intake_id;

    DELETE FROM food_phosphorus_intake
    WHERE food_phosphorus_intake_id = p_food_phosphorus_intake_id;

    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'food_phosphorus_intake',
        p_food_phosphorus_intake_id,
        'DELETE',
        p_entered_by,
        v_deleted_record
    );
END;
$procedure$;
