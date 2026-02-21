-- DROP TABLE public.data_entry_log;

CREATE TABLE public.data_entry_log (
	log_id bigserial NOT NULL,
	table_name varchar(100) NOT NULL,
	record_id int8 NOT NULL,
	action_type varchar(20) NOT NULL,
	entered_by varchar(100) NOT NULL,
	entry_timestamp timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
	change_details jsonb NULL,
	CONSTRAINT data_entry_log_action_type_check CHECK (((action_type)::text = ANY ((ARRAY['INSERT'::character varying, 'UPDATE'::character varying, 'DELETE'::character varying])::text[]))),
	CONSTRAINT data_entry_log_pkey PRIMARY KEY (log_id)
);
CREATE INDEX idx_log_table_record ON public.data_entry_log USING btree (table_name, record_id);
