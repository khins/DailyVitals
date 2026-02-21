-- Drop table

-- DROP TABLE public.vital_alert;

CREATE TABLE public.vital_alert (
	alert_id bigserial NOT NULL,
	person_id int8 NOT NULL,
	vital_type varchar(50) NOT NULL,
	reading_id int8 NOT NULL,
	reading_value numeric NOT NULL,
	threshold_id int8 NOT NULL,
	alert_message text NULL,
	severity varchar(20) NOT NULL,
	alert_time timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	acknowledged bool NOT NULL DEFAULT false,
	acknowledged_at timestamp NULL,
	parent_alert_id int8 NULL,
	escalation_level int4 NOT NULL DEFAULT 0,
	CONSTRAINT vital_alert_pkey PRIMARY KEY (alert_id),
	CONSTRAINT fk_alert_person FOREIGN KEY (person_id) REFERENCES public.person(person_id),
	CONSTRAINT fk_alert_threshold FOREIGN KEY (threshold_id) REFERENCES public.vital_threshold(threshold_id),
	CONSTRAINT fk_parent_alert FOREIGN KEY (parent_alert_id) REFERENCES public.vital_alert(alert_id)
);
