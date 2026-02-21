-- DROP TABLE public.vital_threshold;

CREATE TABLE public.vital_threshold (
	threshold_id bigserial NOT NULL,
	vital_type varchar(50) NOT NULL,
	person_id int8 NULL,
	min_value numeric NULL,
	max_value numeric NULL,
	severity varchar(20) NOT NULL DEFAULT 'medium'::character varying,
	is_active bool NOT NULL DEFAULT true,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT vital_threshold_pkey PRIMARY KEY (threshold_id),
	CONSTRAINT fk_threshold_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);