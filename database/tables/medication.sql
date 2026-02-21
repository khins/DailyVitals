CREATE TABLE public.medication (
    medication_id      bigserial PRIMARY KEY,
    person_id          bigint NOT NULL,

    medication_name    varchar(200) NOT NULL,
    dosage_mg          numeric(6,2) NOT NULL,
    dosage_form        varchar(50) NULL, -- tablet, capsule, injection, etc.

    take_morning       boolean NOT NULL DEFAULT false,
    take_noon          boolean NOT NULL DEFAULT false,
    take_evening       boolean NOT NULL DEFAULT false,
    take_bedtime       boolean NOT NULL DEFAULT false,

    instructions       text NULL,        -- long-form directions
    prescribed_by      varchar(200) NULL,
    start_date         date NULL,
    end_date           date NULL,

    is_active          boolean NOT NULL DEFAULT true,

    created_at         timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at         timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
);
