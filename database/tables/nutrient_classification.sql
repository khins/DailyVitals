-- DROP TABLE public.nutrient_classification;

CREATE TABLE public.nutrient_classification (
	classification_id bigserial NOT NULL,
	nutrient_name varchar(50) NOT NULL,
	classification_label varchar(20) NOT NULL,
	min_value numeric NULL,
	max_value numeric NULL,
	unit varchar(20) NOT NULL,
	created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	CONSTRAINT nutrient_classification_pkey PRIMARY KEY (classification_id),
	CONSTRAINT uq_nutrient_class UNIQUE (nutrient_name, classification_label)
);
CREATE INDEX idx_nutrient_class_lookup ON public.nutrient_classification USING btree (nutrient_name, classification_label);
