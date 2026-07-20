ALTER TABLE public.nutrition_goal
    ADD COLUMN IF NOT EXISTS sugar_limit_g int4 NULL;

ALTER TABLE public.nutrition_goal
    DROP CONSTRAINT IF EXISTS nutrition_goal_sugar_limit_check;

ALTER TABLE public.nutrition_goal
    ADD CONSTRAINT nutrition_goal_sugar_limit_check
    CHECK (sugar_limit_g IS NULL OR sugar_limit_g > 0);

