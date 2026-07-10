ALTER TABLE public.login_user
    ADD COLUMN IF NOT EXISTS failed_login_count integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS last_failed_login_at timestamp NULL,
    ADD COLUMN IF NOT EXISTS locked_until timestamp NULL;
