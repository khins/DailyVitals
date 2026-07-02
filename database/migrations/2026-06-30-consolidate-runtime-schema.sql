-- Consolidates every schema mutation that previously ran inside application services.
-- This migration is intentionally idempotent so an existing developer database can
-- be adopted into the migration history safely.

ALTER TABLE public.person
    ADD COLUMN IF NOT EXISTS height_ft numeric(5, 2) NULL,
    ADD COLUMN IF NOT EXISTS birth_date date NULL,
    ADD COLUMN IF NOT EXISTS gender text NULL,
    ADD COLUMN IF NOT EXISTS is_diabetic boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS glucose_target_mg_dl int4 NULL,
    ADD COLUMN IF NOT EXISTS track_kidney_labs boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS track_weight_loss boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL;

ALTER TABLE public.blood_glucose
    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL;
UPDATE public.blood_glucose SET updated_at = created_at WHERE updated_at IS NULL;
ALTER TABLE public.blood_glucose
    ALTER COLUMN updated_at SET DEFAULT CURRENT_TIMESTAMP,
    ALTER COLUMN updated_at SET NOT NULL;

ALTER TABLE public.blood_pressure
    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL;
UPDATE public.blood_pressure SET updated_at = created_at WHERE updated_at IS NULL;
ALTER TABLE public.blood_pressure
    ALTER COLUMN updated_at SET DEFAULT CURRENT_TIMESTAMP,
    ALTER COLUMN updated_at SET NOT NULL;

ALTER TABLE public.exercise_session
    ADD COLUMN IF NOT EXISTS created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL,
    ADD COLUMN IF NOT EXISTS calories_expended numeric(8, 2) NULL;

ALTER TABLE public.weight
    ADD COLUMN IF NOT EXISTS created_at timestamp NULL,
    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL;
UPDATE public.weight
SET created_at = COALESCE(created_at, reading_time, CURRENT_TIMESTAMP)
WHERE created_at IS NULL;
UPDATE public.weight
SET updated_at = COALESCE(updated_at, created_at, reading_time, CURRENT_TIMESTAMP)
WHERE updated_at IS NULL;
ALTER TABLE public.weight
    ALTER COLUMN created_at SET DEFAULT CURRENT_TIMESTAMP,
    ALTER COLUMN created_at SET NOT NULL,
    ALTER COLUMN updated_at SET DEFAULT CURRENT_TIMESTAMP,
    ALTER COLUMN updated_at SET NOT NULL;

CREATE TABLE IF NOT EXISTS public.fluid_intake (
    fluid_intake_id bigserial NOT NULL,
    person_id int8 NOT NULL,
    consumed_at timestamp NOT NULL,
    fluid_ml int4 NOT NULL,
    beverage_name varchar(120) NOT NULL,
    notes text NULL,
    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp NULL,
    CONSTRAINT fluid_intake_pkey PRIMARY KEY (fluid_intake_id),
    CONSTRAINT fluid_intake_fluid_ml_check CHECK (fluid_ml > 0)
);

CREATE TABLE IF NOT EXISTS public.nutrition_goal (
    nutrition_goal_id bigserial NOT NULL,
    person_id int8 NOT NULL,
    sodium_limit_mg int4 NOT NULL,
    phosphorus_limit_mg int4 NOT NULL,
    calorie_limit int4 NOT NULL,
    effective_date date NOT NULL,
    protein_target_g int4 NULL,
    potassium_limit_mg int4 NULL,
    fluid_limit_ml int4 NULL,
    CONSTRAINT nutrition_goal_pkey PRIMARY KEY (nutrition_goal_id)
);
ALTER TABLE public.nutrition_goal
    ADD COLUMN IF NOT EXISTS protein_target_g int4 NULL,
    ADD COLUMN IF NOT EXISTS potassium_limit_mg int4 NULL,
    ADD COLUMN IF NOT EXISTS fluid_limit_ml int4 NULL;
CREATE INDEX IF NOT EXISTS idx_nutrition_goal_person_effective_date
    ON public.nutrition_goal (person_id, effective_date DESC);

CREATE TABLE IF NOT EXISTS public.nutrition_coach_review (
    nutrition_coach_review_id bigserial NOT NULL,
    person_id int8 NOT NULL,
    period_start date NOT NULL,
    period_end date NOT NULL,
    days_logged int4 NOT NULL,
    model text NOT NULL,
    snapshot_json text NOT NULL,
    api_response_text text NOT NULL,
    review_json text NULL,
    http_status int4 NOT NULL,
    is_success boolean NOT NULL,
    error_message text NULL,
    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT nutrition_coach_review_pkey PRIMARY KEY (nutrition_coach_review_id)
);
CREATE INDEX IF NOT EXISTS ix_nutrition_coach_review_person_period
    ON public.nutrition_coach_review (person_id, period_end DESC, created_at DESC);

CREATE TABLE IF NOT EXISTS public.login_user (
    login_user_id bigserial NOT NULL,
    person_id int8 NULL,
    user_name varchar(100) NOT NULL,
    password_hash text NOT NULL,
    password_salt text NOT NULL,
    password_iterations int4 NOT NULL,
    password_algorithm varchar(50) NOT NULL DEFAULT 'PBKDF2-SHA256',
    is_active boolean NOT NULL DEFAULT true,
    is_demo boolean NOT NULL DEFAULT false,
    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp NULL,
    last_login_at timestamp NULL,
    CONSTRAINT login_user_pkey PRIMARY KEY (login_user_id)
);
ALTER TABLE public.login_user
    ADD COLUMN IF NOT EXISTS person_id int8 NULL,
    ADD COLUMN IF NOT EXISTS password_algorithm varchar(50) NOT NULL DEFAULT 'PBKDF2-SHA256',
    ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS is_demo boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL,
    ADD COLUMN IF NOT EXISTS last_login_at timestamp NULL;
CREATE UNIQUE INDEX IF NOT EXISTS login_user_user_name_lower_key
    ON public.login_user (lower(user_name));

CREATE TABLE IF NOT EXISTS public.food_phosphorus_food (
    food_phosphorus_food_id bigserial NOT NULL,
    person_id int8 NOT NULL,
    food_name varchar(200) NOT NULL,
    default_phosphorus_mg int4 NULL,
    default_calories int4 NULL,
    default_sodium_mg int4 NULL,
    default_protein_g numeric(8, 2) NULL,
    default_potassium_mg int4 NULL,
    default_binders int4 NULL,
    default_serving_description varchar(200) NULL,
    food_notes text NULL,
    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT food_phosphorus_food_pkey PRIMARY KEY (food_phosphorus_food_id)
);

CREATE TABLE IF NOT EXISTS public.food_phosphorus_food_note (
    food_phosphorus_food_note_id bigserial NOT NULL,
    food_phosphorus_food_id int8 NOT NULL,
    note_text text NULL,
    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT food_phosphorus_food_note_pkey PRIMARY KEY (food_phosphorus_food_note_id)
);

ALTER TABLE public.food_phosphorus_intake
    ADD COLUMN IF NOT EXISTS food_phosphorus_food_id int8 NULL,
    ADD COLUMN IF NOT EXISTS binders integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS calories integer NULL,
    ADD COLUMN IF NOT EXISTS sodium_mg integer NULL,
    ADD COLUMN IF NOT EXISTS protein_g numeric(8, 2) NULL,
    ADD COLUMN IF NOT EXISTS potassium_mg integer NULL,
    ADD COLUMN IF NOT EXISTS fluid_ml integer NULL,
    ADD COLUMN IF NOT EXISTS serving_description varchar(200) NULL,
    ADD COLUMN IF NOT EXISTS estimated_by_ai boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS ai_provider varchar(50) NULL,
    ADD COLUMN IF NOT EXISTS ai_confidence varchar(20) NULL,
    ADD COLUMN IF NOT EXISTS source_notes text NULL,
    ADD COLUMN IF NOT EXISTS created_at timestamp NULL,
    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL;
UPDATE public.food_phosphorus_intake
SET created_at = COALESCE(created_at, consumed_at, CURRENT_TIMESTAMP)
WHERE created_at IS NULL;
UPDATE public.food_phosphorus_intake
SET updated_at = COALESCE(updated_at, created_at, consumed_at, CURRENT_TIMESTAMP)
WHERE updated_at IS NULL;
ALTER TABLE public.food_phosphorus_intake
    ALTER COLUMN created_at SET DEFAULT CURRENT_TIMESTAMP,
    ALTER COLUMN created_at SET NOT NULL,
    ALTER COLUMN updated_at SET DEFAULT CURRENT_TIMESTAMP,
    ALTER COLUMN updated_at SET NOT NULL;

CREATE TABLE IF NOT EXISTS public.demo_seed_state (
    person_id int8 NOT NULL PRIMARY KEY,
    seed_version int4 NOT NULL,
    anchor_date date NOT NULL,
    seeded_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
);

DO $$
DECLARE
    child_table text;
    constraint_name text;
BEGIN
    FOREACH child_table IN ARRAY ARRAY[
        'blood_pressure', 'blood_glucose', 'weight', 'kidney_lab_result',
        'food_phosphorus_intake', 'food_phosphorus_food', 'exercise_session',
        'medication', 'nutrition_goal', 'nutrition_coach_review', 'fluid_intake',
        'renal_diet_food', 'login_user'
    ]
    LOOP
        constraint_name := child_table || '_person_id_fkey';
        IF to_regclass('public.' || child_table) IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM pg_constraint c
               JOIN pg_class t ON t.oid = c.conrelid
               JOIN pg_namespace n ON n.oid = t.relnamespace
               WHERE n.nspname = 'public'
                 AND t.relname = child_table
                 AND c.conname = constraint_name
           )
        THEN
            EXECUTE format(
                'ALTER TABLE public.%I ADD CONSTRAINT %I FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE RESTRICT NOT VALID',
                child_table, constraint_name);
        END IF;
    END LOOP;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'food_phosphorus_food_person_food_name_key') THEN
        ALTER TABLE public.food_phosphorus_food
            ADD CONSTRAINT food_phosphorus_food_person_food_name_key UNIQUE (person_id, food_name);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'food_phosphorus_intake_food_fk') THEN
        ALTER TABLE public.food_phosphorus_intake
            ADD CONSTRAINT food_phosphorus_intake_food_fk
            FOREIGN KEY (food_phosphorus_food_id)
            REFERENCES public.food_phosphorus_food(food_phosphorus_food_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'food_phosphorus_food_note_food_key') THEN
        ALTER TABLE public.food_phosphorus_food_note
            ADD CONSTRAINT food_phosphorus_food_note_food_key UNIQUE (food_phosphorus_food_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'food_phosphorus_food_note_food_fk') THEN
        ALTER TABLE public.food_phosphorus_food_note
            ADD CONSTRAINT food_phosphorus_food_note_food_fk
            FOREIGN KEY (food_phosphorus_food_id)
            REFERENCES public.food_phosphorus_food(food_phosphorus_food_id);
    END IF;
END $$;

CREATE OR REPLACE FUNCTION public.prevent_demo_person_write()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    target_person_id bigint;
BEGIN
    IF current_setting('dailyvitals.allow_demo_write', TRUE) = 'on' THEN
        IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
        RETURN NEW;
    END IF;
    IF TG_OP = 'DELETE' THEN target_person_id := OLD.person_id;
    ELSE target_person_id := NEW.person_id;
    END IF;
    IF target_person_id IS NOT NULL AND EXISTS (
        SELECT 1 FROM public.login_user
        WHERE person_id = target_person_id AND is_demo = TRUE
    ) THEN
        RAISE EXCEPTION 'Demo Mode is read-only.' USING ERRCODE = '42501';
    END IF;
    IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
    RETURN NEW;
END;
$$;

DO $$
DECLARE
    protected_table record;
BEGIN
    FOR protected_table IN
        SELECT columns.table_name
        FROM information_schema.columns
        WHERE columns.table_schema = 'public'
          AND columns.column_name = 'person_id'
          AND columns.table_name NOT IN ('login_user', 'demo_seed_state')
        GROUP BY columns.table_name
    LOOP
        EXECUTE format('DROP TRIGGER IF EXISTS trg_prevent_demo_write ON public.%I', protected_table.table_name);
        EXECUTE format(
            'CREATE TRIGGER trg_prevent_demo_write BEFORE INSERT OR UPDATE OR DELETE ON public.%I FOR EACH ROW EXECUTE FUNCTION public.prevent_demo_person_write()',
            protected_table.table_name);
    END LOOP;
END $$;
