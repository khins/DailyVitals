CREATE OR REPLACE FUNCTION public.evaluate_severity_escalation()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
DECLARE
    rule RECORD;
    violation_total INTEGER;
BEGIN
    FOR rule IN
        SELECT *
        FROM severity_escalation_rule
        WHERE vital_type = NEW.vital_type
          AND base_severity = NEW.severity
          AND is_active = TRUE
    LOOP
        SELECT COUNT(*)
        INTO violation_total
        FROM vital_alert
        WHERE person_id = NEW.person_id
          AND vital_type = NEW.vital_type
          AND severity = rule.base_severity
          AND alert_time >= (NEW.alert_time - INTERVAL '1 minute' * rule.time_window_minutes);

        IF violation_total >= rule.violation_count THEN
            INSERT INTO vital_alert (
                person_id,
                vital_type,
                reading_id,
                reading_value,
                threshold_id,
                alert_message,
                severity,
                parent_alert_id,
                escalation_level
            )
            VALUES (
                NEW.person_id,
                NEW.vital_type,
                NEW.reading_id,
                NEW.reading_value,
                NEW.threshold_id,
                'Severity escalated due to repeated violations',
                rule.escalate_to,
                NEW.alert_id,
                NEW.escalation_level + 1
            );
        END IF;
    END LOOP;

    RETURN NEW;
END;
$function$
;
