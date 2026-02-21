-- DROP TABLE public.exercise_type;

CREATE TABLE public.exercise_type (
	exercise_type_id bigserial NOT NULL,
	exercise_name varchar(100) NOT NULL,
	category varchar(50) NOT NULL,
	is_active bool NOT NULL DEFAULT true,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT exercise_type_pkey PRIMARY KEY (exercise_type_id),
	CONSTRAINT uq_exercise_name UNIQUE (exercise_name)
);
