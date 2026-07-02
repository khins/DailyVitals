ALTER TABLE public.food_phosphorus_intake
    ADD COLUMN IF NOT EXISTS meal_type varchar(20) NULL;

SELECT set_config('dailyvitals.allow_demo_write', 'on', true);

UPDATE public.food_phosphorus_intake
SET meal_type = CASE lower(trim(notes))
    WHEN 'breakfast' THEN 'Breakfast'
    WHEN 'lunch' THEN 'Lunch'
    WHEN 'dinner' THEN 'Dinner'
    WHEN 'snack' THEN 'Snack'
    WHEN 'snacks' THEN 'Snack'
    WHEN 'higher sodium lunch item' THEN 'Lunch'
    ELSE meal_type
END
WHERE meal_type IS NULL
  AND notes IS NOT NULL;

SELECT set_config('dailyvitals.allow_demo_write', 'off', true);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'food_phosphorus_intake_meal_type_check'
    ) THEN
        ALTER TABLE public.food_phosphorus_intake
            ADD CONSTRAINT food_phosphorus_intake_meal_type_check
            CHECK (meal_type IS NULL OR meal_type IN ('Breakfast', 'Lunch', 'Dinner', 'Snack'));
    END IF;
END $$;

COMMENT ON COLUMN public.food_phosphorus_intake.meal_type IS
    'User-selected meal category; never inferred from the entry timestamp.';
