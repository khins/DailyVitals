-- DROP SCHEMA public;

CREATE SCHEMA public AUTHORIZATION pg_database_owner;

-- DROP SEQUENCE public.blood_glucose_glucose_id_seq;

CREATE SEQUENCE public.blood_glucose_glucose_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.blood_pressure_bp_id_seq;

CREATE SEQUENCE public.blood_pressure_bp_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.data_entry_log_log_id_seq;

CREATE SEQUENCE public.data_entry_log_log_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.exercise_goal_exercise_goal_id_seq;

CREATE SEQUENCE public.exercise_goal_exercise_goal_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.exercise_session_exercise_session_id_seq;

CREATE SEQUENCE public.exercise_session_exercise_session_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.exercise_type_exercise_type_id_seq;

CREATE SEQUENCE public.exercise_type_exercise_type_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.heart_rate_heart_rate_id_seq;

CREATE SEQUENCE public.heart_rate_heart_rate_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.medication_medication_id_seq;

CREATE SEQUENCE public.medication_medication_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.nutrient_classification_classification_id_seq;

CREATE SEQUENCE public.nutrient_classification_classification_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.person_person_id_seq;

CREATE SEQUENCE public.person_person_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.renal_diet_food_renal_food_id_seq;

CREATE SEQUENCE public.renal_diet_food_renal_food_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.renal_food_category_category_id_seq;

CREATE SEQUENCE public.renal_food_category_category_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.renal_food_category_map_food_category_id_seq;

CREATE SEQUENCE public.renal_food_category_map_food_category_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.renal_food_nutrient_classification_food_class_id_seq;

CREATE SEQUENCE public.renal_food_nutrient_classification_food_class_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.severity_escalation_rule_rule_id_seq;

CREATE SEQUENCE public.severity_escalation_rule_rule_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.vital_alert_alert_id_seq;

CREATE SEQUENCE public.vital_alert_alert_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.vital_threshold_threshold_id_seq;

CREATE SEQUENCE public.vital_threshold_threshold_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;
-- DROP SEQUENCE public.weight_weight_id_seq;

CREATE SEQUENCE public.weight_weight_id_seq
	INCREMENT BY 1
	MINVALUE 1
	MAXVALUE 9223372036854775807
	START 1
	CACHE 1
	NO CYCLE;-- public.data_entry_log definition

-- Drop table

-- DROP TABLE public.data_entry_log;

CREATE TABLE public.data_entry_log (
	log_id bigserial NOT NULL,
	table_name varchar(100) NOT NULL,
	record_id int8 NOT NULL,
	action_type varchar(20) NOT NULL,
	entered_by varchar(100) NOT NULL,
	entry_timestamp timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	change_details jsonb NULL,
	CONSTRAINT data_entry_log_action_type_check CHECK (((action_type)::text = ANY ((ARRAY['INSERT'::character varying, 'UPDATE'::character varying, 'DELETE'::character varying])::text[]))),
	CONSTRAINT data_entry_log_pkey PRIMARY KEY (log_id)
);
CREATE INDEX idx_log_table_record ON public.data_entry_log USING btree (table_name, record_id);


-- public.exercise_type definition

-- Drop table

-- DROP TABLE public.exercise_type;

CREATE TABLE public.exercise_type (
	exercise_type_id bigserial NOT NULL,
	exercise_name varchar(100) NOT NULL,
	category varchar(50) NOT NULL,
	is_active bool NOT NULL DEFAULT true,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT exercise_type_pkey PRIMARY KEY (exercise_type_id),
	CONSTRAINT uq_exercise_name UNIQUE (exercise_name)
);


-- public.medication definition

-- Drop table

-- DROP TABLE public.medication;

CREATE TABLE public.medication (
	medication_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	medication_name varchar(200) NOT NULL,
	dosage_mg numeric(6, 2) NOT NULL,
	dosage_form varchar(50) NULL,
	take_morning bool NOT NULL DEFAULT false,
	take_noon bool NOT NULL DEFAULT false,
	take_evening bool NOT NULL DEFAULT false,
	take_bedtime bool NOT NULL DEFAULT false,
	instructions text NULL,
	prescribed_by varchar(200) NULL,
	start_date date NULL,
	end_date date NULL,
	is_active bool NOT NULL DEFAULT true,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	updated_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT medication_pkey PRIMARY KEY (medication_id)
);

-- Table Triggers

create trigger trg_medication_updated before
update
    on
    public.medication for each row execute function set_updated_at();


-- public.nutrient_classification definition

-- Drop table

-- DROP TABLE public.nutrient_classification;

CREATE TABLE public.nutrient_classification (
	classification_id bigserial NOT NULL,
	nutrient_name varchar(50) NOT NULL,
	classification_label varchar(20) NOT NULL,
	min_value numeric NULL,
	max_value numeric NULL,
	unit varchar(20) NOT NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT nutrient_classification_pkey PRIMARY KEY (classification_id),
	CONSTRAINT uq_nutrient_class UNIQUE (nutrient_name, classification_label)
);
CREATE INDEX idx_nutrient_class_lookup ON public.nutrient_classification USING btree (nutrient_name, classification_label);


-- public.person definition

-- Drop table

-- DROP TABLE public.person;

CREATE TABLE public.person (
	person_id bigserial NOT NULL,
	first_name varchar(100) NOT NULL,
	last_name varchar(100) NOT NULL,
	date_of_birth date NULL,
	gender varchar(20) NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	updated_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT person_pkey PRIMARY KEY (person_id)
);


-- public.renal_food_category definition

-- Drop table

-- DROP TABLE public.renal_food_category;

CREATE TABLE public.renal_food_category (
	category_id bigserial NOT NULL,
	category_name varchar(50) NOT NULL,
	description text NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT renal_food_category_pkey PRIMARY KEY (category_id),
	CONSTRAINT uq_food_category UNIQUE (category_name)
);


-- public.severity_escalation_rule definition

-- Drop table

-- DROP TABLE public.severity_escalation_rule;

CREATE TABLE public.severity_escalation_rule (
	rule_id bigserial NOT NULL,
	vital_type varchar(50) NOT NULL,
	base_severity varchar(20) NOT NULL,
	escalate_to varchar(20) NOT NULL,
	violation_count int4 NOT NULL,
	time_window_minutes int4 NOT NULL,
	requires_ack bool NOT NULL DEFAULT false,
	is_active bool NOT NULL DEFAULT true,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT severity_escalation_rule_pkey PRIMARY KEY (rule_id)
);


-- public.blood_glucose definition

-- Drop table

-- DROP TABLE public.blood_glucose;

CREATE TABLE public.blood_glucose (
	glucose_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	glucose_value int4 NOT NULL,
	glucose_unit varchar(10) NOT NULL DEFAULT 'mg/dL'::character varying,
	reading_type varchar(30) NULL,
	reading_time timestamp NOT NULL,
	notes text NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT blood_glucose_glucose_value_check CHECK ((glucose_value > 0)),
	CONSTRAINT blood_glucose_pkey PRIMARY KEY (glucose_id),
	CONSTRAINT fk_glucose_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);
CREATE INDEX idx_glucose_person_time ON public.blood_glucose USING btree (person_id, reading_time);


-- public.blood_pressure definition

-- Drop table

-- DROP TABLE public.blood_pressure;

CREATE TABLE public.blood_pressure (
	bp_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	systolic int4 NOT NULL,
	diastolic int4 NOT NULL,
	pulse int4 NULL,
	reading_time timestamp NOT NULL,
	notes text NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT blood_pressure_diastolic_check CHECK ((diastolic > 0)),
	CONSTRAINT blood_pressure_pkey PRIMARY KEY (bp_id),
	CONSTRAINT blood_pressure_pulse_check CHECK ((pulse > 0)),
	CONSTRAINT blood_pressure_systolic_check CHECK ((systolic > 0)),
	CONSTRAINT fk_bp_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);
CREATE INDEX idx_bp_person_time ON public.blood_pressure USING btree (person_id, reading_time);

-- Table Triggers

create trigger trg_log_bp_insert after
insert
    on
    public.blood_pressure for each row execute function log_blood_pressure_insert();


-- public.exercise_goal definition

-- Drop table

-- DROP TABLE public.exercise_goal;

CREATE TABLE public.exercise_goal (
	exercise_goal_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	goal_type varchar(50) NOT NULL,
	target_value int4 NOT NULL,
	start_date date NOT NULL,
	end_date date NULL,
	is_active bool NOT NULL DEFAULT true,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT exercise_goal_pkey PRIMARY KEY (exercise_goal_id),
	CONSTRAINT fk_goal_person FOREIGN KEY (person_id) REFERENCES public.person(person_id)
);


-- public.exercise_session definition

-- Drop table

-- DROP TABLE public.exercise_session;

CREATE TABLE public.exercise_session (
	exercise_session_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	exercise_type_id int8 NOT NULL,
	start_time timestamp NOT NULL,
	duration_minutes numeric(6, 2) NOT NULL,
	intensity varchar(20) NOT NULL,
	notes text NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	updated_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT exercise_session_duration_minutes_check CHECK ((duration_minutes > (0)::numeric)),
	CONSTRAINT exercise_session_intensity_check CHECK (((intensity)::text = ANY ((ARRAY['Low'::character varying, 'Moderate'::character varying, 'High'::character varying])::text[]))),
	CONSTRAINT exercise_session_pkey PRIMARY KEY (exercise_session_id),
	CONSTRAINT fk_exercise_person FOREIGN KEY (person_id) REFERENCES public.person(person_id),
	CONSTRAINT fk_exercise_type FOREIGN KEY (exercise_type_id) REFERENCES public.exercise_type(exercise_type_id)
);


-- public.heart_rate definition

-- Drop table

-- DROP TABLE public.heart_rate;

CREATE TABLE public.heart_rate (
	heart_rate_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	bpm int4 NOT NULL,
	reading_time timestamp NOT NULL,
	notes text NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT heart_rate_bpm_check CHECK ((bpm > 0)),
	CONSTRAINT heart_rate_pkey PRIMARY KEY (heart_rate_id),
	CONSTRAINT fk_hr_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);
CREATE INDEX idx_hr_person_time ON public.heart_rate USING btree (person_id, reading_time);


-- public.renal_diet_food definition

-- Drop table

-- DROP TABLE public.renal_diet_food;

CREATE TABLE public.renal_diet_food (
	renal_food_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	food_name varchar(200) NOT NULL,
	serving_size varchar(100) NULL,
	calories int4 NULL,
	sodium_mg int4 NULL,
	potassium_mg int4 NULL,
	phosphorus_mg int4 NULL,
	protein_g numeric(6, 2) NULL,
	allowed bool NOT NULL DEFAULT true,
	restriction_notes text NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	updated_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	category_id int8 NULL,
	CONSTRAINT renal_diet_food_calories_check CHECK ((calories >= 0)),
	CONSTRAINT renal_diet_food_phosphorus_mg_check CHECK ((phosphorus_mg >= 0)),
	CONSTRAINT renal_diet_food_pkey PRIMARY KEY (renal_food_id),
	CONSTRAINT renal_diet_food_potassium_mg_check CHECK ((potassium_mg >= 0)),
	CONSTRAINT renal_diet_food_protein_g_check CHECK ((protein_g >= (0)::numeric)),
	CONSTRAINT renal_diet_food_sodium_mg_check CHECK ((sodium_mg >= 0)),
	CONSTRAINT fk_renal_food_category FOREIGN KEY (category_id) REFERENCES public.renal_food_category(category_id),
	CONSTRAINT fk_renal_food_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);
CREATE INDEX idx_renal_food_allowed ON public.renal_diet_food USING btree (person_id, allowed);
CREATE INDEX idx_renal_food_person ON public.renal_diet_food USING btree (person_id);


-- public.renal_food_category_map definition

-- Drop table

-- DROP TABLE public.renal_food_category_map;

CREATE TABLE public.renal_food_category_map (
	food_category_id bigserial NOT NULL,
	renal_food_id int8 NOT NULL,
	category_id int8 NOT NULL,
	CONSTRAINT renal_food_category_map_pkey PRIMARY KEY (food_category_id),
	CONSTRAINT uq_food_category_map UNIQUE (renal_food_id, category_id),
	CONSTRAINT fk_category_category FOREIGN KEY (category_id) REFERENCES public.renal_food_category(category_id) ON DELETE CASCADE,
	CONSTRAINT fk_category_food FOREIGN KEY (renal_food_id) REFERENCES public.renal_diet_food(renal_food_id) ON DELETE CASCADE
);
CREATE INDEX idx_food_category_map ON public.renal_food_category_map USING btree (category_id);


-- public.renal_food_nutrient_classification definition

-- Drop table

-- DROP TABLE public.renal_food_nutrient_classification;

CREATE TABLE public.renal_food_nutrient_classification (
	food_class_id bigserial NOT NULL,
	renal_food_id int8 NOT NULL,
	nutrient_name varchar(50) NOT NULL,
	classification_id int8 NOT NULL,
	evaluated_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT renal_food_nutrient_classification_pkey PRIMARY KEY (food_class_id),
	CONSTRAINT uq_food_nutrient UNIQUE (renal_food_id, nutrient_name),
	CONSTRAINT fk_food_class_classification FOREIGN KEY (classification_id) REFERENCES public.nutrient_classification(classification_id) ON DELETE CASCADE,
	CONSTRAINT fk_food_class_food FOREIGN KEY (renal_food_id) REFERENCES public.renal_diet_food(renal_food_id) ON DELETE CASCADE
);
CREATE INDEX idx_food_nutrient_class ON public.renal_food_nutrient_classification USING btree (renal_food_id);


-- public.vital_threshold definition

-- Drop table

-- DROP TABLE public.vital_threshold;

CREATE TABLE public.vital_threshold (
	threshold_id bigserial NOT NULL,
	vital_type varchar(50) NOT NULL,
	person_id int8 NULL,
	min_value numeric NULL,
	max_value numeric NULL,
	severity varchar(20) NOT NULL DEFAULT 'medium'::character varying,
	is_active bool NOT NULL DEFAULT true,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT vital_threshold_pkey PRIMARY KEY (threshold_id),
	CONSTRAINT fk_threshold_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);


-- public.weight definition

-- Drop table

-- DROP TABLE public.weight;

CREATE TABLE public.weight (
	weight_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	weight_value numeric(6, 2) NOT NULL,
	weight_unit varchar(10) NOT NULL DEFAULT 'lb'::character varying,
	reading_time timestamp NOT NULL,
	notes text NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	height_cm numeric(6, 2) NULL,
	height_ft numeric(4, 2) NULL,
	CONSTRAINT weight_height_ft_check CHECK (((height_ft IS NULL) OR ((height_ft >= 3.0) AND (height_ft <= 8.5)))),
	CONSTRAINT weight_pkey PRIMARY KEY (weight_id),
	CONSTRAINT weight_weight_value_check CHECK ((weight_value > (0)::numeric)),
	CONSTRAINT fk_weight_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);
CREATE INDEX idx_weight_person_time ON public.weight USING btree (person_id, reading_time);


-- public.vital_alert definition

-- Drop table

-- DROP TABLE public.vital_alert;

CREATE TABLE public.vital_alert (
	alert_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	vital_type varchar(50) NOT NULL,
	reading_id int8 NOT NULL,
	reading_value numeric NOT NULL,
	threshold_id int8 NOT NULL,
	alert_message text NULL,
	severity varchar(20) NOT NULL,
	alert_time timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	acknowledged bool NOT NULL DEFAULT false,
	acknowledged_at timestamp NULL,
	parent_alert_id int8 NULL,
	escalation_level int4 NOT NULL DEFAULT 0,
	CONSTRAINT vital_alert_pkey PRIMARY KEY (alert_id),
	CONSTRAINT fk_alert_person FOREIGN KEY (person_id) REFERENCES public.person(person_id),
	CONSTRAINT fk_alert_threshold FOREIGN KEY (threshold_id) REFERENCES public.vital_threshold(threshold_id),
	CONSTRAINT fk_parent_alert FOREIGN KEY (parent_alert_id) REFERENCES public.vital_alert(alert_id)
);

-- Table Triggers

create trigger trg_escalate_severity after
insert
    on
    public.vital_alert for each row execute function evaluate_severity_escalation();



CREATE OR REPLACE FUNCTION public.evaluate_severity_escalation()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
DECLARE
    rule RECORD;
    violation_total INTEGER;
BEGIN
    FOR rule IN
        SELECT *
        FROM severity_escalation_rule
        WHERE vital_type = NEW.vital_type
          AND base_severity = NEW.severity
          AND is_active = TRUE
    LOOP
        SELECT COUNT(*)
        INTO violation_total
        FROM vital_alert
        WHERE person_id = NEW.person_id
          AND vital_type = NEW.vital_type
          AND severity = rule.base_severity
          AND alert_time >= (NEW.alert_time - INTERVAL '1 minute' * rule.time_window_minutes);

        IF violation_total >= rule.violation_count THEN
            INSERT INTO vital_alert (
                person_id,
                vital_type,
                reading_id,
                reading_value,
                threshold_id,
                alert_message,
                severity,
                parent_alert_id,
                escalation_level
            )
            VALUES (
                NEW.person_id,
                NEW.vital_type,
                NEW.reading_id,
                NEW.reading_value,
                NEW.threshold_id,
                'Severity escalated due to repeated violations',
                rule.escalate_to,
                NEW.alert_id,
                NEW.escalation_level + 1
            );
        END IF;
    END LOOP;

    RETURN NEW;
END;
$function$
;

CREATE OR REPLACE FUNCTION public.log_blood_pressure_insert()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'blood_pressure',
        NEW.bp_id,
        'INSERT',
        current_user,
        to_jsonb(NEW)
    );

    RETURN NEW;
END;
$function$
;

CREATE OR REPLACE FUNCTION public.set_updated_at()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$function$
;

CREATE OR REPLACE PROCEDURE public.sp_deactivate_medication(IN p_medication_id bigint, IN p_entered_by text)
 LANGUAGE plpgsql
AS $procedure$
BEGIN
    UPDATE medication
    SET is_active = false,
        updated_at = CURRENT_TIMESTAMP
    WHERE medication_id = p_medication_id;

	INSERT INTO data_entry_log (
	    table_name,
	    record_id,
	    action_type,
	    entered_by,
	    change_details
	)
	VALUES (
	    'medication',
	    p_medication_id,
	    'UPDATE',
	    p_entered_by,
	    jsonb_build_object('is_active', false)
	);

END;
$procedure$
;

CREATE OR REPLACE FUNCTION public.sp_delete_blood_glucose(p_glucose_id bigint, p_entered_by text)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Log BEFORE delete (so data is preserved)
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    SELECT
        'blood_glucose',
        glucose_id,
        'DELETE',
        p_entered_by,
        jsonb_build_object(
            'person_id', person_id,
            'glucose_value', glucose_value,
            'reading_time', reading_time,
            'notes', notes
        )
    FROM blood_glucose
    WHERE glucose_id = p_glucose_id;

    -- Delete record
    DELETE FROM blood_glucose
    WHERE glucose_id = p_glucose_id;
END;
$function$
;

CREATE OR REPLACE PROCEDURE public.sp_delete_exercise_session(IN p_exercise_session_id bigint, IN p_entered_by text)
 LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_deleted_record jsonb;
BEGIN
    -- Capture record before delete
    SELECT to_jsonb(es)
    INTO v_deleted_record
    FROM exercise_session es
    WHERE es.exercise_session_id = p_exercise_session_id;

    -- Delete the record
    DELETE FROM exercise_session
    WHERE exercise_session_id = p_exercise_session_id;

    -- Log deletion
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'exercise_session',
        p_exercise_session_id,
        'DELETE',
        p_entered_by,
        v_deleted_record
    );
END;
$procedure$
;

CREATE OR REPLACE FUNCTION public.sp_delete_weight(p_weight_id bigint, p_entered_by text)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Log the record BEFORE deletion
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    SELECT
        'weight',
        weight_id,
        'DELETE',
        p_entered_by,
        jsonb_build_object(
            'person_id', person_id,
            'weight_value', weight_value,
            'weight_unit', weight_unit,
            'reading_time', reading_time,
            'notes', notes
        )
    FROM weight
    WHERE weight_id = p_weight_id;

    -- Delete the record
    DELETE FROM weight
    WHERE weight_id = p_weight_id;
END;
$function$
;

CREATE OR REPLACE FUNCTION public.sp_get_bp_trend(p_person_id bigint, p_start_date timestamp without time zone, p_end_date timestamp without time zone)
 RETURNS TABLE(reading_time timestamp without time zone, systolic integer, diastolic integer)
 LANGUAGE sql
AS $function$
    SELECT reading_time,
           systolic,
           diastolic
    FROM blood_pressure
    WHERE person_id = p_person_id
      AND reading_time >= p_start_date
      AND reading_time <  p_end_date
    ORDER BY reading_time;
$function$
;

CREATE OR REPLACE FUNCTION public.sp_get_exercise_history(p_person_id bigint)
 RETURNS TABLE(exercise_session_id bigint, exercise_type_id bigint, exercise_name text, start_time timestamp without time zone, duration_minutes integer, intensity text, notes text)
 LANGUAGE sql
AS $function$
    SELECT
        es.exercise_session_id,
        es.exercise_type_id,
        et.exercise_name,
        es.start_time,
        es.duration_minutes,
        es.intensity,
        es.notes
    FROM exercise_session es
    JOIN exercise_type et
      ON et.exercise_type_id = es.exercise_type_id
    WHERE es.person_id = p_person_id
    ORDER BY es.start_time DESC;
$function$
;

CREATE OR REPLACE FUNCTION public.sp_insert_blood_glucose(p_person_id bigint, p_glucose_value integer, p_reading_time timestamp without time zone, p_notes text, p_entered_by text)
 RETURNS bigint
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_glucose_id bigint;
BEGIN
    -- Insert glucose reading
    INSERT INTO blood_glucose
        (person_id, glucose_value, reading_time, notes)
    VALUES
        (p_person_id, p_glucose_value, p_reading_time, p_notes)
    RETURNING glucose_id
    INTO v_glucose_id;

    -- Log the data entry (explicit JSONB)
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'blood_glucose',
        v_glucose_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'person_id', p_person_id,
            'glucose_value', p_glucose_value,
            'reading_time', p_reading_time,
            'notes', p_notes
        )
    );

    RETURN v_glucose_id;
END;
$function$
;

CREATE OR REPLACE FUNCTION public.sp_insert_blood_pressure(p_person_id bigint, p_systolic integer, p_diastolic integer, p_pulse integer, p_reading_time timestamp without time zone, p_notes text, p_entered_by character varying)
 RETURNS bigint
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_bp_id         BIGINT;
    v_threshold     RECORD;
    v_value         INTEGER;
    v_vital_type    VARCHAR(50);
BEGIN
    ------------------------------------------------------------------
    -- Validation
    ------------------------------------------------------------------
    IF p_systolic <= 0 OR p_diastolic <= 0 THEN
        RAISE EXCEPTION 'Blood pressure values must be greater than zero';
    END IF;

    IF p_systolic < p_diastolic THEN
        RAISE EXCEPTION 'Systolic must be greater than diastolic';
    END IF;

    ------------------------------------------------------------------
    -- Insert blood pressure reading
    ------------------------------------------------------------------
    INSERT INTO blood_pressure (
        person_id,
        systolic,
        diastolic,
        pulse,
        reading_time,
        notes
    )
    VALUES (
        p_person_id,
        p_systolic,
        p_diastolic,
        p_pulse,
        p_reading_time,
        p_notes
    )
    RETURNING bp_id INTO v_bp_id;

    ------------------------------------------------------------------
    -- Audit log
    ------------------------------------------------------------------
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'blood_pressure',
        v_bp_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'systolic', p_systolic,
            'diastolic', p_diastolic,
            'pulse', p_pulse,
            'reading_time', p_reading_time
        )
    );

    ------------------------------------------------------------------
    -- Alert evaluation loop (systolic & diastolic)
    ------------------------------------------------------------------
    FOR v_vital_type, v_value IN
        SELECT * FROM (
            VALUES
            ('blood_pressure_systolic', p_systolic),
            ('blood_pressure_diastolic', p_diastolic)
        ) AS v(vital_type, value)
    LOOP
        -- Resolve threshold: person-specific first, fallback to global
        SELECT *
        INTO v_threshold
        FROM vital_threshold
        WHERE vital_type = v_vital_type
          AND is_active = TRUE
          AND (person_id = p_person_id OR person_id IS NULL)
        ORDER BY person_id DESC
        LIMIT 1;

        -- Skip if no threshold defined
        IF NOT FOUND THEN
            CONTINUE;
        END IF;

        -- Evaluate threshold breach
        IF (v_threshold.min_value IS NOT NULL AND v_value < v_threshold.min_value)
           OR (v_threshold.max_value IS NOT NULL AND v_value > v_threshold.max_value)
        THEN
            INSERT INTO vital_alert (
                person_id,
                vital_type,
                reading_id,
                reading_value,
                threshold_id,
                alert_message,
                severity
            )
            VALUES (
                p_person_id,
                v_vital_type,
                v_bp_id,
                v_value,
                v_threshold.threshold_id,
                format(
                    '%s value %s outside threshold (%s - %s)',
                    v_vital_type,
                    v_value,
                    COALESCE(v_threshold.min_value::TEXT, '∞'),
                    COALESCE(v_threshold.max_value::TEXT, '∞')
                ),
                v_threshold.severity
            );
        END IF;
    END LOOP;

    ------------------------------------------------------------------
    -- Return inserted BP ID
    ------------------------------------------------------------------
    RETURN v_bp_id;
END;
$function$
;

CREATE OR REPLACE PROCEDURE public.sp_insert_exercise_session(IN p_person_id bigint, IN p_exercise_type_id bigint, IN p_start_time timestamp without time zone, IN p_duration_minutes integer, IN p_intensity character varying, IN p_notes text, IN p_entered_by text)
 LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_exercise_session_id bigint;
BEGIN
    INSERT INTO exercise_session (
        person_id,
        exercise_type_id,
        start_time,
        duration_minutes,
        intensity,
        notes
    )
    VALUES (
        p_person_id,
        p_exercise_type_id,
        p_start_time,
        p_duration_minutes,
        p_intensity,
        p_notes
    )
    RETURNING exercise_session_id
    INTO v_exercise_session_id;

    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'exercise_session',
        v_exercise_session_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'person_id', p_person_id,
            'exercise_type_id', p_exercise_type_id,
            'start_time', p_start_time,
            'duration_minutes', p_duration_minutes,
            'intensity', p_intensity,
            'notes', p_notes
        )
    );
END;
$procedure$
;

CREATE OR REPLACE FUNCTION public.sp_insert_medication(p_person_id bigint, p_medication_name character varying, p_dosage_mg numeric, p_dosage_form character varying, p_take_morning boolean, p_take_noon boolean, p_take_evening boolean, p_take_bedtime boolean, p_instructions text, p_prescribed_by character varying, p_start_date date, p_end_date date)
 RETURNS bigint
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_medication_id bigint;
BEGIN
    INSERT INTO public.medication (
        person_id,
        medication_name,
        dosage_mg,
        dosage_form,
        take_morning,
        take_noon,
        take_evening,
        take_bedtime,
        instructions,
        prescribed_by,
        start_date,
        end_date
    )
    VALUES (
        p_person_id,
        p_medication_name,
        p_dosage_mg,
        p_dosage_form,
        p_take_morning,
        p_take_noon,
        p_take_evening,
        p_take_bedtime,
        p_instructions,
        p_prescribed_by,
        p_start_date,
        p_end_date
    )
    RETURNING medication_id
    INTO v_medication_id;

    RETURN v_medication_id;
END;
$function$
;

CREATE OR REPLACE PROCEDURE public.sp_insert_medication(IN p_person_id bigint, IN p_medication_name text, IN p_dosage_mg numeric, IN p_dosage_form text, IN p_take_morning boolean, IN p_take_noon boolean, IN p_take_evening boolean, IN p_take_bedtime boolean, IN p_instructions text, IN p_prescribed_by text, IN p_start_date timestamp without time zone, IN p_end_date timestamp without time zone)
 LANGUAGE plpgsql
AS $procedure$
BEGIN
    INSERT INTO medication (
        person_id,
        medication_name,
        dosage_mg,
        dosage_form,
        take_morning,
        take_noon,
        take_evening,
        take_bedtime,
        instructions,
        prescribed_by,
        start_date,
        end_date
    )
    VALUES (
        p_person_id,
        p_medication_name,
        p_dosage_mg,
        p_dosage_form,
        p_take_morning,
        p_take_noon,
        p_take_evening,
        p_take_bedtime,
        p_instructions,
        p_prescribed_by,
        p_start_date,
        p_end_date
    );
END;
$procedure$
;

CREATE OR REPLACE FUNCTION public.sp_insert_renal_diet_food(p_person_id bigint, p_food_name character varying, p_serving_size character varying, p_calories integer, p_sodium_mg integer, p_potassium_mg integer, p_phosphorus_mg integer, p_protein_g numeric, p_allowed boolean, p_restriction_notes text, p_entered_by character varying)
 RETURNS bigint
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_renal_food_id BIGINT;
BEGIN
    ------------------------------------------------------------------
    -- Validation
    ------------------------------------------------------------------
    IF p_food_name IS NULL OR LENGTH(TRIM(p_food_name)) = 0 THEN
        RAISE EXCEPTION 'Food name is required';
    END IF;

    IF p_calories < 0
       OR p_sodium_mg < 0
       OR p_potassium_mg < 0
       OR p_phosphorus_mg < 0
       OR p_protein_g < 0
    THEN
        RAISE EXCEPTION 'Nutrient values cannot be negative';
    END IF;

    ------------------------------------------------------------------
    -- Insert renal diet food
    ------------------------------------------------------------------
    INSERT INTO renal_diet_food (
        person_id,
        food_name,
        serving_size,
        calories,
        sodium_mg,
        potassium_mg,
        phosphorus_mg,
        protein_g,
        allowed,
        restriction_notes
    )
    VALUES (
        p_person_id,
        p_food_name,
        p_serving_size,
        p_calories,
        p_sodium_mg,
        p_potassium_mg,
        p_phosphorus_mg,
        p_protein_g,
        COALESCE(p_allowed, TRUE),
        p_restriction_notes
    )
    RETURNING renal_food_id INTO v_renal_food_id;

    ------------------------------------------------------------------
    -- Audit log
    ------------------------------------------------------------------
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'renal_diet_food',
        v_renal_food_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'food_name', p_food_name,
            'serving_size', p_serving_size,
            'calories', p_calories,
            'sodium_mg', p_sodium_mg,
            'potassium_mg', p_potassium_mg,
            'phosphorus_mg', p_phosphorus_mg,
            'protein_g', p_protein_g,
            'allowed', p_allowed
        )
    );

    ------------------------------------------------------------------
    -- Return ID
    ------------------------------------------------------------------
    RETURN v_renal_food_id;
END;
$function$
;

CREATE OR REPLACE FUNCTION public.sp_insert_weight(p_person_id bigint, p_weight_value numeric, p_weight_unit character varying, p_reading_time timestamp without time zone, p_notes text, p_entered_by text)
 RETURNS bigint
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_weight_id bigint;
    v_height_ft numeric(4,2) := 6.0833;  -- 6 ft 1 in
BEGIN
    -- Insert weight record
    INSERT INTO public.weight
        (person_id, weight_value, weight_unit, height_ft, reading_time, notes)
    VALUES
        (p_person_id, p_weight_value, p_weight_unit, v_height_ft, p_reading_time, p_notes)
    RETURNING weight_id
    INTO v_weight_id;

    -- Log the data entry
    INSERT INTO public.data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'weight',
        v_weight_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'person_id', p_person_id,
            'weight_value', p_weight_value,
            'weight_unit', p_weight_unit,
            'height_ft', v_height_ft,
            'reading_time', p_reading_time,
            'notes', p_notes
        )
    );

    RETURN v_weight_id;
END;
$function$
;

CREATE OR REPLACE FUNCTION public.sp_insert_weight(p_person_id bigint, p_weight_value numeric, p_weight_unit character varying, p_height_ft numeric, p_reading_time timestamp without time zone, p_notes text, p_entered_by text)
 RETURNS bigint
 LANGUAGE plpgsql
AS $function$
DECLARE
    v_weight_id bigint;
    v_height_ft numeric(4,2);
BEGIN
    /*
        Height resolution priority:
        1) Explicit height passed in
        2) Most recent height for this person
        3) Default to 6 ft 1 in (6.0833)
    */
    v_height_ft :=
        COALESCE(
            p_height_ft,
            (
                SELECT height_ft
                FROM weight
                WHERE person_id = p_person_id
                  AND height_ft IS NOT NULL
                ORDER BY reading_time DESC
                LIMIT 1
            ),
            6.0833
        );

    -- Insert weight record
    INSERT INTO public.weight
        (person_id, weight_value, weight_unit, height_ft, reading_time, notes)
    VALUES
        (p_person_id, p_weight_value, p_weight_unit, v_height_ft, p_reading_time, p_notes)
    RETURNING weight_id
    INTO v_weight_id;

    -- Audit log
    INSERT INTO public.data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    VALUES (
        'weight',
        v_weight_id,
        'INSERT',
        p_entered_by,
        jsonb_build_object(
            'person_id', p_person_id,
            'weight_value', p_weight_value,
            'weight_unit', p_weight_unit,
            'height_ft', v_height_ft,
            'reading_time', p_reading_time,
            'notes', p_notes
        )
    );

    RETURN v_weight_id;
END;
$function$
;

CREATE OR REPLACE PROCEDURE public.sp_update_medication(IN p_medication_id bigint, IN p_dosage_mg numeric, IN p_dosage_form text, IN p_take_morning boolean, IN p_take_noon boolean, IN p_take_evening boolean, IN p_take_bedtime boolean, IN p_instructions text, IN p_prescribed_by text)
 LANGUAGE plpgsql
AS $procedure$
BEGIN
    UPDATE medication
    SET dosage_mg = p_dosage_mg,
        dosage_form = p_dosage_form,
        take_morning = p_take_morning,
        take_noon = p_take_noon,
        take_evening = p_take_evening,
        take_bedtime = p_take_bedtime,
        instructions = p_instructions,
        prescribed_by = p_prescribed_by,
        updated_at = CURRENT_TIMESTAMP
    WHERE medication_id = p_medication_id;
END;
$procedure$
;

CREATE OR REPLACE FUNCTION public.sp_update_weight(p_weight_id bigint, p_weight_value numeric, p_weight_unit text, p_reading_time timestamp without time zone, p_notes text, p_entered_by text)
 RETURNS void
 LANGUAGE plpgsql
AS $function$
BEGIN
    -- Log BEFORE update
    INSERT INTO data_entry_log (
        table_name,
        record_id,
        action_type,
        entered_by,
        change_details
    )
    SELECT
        'weight',
        weight_id,
        'UPDATE',
        p_entered_by,
        jsonb_build_object(
            'before', jsonb_build_object(
                'weight_value', weight_value,
                'weight_unit', weight_unit,
                'reading_time', reading_time,
                'notes', notes
            ),
            'after', jsonb_build_object(
                'weight_value', p_weight_value,
                'weight_unit', p_weight_unit,
                'reading_time', p_reading_time,
                'notes', p_notes
            )
        )
    FROM weight
    WHERE weight_id = p_weight_id;

    -- Update record
    UPDATE weight
    SET
        weight_value = p_weight_value,
        weight_unit  = p_weight_unit,
        reading_time = p_reading_time,
        notes        = p_notes
    WHERE weight_id = p_weight_id;
END;
$function$
;
