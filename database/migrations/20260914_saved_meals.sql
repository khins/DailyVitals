CREATE TABLE IF NOT EXISTS public.saved_meal (
    saved_meal_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    person_id bigint NOT NULL REFERENCES public.person(person_id),
    name varchar(100) NOT NULL CHECK (length(trim(name)) > 0),
    meal_type text NOT NULL CHECK (meal_type IN ('Breakfast', 'Lunch', 'Dinner', 'Snack')),
    foods_json jsonb NOT NULL CHECK (jsonb_typeof(foods_json) = 'array' AND jsonb_array_length(foods_json) > 0),
    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE UNIQUE INDEX IF NOT EXISTS saved_meal_person_name ON public.saved_meal(person_id, lower(trim(name)));
