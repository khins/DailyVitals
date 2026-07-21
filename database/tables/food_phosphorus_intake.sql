CREATE TABLE IF NOT EXISTS public.food_phosphorus_intake (
    food_phosphorus_intake_id bigserial NOT NULL,
    person_id int8 NOT NULL,
    food_name varchar(200) NOT NULL,
    phosphorus_mg integer NOT NULL,
    calories integer NULL,
    sodium_mg integer NULL,
    protein_g numeric(8, 2) NULL,
    potassium_mg integer NULL,
    fluid_ml integer NULL,
    binders integer NOT NULL DEFAULT 0,
    consumed_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    notes text NULL,
    serving_description text NULL,
    estimated_by_ai boolean NOT NULL DEFAULT false,
    ai_provider varchar(50) NULL,
    ai_confidence varchar(20) NULL,
    source_notes text NULL,
    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT food_phosphorus_intake_pkey PRIMARY KEY (food_phosphorus_intake_id),
    CONSTRAINT food_phosphorus_intake_food_name_check CHECK (length(trim(food_name)) > 0),
    CONSTRAINT food_phosphorus_intake_phosphorus_check CHECK (phosphorus_mg >= 0),
    CONSTRAINT food_phosphorus_intake_calories_check CHECK (calories IS NULL OR calories >= 0),
    CONSTRAINT food_phosphorus_intake_sodium_check CHECK (sodium_mg IS NULL OR sodium_mg >= 0),
    CONSTRAINT food_phosphorus_intake_protein_check CHECK (protein_g IS NULL OR protein_g >= 0),
    CONSTRAINT food_phosphorus_intake_potassium_check CHECK (potassium_mg IS NULL OR potassium_mg >= 0),
    CONSTRAINT food_phosphorus_intake_fluid_check CHECK (fluid_ml IS NULL OR fluid_ml >= 0),
    CONSTRAINT food_phosphorus_intake_binders_check CHECK (binders >= 0),
    CONSTRAINT fk_food_phosphorus_intake_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_food_phosphorus_intake_person_date
    ON public.food_phosphorus_intake USING btree (person_id, consumed_at DESC);
