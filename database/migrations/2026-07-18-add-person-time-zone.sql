ALTER TABLE public.person
    ADD COLUMN IF NOT EXISTS time_zone_id varchar(100);

UPDATE public.person
SET time_zone_id = 'America/Chicago'
WHERE time_zone_id IS NULL OR length(btrim(time_zone_id)) = 0;

ALTER TABLE public.person
    ALTER COLUMN time_zone_id SET DEFAULT 'America/Chicago',
    ALTER COLUMN time_zone_id SET NOT NULL;

DO $migration$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'public.person'::regclass
          AND conname = 'person_time_zone_id_not_blank'
    ) THEN
        ALTER TABLE public.person
            ADD CONSTRAINT person_time_zone_id_not_blank
            CHECK (length(btrim(time_zone_id)) > 0);
    END IF;
END
$migration$;

COMMENT ON COLUMN public.person.time_zone_id IS
    'IANA time zone identifier used for user-local entry defaults and displays.';
