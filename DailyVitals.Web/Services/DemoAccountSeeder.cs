using DailyVitals.Data.Configuration;
using DailyVitals.Data.Services;
using Npgsql;

namespace DailyVitals.Web.Services;

public sealed class DemoAccountSeeder
{
    private const int SeedVersion = 2;
    private readonly IConfiguration _configuration;
    private readonly PersonService _personService;
    private readonly LoginUserService _loginUserService;

    public DemoAccountSeeder(
        IConfiguration configuration,
        PersonService personService,
        LoginUserService loginUserService)
    {
        _configuration = configuration;
        _personService = personService;
        _loginUserService = loginUserService;
    }

    public void EnsureSeeded()
    {
        if (!_configuration.GetValue("DemoMode:Enabled", false))
            return;

        var userName = _configuration["DemoMode:UserName"];
        var password = _configuration["DemoMode:Password"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Demo Mode requires a user name and password.");

        var demoPerson = _personService.GetAllPersons()
            .FirstOrDefault(person =>
                string.Equals(person.FirstName, "Demo", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(person.LastName, "Patient", StringComparison.OrdinalIgnoreCase));

        var personId = demoPerson?.PersonId
            ?? _personService.InsertPerson("Demo", "Patient", 5.75m, new DateTime(1980, 1, 15), "Prefer not to say");

        _loginUserService.EnsureDemoLoginUser(personId, userName, password);
        SeedPersonData(personId, userName);
    }

    private static void SeedPersonData(long personId, string enteredBy)
    {
        using var conn = DbConnectionFactory.Create();
        conn.Open();
        using var transaction = conn.BeginTransaction();

        using (var bypassCmd = new NpgsqlCommand(
            "SET LOCAL dailyvitals.allow_demo_write = 'on';",
            conn,
            transaction))
        {
            bypassCmd.ExecuteNonQuery();
        }

        EnsureSupportingTables(conn, transaction);

        using (var profileCmd = new NpgsqlCommand(@"
            UPDATE public.person
            SET height_ft = 5.75,
                birth_date = DATE '1980-01-15',
                gender = 'Prefer not to say',
                is_diabetic = TRUE,
                glucose_target_mg_dl = 130,
                track_kidney_labs = TRUE,
                track_weight_loss = TRUE,
                updated_at = CURRENT_TIMESTAMP
            WHERE person_id = @person_id;", conn, transaction))
        {
            profileCmd.Parameters.AddWithValue("person_id", personId);
            profileCmd.ExecuteNonQuery();
        }

        if (SeedIsCurrent(conn, transaction, personId))
        {
            EnsureReadOnlyProtection(conn, transaction);
            transaction.Commit();
            return;
        }

        DeleteExistingDemoData(conn, transaction, personId);
        SeedGoals(conn, transaction, personId);
        SeedVitalReadings(conn, transaction, personId);
        SeedExercise(conn, transaction, personId, enteredBy);
        SeedNutrition(conn, transaction, personId);
        SeedFluid(conn, transaction, personId);
        SeedKidneyLabs(conn, transaction, personId);
        SeedRenalFoods(conn, transaction, personId);
        SeedCoachReview(conn, transaction, personId);
        SaveSeedState(conn, transaction, personId);
        EnsureReadOnlyProtection(conn, transaction);

        transaction.Commit();
    }

    private static void EnsureSupportingTables(NpgsqlConnection conn, NpgsqlTransaction transaction)
    {
        const string sql = @"
            CREATE TABLE IF NOT EXISTS public.demo_seed_state (
                person_id int8 NOT NULL PRIMARY KEY,
                seed_version int4 NOT NULL,
                anchor_date date NOT NULL,
                seeded_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS public.fluid_intake (
                fluid_intake_id bigserial NOT NULL PRIMARY KEY,
                person_id int8 NOT NULL,
                consumed_at timestamp NOT NULL,
                fluid_ml int4 NOT NULL,
                beverage_name varchar(120) NOT NULL,
                notes text NULL,
                created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                updated_at timestamp NULL
            );

            CREATE TABLE IF NOT EXISTS public.nutrition_coach_review (
                nutrition_coach_review_id bigserial NOT NULL PRIMARY KEY,
                person_id int8 NOT NULL,
                period_start date NOT NULL,
                period_end date NOT NULL,
                days_logged int4 NOT NULL,
                model text NOT NULL,
                snapshot_json text NOT NULL,
                api_response_text text NOT NULL,
                review_json text NULL,
                http_status int4 NOT NULL,
                is_success boolean NOT NULL,
                error_message text NULL,
                created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
            );";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.ExecuteNonQuery();
    }

    private static bool SeedIsCurrent(NpgsqlConnection conn, NpgsqlTransaction transaction, long personId)
    {
        const string sql = @"
            SELECT EXISTS (
                SELECT 1
                FROM public.demo_seed_state
                WHERE person_id = @person_id
                  AND seed_version = @seed_version
                  AND anchor_date = CURRENT_DATE
            );";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("person_id", personId);
        cmd.Parameters.AddWithValue("seed_version", SeedVersion);
        return cmd.ExecuteScalar() is true;
    }

    private static void DeleteExistingDemoData(NpgsqlConnection conn, NpgsqlTransaction transaction, long personId)
    {
        var tables = new[]
        {
            "nutrition_coach_review",
            "kidney_lab_result",
            "fluid_intake",
            "food_phosphorus_intake",
            "food_phosphorus_food",
            "exercise_session",
            "weight",
            "blood_glucose",
            "blood_pressure",
            "nutrition_goal",
            "renal_diet_food"
        };

        foreach (var table in tables)
        {
            using var existsCmd = new NpgsqlCommand(
                "SELECT to_regclass(@table_name) IS NOT NULL;",
                conn,
                transaction);
            existsCmd.Parameters.AddWithValue("table_name", $"public.{table}");
            if (existsCmd.ExecuteScalar() is not true)
                continue;

            using var deleteCmd = new NpgsqlCommand(
                $"DELETE FROM public.{table} WHERE person_id = @person_id;",
                conn,
                transaction);
            deleteCmd.Parameters.AddWithValue("person_id", personId);
            deleteCmd.ExecuteNonQuery();
        }
    }

    private static void SeedGoals(NpgsqlConnection conn, NpgsqlTransaction transaction, long personId)
    {
        const string sql = @"
            INSERT INTO public.nutrition_goal (
                person_id, sodium_limit_mg, phosphorus_limit_mg, calorie_limit,
                effective_date, protein_target_g, potassium_limit_mg, fluid_limit_ml
            )
            VALUES (@person_id, 2000, 1000, 2000, CURRENT_DATE - 90, 70, 2000, 1200);";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("person_id", personId);
        cmd.ExecuteNonQuery();
    }

    private static void SeedVitalReadings(NpgsqlConnection conn, NpgsqlTransaction transaction, long personId)
    {
        const string sql = @"
            INSERT INTO public.blood_pressure (person_id, systolic, diastolic, pulse, reading_time, notes)
            SELECT
                @person_id,
                118 + ((day_number * 3) % 15),
                76 + ((day_number * 2) % 11),
                68 + (day_number % 9),
                CURRENT_DATE - day_number + TIME '09:15',
                CASE WHEN day_number % 7 = 0 THEN 'After dialysis treatment' ELSE 'Morning reading' END
            FROM generate_series(0, 29) AS day_number;

            INSERT INTO public.blood_glucose (person_id, glucose_value, reading_time, notes)
            SELECT
                @person_id,
                92 + ((day_number * 7) % 43),
                CURRENT_DATE - day_number + TIME '07:45',
                CASE WHEN day_number % 5 = 0 THEN 'Before breakfast' ELSE 'Morning reading' END
            FROM generate_series(0, 29) AS day_number;

            INSERT INTO public.weight (person_id, weight_value, weight_unit, reading_time, notes, height_ft)
            SELECT
                @person_id,
                188.5 - (day_number * 0.08) + ((day_number % 4) * 0.35),
                'lb',
                CURRENT_DATE - day_number + TIME '08:30',
                CASE WHEN day_number % 7 = 0 THEN 'Post-treatment weight' ELSE 'Morning weight' END,
                5.75
            FROM generate_series(0, 29) AS day_number;";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("person_id", personId);
        cmd.ExecuteNonQuery();
    }

    private static void SeedExercise(
        NpgsqlConnection conn,
        NpgsqlTransaction transaction,
        long personId,
        string enteredBy)
    {
        const string sql = @"
            INSERT INTO public.exercise_type (exercise_name, category)
            SELECT 'Outdoor Walk', 'Cardio'
            WHERE NOT EXISTS (
                SELECT 1 FROM public.exercise_type WHERE lower(exercise_name) = lower('Outdoor Walk')
            );

            INSERT INTO public.exercise_type (exercise_name, category)
            SELECT 'Stationary Bike', 'Cardio'
            WHERE NOT EXISTS (
                SELECT 1 FROM public.exercise_type WHERE lower(exercise_name) = lower('Stationary Bike')
            );

            INSERT INTO public.exercise_session (
                person_id, exercise_type_id, start_time, duration_minutes,
                calories_expended, intensity, notes
            )
            SELECT
                @person_id,
                CASE
                    WHEN session_number % 2 = 0 THEN
                        (SELECT exercise_type_id FROM public.exercise_type WHERE lower(exercise_name) = lower('Outdoor Walk') LIMIT 1)
                    ELSE
                        (SELECT exercise_type_id FROM public.exercise_type WHERE lower(exercise_name) = lower('Stationary Bike') LIMIT 1)
                END,
                CURRENT_DATE - (session_number * 2) + TIME '17:30',
                30 + ((session_number % 3) * 15),
                180 + ((session_number % 3) * 85),
                CASE WHEN session_number % 4 = 0 THEN 'Low' ELSE 'Moderate' END,
                CASE WHEN session_number % 2 = 0 THEN 'Comfortable pace' ELSE 'Steady session' END
            FROM generate_series(0, 14) AS session_number;";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("person_id", personId);
        cmd.Parameters.AddWithValue("entered_by", enteredBy);
        cmd.ExecuteNonQuery();
    }

    private static void SeedNutrition(NpgsqlConnection conn, NpgsqlTransaction transaction, long personId)
    {
        const string sql = @"
            INSERT INTO public.food_phosphorus_intake (
                person_id, food_name, phosphorus_mg, calories, sodium_mg,
                protein_g, potassium_mg, fluid_ml, binders, consumed_at,
                notes, serving_description, estimated_by_ai, ai_provider,
                ai_confidence, source_notes
            )
            SELECT
                @person_id,
                meal.food_name,
                meal.phosphorus_mg,
                meal.calories,
                meal.sodium_mg + CASE WHEN day_number % 6 = 0 THEN 180 ELSE 0 END,
                meal.protein_g,
                meal.potassium_mg,
                meal.fluid_ml,
                meal.binders,
                CURRENT_DATE - day_number + meal.meal_time,
                meal.notes,
                meal.serving,
                TRUE,
                'OpenAI',
                'medium',
                'Synthetic demo estimate'
            FROM generate_series(0, 29) AS day_number
            CROSS JOIN (VALUES
                ('Oatmeal with blueberries', 120, 310, 180, 9.0, 280, 180, 0, TIME '08:00', 'Breakfast', '1 bowl'),
                ('Turkey sandwich', 285, 520, 1050, 30.0, 510, 0, 1, TIME '12:30', 'Lunch', '1 sandwich'),
                ('Grilled chicken with rice', 330, 610, 640, 42.0, 690, 0, 2, TIME '18:15', 'Dinner', '1 plate')
            ) AS meal(food_name, phosphorus_mg, calories, sodium_mg, protein_g, potassium_mg, fluid_ml, binders, meal_time, notes, serving);

            INSERT INTO public.food_phosphorus_intake (
                person_id, food_name, phosphorus_mg, calories, sodium_mg,
                protein_g, potassium_mg, fluid_ml, binders, consumed_at,
                notes, serving_description, estimated_by_ai, ai_provider,
                ai_confidence, source_notes
            )
            SELECT
                @person_id,
                'Canned chicken noodle soup',
                145,
                240,
                860,
                14.0,
                420,
                240,
                1,
                CURRENT_DATE - day_number + TIME '12:45',
                'Higher sodium lunch item',
                '1 cup',
                TRUE,
                'OpenAI',
                'medium',
                'Synthetic demo estimate'
            FROM generate_series(0, 29) AS day_number
            WHERE day_number % 4 = 0;";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("person_id", personId);
        cmd.ExecuteNonQuery();
    }

    private static void SeedFluid(NpgsqlConnection conn, NpgsqlTransaction transaction, long personId)
    {
        const string sql = @"
            INSERT INTO public.fluid_intake (person_id, consumed_at, fluid_ml, beverage_name, notes)
            SELECT @person_id, CURRENT_DATE - day_number + TIME '08:15', 240, 'Coffee', 'Morning beverage'
            FROM generate_series(0, 29) AS day_number;

            INSERT INTO public.fluid_intake (person_id, consumed_at, fluid_ml, beverage_name, notes)
            SELECT @person_id, CURRENT_DATE - day_number + TIME '15:00', 355, 'Water', 'Afternoon water'
            FROM generate_series(0, 29) AS day_number;";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("person_id", personId);
        cmd.ExecuteNonQuery();
    }

    private static void SeedKidneyLabs(NpgsqlConnection conn, NpgsqlTransaction transaction, long personId)
    {
        const string sql = @"
            INSERT INTO public.kidney_lab_result (
                person_id, result_month, albumin, npcr, potassium, wktv,
                calcium, phosphorus, ipth, hemoglobin, glucose, cholesterol,
                triglycerides, bun, creatinine, notes, updated_at
            )
            SELECT
                @person_id,
                date_trunc('month', CURRENT_DATE - (month_number || ' months')::interval)::date,
                3.8 + (month_number * 0.05),
                1.0 + (month_number * 0.03),
                4.6 + ((month_number % 2) * 0.2),
                1.35 + (month_number * 0.04),
                8.8 + (month_number * 0.1),
                5.2 - (month_number * 0.1),
                315 + (month_number * 12),
                10.8 + (month_number * 0.2),
                108 + (month_number * 3),
                168 + (month_number * 4),
                132 + (month_number * 5),
                46 + (month_number * 2),
                7.8 + (month_number * 0.2),
                'Synthetic monthly renal panel',
                CURRENT_TIMESTAMP
            FROM generate_series(0, 3) AS month_number;";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("person_id", personId);
        cmd.ExecuteNonQuery();
    }

    private static void SeedRenalFoods(NpgsqlConnection conn, NpgsqlTransaction transaction, long personId)
    {
        const string sql = @"
            INSERT INTO public.renal_food_category (category_name, description)
            SELECT category_name, description
            FROM (VALUES
                ('Protein', 'Protein-focused foods'),
                ('Vegetable', 'Vegetable choices'),
                ('Fruit', 'Fruit choices'),
                ('Grain', 'Grain and starch choices')
            ) AS categories(category_name, description)
            WHERE NOT EXISTS (
                SELECT 1 FROM public.renal_food_category existing
                WHERE lower(existing.category_name) = lower(categories.category_name)
            );

            INSERT INTO public.renal_diet_food (
                person_id, food_name, serving_size, calories, sodium_mg,
                potassium_mg, phosphorus_mg, protein_g, allowed,
                restriction_notes, category_id
            )
            SELECT
                @person_id,
                food.food_name,
                food.serving_size,
                food.calories,
                food.sodium_mg,
                food.potassium_mg,
                food.phosphorus_mg,
                food.protein_g,
                food.allowed,
                food.notes,
                (SELECT category_id FROM public.renal_food_category WHERE lower(category_name) = lower(food.category_name) LIMIT 1)
            FROM (VALUES
                ('Egg whites', '3 large', 51, 165, 162, 15, 11.0, TRUE, 'High-quality protein with low phosphorus', 'Protein'),
                ('Grilled chicken breast', '3 oz', 140, 60, 220, 195, 26.0, TRUE, 'Choose unseasoned chicken', 'Protein'),
                ('Cabbage', '1/2 cup cooked', 17, 6, 147, 18, 1.0, TRUE, 'Lower-potassium vegetable choice', 'Vegetable'),
                ('Red bell pepper', '1/2 cup', 23, 2, 157, 14, 0.8, TRUE, 'Fresh, naturally low sodium', 'Vegetable'),
                ('Blueberries', '1/2 cup', 42, 1, 57, 9, 0.5, TRUE, 'Lower-potassium fruit choice', 'Fruit'),
                ('White rice', '1/2 cup cooked', 103, 1, 35, 34, 2.1, TRUE, 'Watch portion size', 'Grain'),
                ('Deli ham', '3 oz', 150, 1040, 350, 210, 18.0, FALSE, 'High sodium processed meat', 'Protein'),
                ('Instant potatoes', '1/2 cup', 170, 560, 610, 95, 3.0, FALSE, 'Higher sodium and potassium', 'Vegetable')
            ) AS food(food_name, serving_size, calories, sodium_mg, potassium_mg, phosphorus_mg, protein_g, allowed, notes, category_name);";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("person_id", personId);
        cmd.ExecuteNonQuery();
    }

    private static void SeedCoachReview(NpgsqlConnection conn, NpgsqlTransaction transaction, long personId)
    {
        const string reviewJson = """
            {
              "headline": "Sodium is the clearest opportunity this week",
              "summary": "You logged food on all 7 days. Phosphorus stayed within your goal on most logged days, while sodium was pushed above goal by the turkey sandwich and canned soup entries.",
              "wins": [
                "Nutrition was logged consistently for all 7 days.",
                "Protein met the recorded target on most days.",
                "Oatmeal and blueberries provided a lower-sodium breakfast pattern."
              ],
              "focusAreas": [
                "Processed lunch foods were the largest sodium sources.",
                "Canned soup raised both sodium and fluid intake on several days."
              ],
              "suggestedActions": [
                "Compare deli-meat labels and choose a lower-sodium option.",
                "Try a homemade soup or another lower-sodium lunch on one day this week."
              ],
              "careTeamNote": "Review personal sodium, potassium, phosphorus, protein, fluid, and binder guidance with the renal care team."
            }
            """;

        const string snapshotJson = """
            {
              "daysInPeriod": 7,
              "daysLogged": 7,
              "foodEntries": 23,
              "note": "Synthetic demonstration snapshot"
            }
            """;

        const string sql = @"
            INSERT INTO public.nutrition_coach_review (
                person_id, period_start, period_end, days_logged, model,
                snapshot_json, api_response_text, review_json, http_status,
                is_success, error_message, created_at
            )
            VALUES (
                @person_id,
                CURRENT_DATE - 6,
                CURRENT_DATE,
                7,
                'demo-seeded',
                @snapshot_json,
                @review_json,
                @review_json,
                200,
                TRUE,
                NULL,
                CURRENT_TIMESTAMP
            );";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("person_id", personId);
        cmd.Parameters.AddWithValue("snapshot_json", snapshotJson);
        cmd.Parameters.AddWithValue("review_json", reviewJson);
        cmd.ExecuteNonQuery();
    }

    private static void SaveSeedState(NpgsqlConnection conn, NpgsqlTransaction transaction, long personId)
    {
        const string sql = @"
            INSERT INTO public.demo_seed_state (person_id, seed_version, anchor_date, seeded_at)
            VALUES (@person_id, @seed_version, CURRENT_DATE, CURRENT_TIMESTAMP)
            ON CONFLICT (person_id) DO UPDATE
            SET seed_version = EXCLUDED.seed_version,
                anchor_date = EXCLUDED.anchor_date,
                seeded_at = EXCLUDED.seeded_at;";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.Parameters.AddWithValue("person_id", personId);
        cmd.Parameters.AddWithValue("seed_version", SeedVersion);
        cmd.ExecuteNonQuery();
    }

    private static void EnsureReadOnlyProtection(NpgsqlConnection conn, NpgsqlTransaction transaction)
    {
        const string sql = @"
            CREATE OR REPLACE FUNCTION public.prevent_demo_person_write()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            DECLARE
                target_person_id bigint;
            BEGIN
                IF current_setting('dailyvitals.allow_demo_write', TRUE) = 'on' THEN
                    IF TG_OP = 'DELETE' THEN
                        RETURN OLD;
                    END IF;
                    RETURN NEW;
                END IF;

                IF TG_OP = 'DELETE' THEN
                    target_person_id := OLD.person_id;
                ELSE
                    target_person_id := NEW.person_id;
                END IF;

                IF target_person_id IS NOT NULL AND EXISTS (
                    SELECT 1
                    FROM public.login_user
                    WHERE person_id = target_person_id
                      AND is_demo = TRUE
                ) THEN
                    RAISE EXCEPTION 'Demo Mode is read-only.' USING ERRCODE = '42501';
                END IF;

                IF TG_OP = 'DELETE' THEN
                    RETURN OLD;
                END IF;
                RETURN NEW;
            END;
            $$;

            DO $$
            DECLARE
                protected_table record;
            BEGIN
                FOR protected_table IN
                    SELECT columns.table_name
                    FROM information_schema.columns
                    WHERE columns.table_schema = 'public'
                      AND columns.column_name = 'person_id'
                      AND columns.table_name NOT IN ('login_user', 'demo_seed_state')
                    GROUP BY columns.table_name
                LOOP
                    EXECUTE format(
                        'DROP TRIGGER IF EXISTS trg_prevent_demo_write ON public.%I',
                        protected_table.table_name);
                    EXECUTE format(
                        'CREATE TRIGGER trg_prevent_demo_write BEFORE INSERT OR UPDATE OR DELETE ON public.%I FOR EACH ROW EXECUTE FUNCTION public.prevent_demo_person_write()',
                        protected_table.table_name);
                END LOOP;
            END $$;";

        using var cmd = new NpgsqlCommand(sql, conn, transaction);
        cmd.ExecuteNonQuery();
    }
}
