ALTER TABLE public.nutrition_goal
    ADD COLUMN IF NOT EXISTS phosphorus_enabled boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS sodium_enabled boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS calorie_enabled boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS protein_enabled boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS potassium_enabled boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS fluid_enabled boolean NOT NULL DEFAULT true;

