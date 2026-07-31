-- The shared renal catalog is maintained by an administrative/migration role.
-- The web runtime only needs read access; it must not insert, update, or delete
-- catalog rows.
DO $migration$
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM pg_roles
        WHERE rolname = 'dailyvitals_app'
    ) THEN
        GRANT SELECT ON TABLE public.renal_friendly_food TO dailyvitals_app;
    END IF;
END
$migration$;
