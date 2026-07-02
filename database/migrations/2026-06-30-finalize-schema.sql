-- Follow-up kept separate because the consolidation migration has already been
-- applied to development databases and applied migrations are immutable.

ALTER TABLE public.kidney_lab_result
    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL;

UPDATE public.kidney_lab_result
SET updated_at = created_at
WHERE updated_at IS NULL;

UPDATE public.person p
SET height_ft = latest.height_ft
FROM (
    SELECT DISTINCT ON (person_id)
        person_id,
        height_ft
    FROM public.weight
    WHERE height_ft IS NOT NULL
    ORDER BY person_id, reading_time DESC, weight_id DESC
) latest
WHERE p.person_id = latest.person_id
  AND p.height_ft IS NULL;
