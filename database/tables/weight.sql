-- DROP TABLE public.weight;

CREATE TABLE public.weight (
	weight_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	weight_value numeric(6, 2) NOT NULL,
	weight_unit varchar(10) NOT NULL DEFAULT 'lb'::character varying,
	reading_time timestamp NOT NULL,
	notes text NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	height_cm numeric(6, 2) NULL,
	height_ft numeric(4, 2) NULL,
	CONSTRAINT weight_height_ft_check CHECK (((height_ft IS NULL) OR ((height_ft >= 3.0) AND (height_ft <= 8.5)))),
	CONSTRAINT weight_pkey PRIMARY KEY (weight_id),
	CONSTRAINT weight_weight_value_check CHECK ((weight_value > (0)::numeric)),
	CONSTRAINT fk_weight_person FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);
CREATE INDEX idx_weight_person_time ON public.weight USING btree (person_id, reading_time);
