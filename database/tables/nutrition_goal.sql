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
    sugar_limit_g int4 NULL,
    phosphorus_enabled boolean NOT NULL DEFAULT true,
    sodium_enabled boolean NOT NULL DEFAULT true,
    calorie_enabled boolean NOT NULL DEFAULT true,
    protein_enabled boolean NOT NULL DEFAULT true,
    potassium_enabled boolean NOT NULL DEFAULT true,
    fluid_enabled boolean NOT NULL DEFAULT true,
    CONSTRAINT nutrition_goal_pkey PRIMARY KEY (nutrition_goal_id),
    CONSTRAINT nutrition_goal_sodium_limit_check CHECK (sodium_limit_mg >= 0),
    CONSTRAINT nutrition_goal_phosphorus_limit_check CHECK (phosphorus_limit_mg >= 0),
    CONSTRAINT nutrition_goal_calorie_limit_check CHECK (calorie_limit >= 0),
    CONSTRAINT nutrition_goal_protein_target_check CHECK (protein_target_g IS NULL OR protein_target_g >= 0),
    CONSTRAINT nutrition_goal_potassium_limit_check CHECK (potassium_limit_mg IS NULL OR potassium_limit_mg >= 0),
    CONSTRAINT nutrition_goal_fluid_limit_check CHECK (fluid_limit_ml IS NULL OR fluid_limit_ml >= 0),
    CONSTRAINT nutrition_goal_sugar_limit_check CHECK (sugar_limit_g IS NULL OR sugar_limit_g > 0),
    CONSTRAINT fk_nutrition_goal_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_nutrition_goal_person_effective_date
    ON public.nutrition_goal USING btree (person_id, effective_date DESC);
