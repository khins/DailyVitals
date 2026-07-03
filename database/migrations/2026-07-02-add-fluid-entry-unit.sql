ALTER TABLE public.fluid_intake
    ADD COLUMN IF NOT EXISTS entered_amount numeric(10, 2) NULL,
    ADD COLUMN IF NOT EXISTS entered_unit varchar(10) NULL;

SELECT set_config('dailyvitals.allow_demo_write', 'on', true);

UPDATE public.fluid_intake
SET
    entered_amount = COALESCE(entered_amount, fluid_ml),
    entered_unit = COALESCE(entered_unit, 'mL')
WHERE entered_amount IS NULL
   OR entered_unit IS NULL;

SELECT set_config('dailyvitals.allow_demo_write', 'off', true);

ALTER TABLE public.fluid_intake
    ALTER COLUMN entered_amount SET NOT NULL,
    ALTER COLUMN entered_unit SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fluid_intake_entered_amount_check'
    ) THEN
        ALTER TABLE public.fluid_intake
            ADD CONSTRAINT fluid_intake_entered_amount_check
            CHECK (entered_amount > 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fluid_intake_entered_unit_check'
    ) THEN
        ALTER TABLE public.fluid_intake
            ADD CONSTRAINT fluid_intake_entered_unit_check
            CHECK (entered_unit IN ('mL', 'fl oz'));
    END IF;
END $$;

COMMENT ON COLUMN public.fluid_intake.fluid_ml IS
    'Normalized milliliters used for totals and reporting.';

COMMENT ON COLUMN public.fluid_intake.entered_amount IS
    'Original amount entered by the user before unit conversion.';

COMMENT ON COLUMN public.fluid_intake.entered_unit IS
    'Original unit selected by the user: mL or fl oz.';
