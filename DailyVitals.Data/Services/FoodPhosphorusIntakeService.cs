using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;

namespace DailyVitals.Data.Services
{
    public class FoodPhosphorusIntakeService
    {
        public long Insert(
            long personId,
            string foodName,
            int phosphorusMg,
            int? calories,
            int? sodiumMg,
            decimal? proteinG,
            int? potassiumMg,
            int? fluidMl,
            int binders,
            DateTime consumedAt,
            string? notes,
            string? servingDescription,
            bool estimatedByAi,
            string? aiProvider,
            string? aiConfidence,
            string? sourceNotes,
            string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureFoodPhosphorusIntakeColumns(conn);
            var foodProfileId = UpsertFoodProfile(
                conn,
                personId,
                foodName,
                phosphorusMg,
                calories,
                sodiumMg,
                proteinG,
                potassiumMg,
                binders,
                servingDescription);

            using var cmd = new NpgsqlCommand(
                @"
                WITH inserted AS (
                    INSERT INTO food_phosphorus_intake (
                        food_phosphorus_food_id,
                        person_id,
                        food_name,
                        phosphorus_mg,
                        calories,
                        sodium_mg,
                        protein_g,
                        potassium_mg,
                        fluid_ml,
                        binders,
                        consumed_at,
                        notes,
                        serving_description,
                        estimated_by_ai,
                        ai_provider,
                        ai_confidence,
                        source_notes
                    )
                    VALUES (
                        @p_food_phosphorus_food_id,
                        @p_person_id,
                        TRIM(@p_food_name),
                        @p_phosphorus_mg,
                        @p_calories,
                        @p_sodium_mg,
                        @p_protein_g,
                        @p_potassium_mg,
                        @p_fluid_ml,
                        @p_binders,
                        COALESCE(@p_consumed_at, CURRENT_TIMESTAMP),
                        @p_notes,
                        @p_serving_description,
                        COALESCE(@p_estimated_by_ai, false),
                        @p_ai_provider,
                        @p_ai_confidence,
                        @p_source_notes
                    )
                    RETURNING food_phosphorus_intake_id
                ),
                logged AS (
                    INSERT INTO data_entry_log (
                        table_name,
                        record_id,
                        action_type,
                        entered_by,
                        change_details
                    )
                    SELECT
                        'food_phosphorus_intake',
                        food_phosphorus_intake_id,
                        'INSERT',
                        @p_entered_by,
                        jsonb_build_object(
                            'food_name', @p_food_name,
                            'phosphorus_mg', @p_phosphorus_mg,
                            'calories', @p_calories,
                            'sodium_mg', @p_sodium_mg,
                            'protein_g', @p_protein_g,
                            'potassium_mg', @p_potassium_mg,
                            'fluid_ml', @p_fluid_ml,
                            'binders', @p_binders,
                            'consumed_at', @p_consumed_at,
                            'notes', @p_notes,
                            'serving_description', @p_serving_description,
                            'estimated_by_ai', @p_estimated_by_ai,
                            'ai_provider', @p_ai_provider,
                            'ai_confidence', @p_ai_confidence,
                            'source_notes', @p_source_notes
                        )
                    FROM inserted
                    RETURNING 1
                )
                SELECT food_phosphorus_intake_id
                FROM inserted;",
                conn);

            cmd.Parameters.AddWithValue("p_person_id", personId);
            cmd.Parameters.AddWithValue("p_food_phosphorus_food_id", foodProfileId);
            cmd.Parameters.AddWithValue("p_food_name", foodName);
            cmd.Parameters.AddWithValue("p_phosphorus_mg", phosphorusMg);
            cmd.Parameters.Add("p_calories", NpgsqlDbType.Integer).Value =
                (object?)calories ?? DBNull.Value;
            cmd.Parameters.Add("p_sodium_mg", NpgsqlDbType.Integer).Value =
                (object?)sodiumMg ?? DBNull.Value;
            cmd.Parameters.Add("p_protein_g", NpgsqlDbType.Numeric).Value =
                (object?)proteinG ?? DBNull.Value;
            cmd.Parameters.Add("p_potassium_mg", NpgsqlDbType.Integer).Value =
                (object?)potassiumMg ?? DBNull.Value;
            cmd.Parameters.Add("p_fluid_ml", NpgsqlDbType.Integer).Value =
                (object?)fluidMl ?? DBNull.Value;
            cmd.Parameters.AddWithValue("p_binders", binders);
            cmd.Parameters.AddWithValue("p_consumed_at", consumedAt);
            cmd.Parameters.AddWithValue("p_notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_serving_description", (object?)servingDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_estimated_by_ai", estimatedByAi);
            cmd.Parameters.AddWithValue("p_ai_provider", (object?)aiProvider ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_ai_confidence", (object?)aiConfidence ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_source_notes", (object?)sourceNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

            var result = cmd.ExecuteScalar();

            if (result is null or DBNull)
                throw new Exception("Food phosphorus intake insert failed. No ID returned.");

            return Convert.ToInt64(result);
        }

        public void Update(
            long foodPhosphorusIntakeId,
            long personId,
            string foodName,
            int phosphorusMg,
            int? calories,
            int? sodiumMg,
            decimal? proteinG,
            int? potassiumMg,
            int? fluidMl,
            int binders,
            DateTime consumedAt,
            string? notes,
            string? servingDescription,
            bool estimatedByAi,
            string? aiProvider,
            string? aiConfidence,
            string? sourceNotes,
            string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureFoodPhosphorusIntakeColumns(conn);
            var foodProfileId = UpsertFoodProfile(
                conn,
                personId,
                foodName,
                phosphorusMg,
                calories,
                sodiumMg,
                proteinG,
                potassiumMg,
                binders,
                servingDescription);

            using var cmd = new NpgsqlCommand(
                @"
                WITH updated AS (
                    UPDATE food_phosphorus_intake
                    SET
                        food_phosphorus_food_id = @p_food_phosphorus_food_id,
                        food_name = TRIM(@p_food_name),
                        phosphorus_mg = @p_phosphorus_mg,
                        calories = @p_calories,
                        sodium_mg = @p_sodium_mg,
                        protein_g = @p_protein_g,
                        potassium_mg = @p_potassium_mg,
                        fluid_ml = @p_fluid_ml,
                        binders = @p_binders,
                        consumed_at = COALESCE(@p_consumed_at, CURRENT_TIMESTAMP),
                        notes = @p_notes,
                        serving_description = @p_serving_description,
                        estimated_by_ai = COALESCE(@p_estimated_by_ai, false),
                        ai_provider = @p_ai_provider,
                        ai_confidence = @p_ai_confidence,
                        source_notes = @p_source_notes,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE food_phosphorus_intake_id = @p_food_phosphorus_intake_id
                      AND person_id = @p_person_id
                    RETURNING food_phosphorus_intake_id
                ),
                logged AS (
                    INSERT INTO data_entry_log (
                        table_name,
                        record_id,
                        action_type,
                        entered_by,
                        change_details
                    )
                    SELECT
                        'food_phosphorus_intake',
                        food_phosphorus_intake_id,
                        'UPDATE',
                        @p_entered_by,
                        jsonb_build_object(
                            'food_name', @p_food_name,
                            'phosphorus_mg', @p_phosphorus_mg,
                            'calories', @p_calories,
                            'sodium_mg', @p_sodium_mg,
                            'protein_g', @p_protein_g,
                            'potassium_mg', @p_potassium_mg,
                            'fluid_ml', @p_fluid_ml,
                            'binders', @p_binders,
                            'consumed_at', @p_consumed_at,
                            'notes', @p_notes,
                            'serving_description', @p_serving_description,
                            'estimated_by_ai', @p_estimated_by_ai,
                            'ai_provider', @p_ai_provider,
                            'ai_confidence', @p_ai_confidence,
                            'source_notes', @p_source_notes
                        )
                    FROM updated
                    RETURNING 1
                )
                SELECT food_phosphorus_intake_id
                FROM updated;",
                conn);

            cmd.Parameters.AddWithValue("p_food_phosphorus_intake_id", foodPhosphorusIntakeId);
            cmd.Parameters.AddWithValue("p_person_id", personId);
            cmd.Parameters.AddWithValue("p_food_phosphorus_food_id", foodProfileId);
            cmd.Parameters.AddWithValue("p_food_name", foodName);
            cmd.Parameters.AddWithValue("p_phosphorus_mg", phosphorusMg);
            cmd.Parameters.Add("p_calories", NpgsqlDbType.Integer).Value =
                (object?)calories ?? DBNull.Value;
            cmd.Parameters.Add("p_sodium_mg", NpgsqlDbType.Integer).Value =
                (object?)sodiumMg ?? DBNull.Value;
            cmd.Parameters.Add("p_protein_g", NpgsqlDbType.Numeric).Value =
                (object?)proteinG ?? DBNull.Value;
            cmd.Parameters.Add("p_potassium_mg", NpgsqlDbType.Integer).Value =
                (object?)potassiumMg ?? DBNull.Value;
            cmd.Parameters.Add("p_fluid_ml", NpgsqlDbType.Integer).Value =
                (object?)fluidMl ?? DBNull.Value;
            cmd.Parameters.AddWithValue("p_binders", binders);
            cmd.Parameters.AddWithValue("p_consumed_at", consumedAt);
            cmd.Parameters.AddWithValue("p_notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_serving_description", (object?)servingDescription ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_estimated_by_ai", estimatedByAi);
            cmd.Parameters.AddWithValue("p_ai_provider", (object?)aiProvider ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_ai_confidence", (object?)aiConfidence ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_source_notes", (object?)sourceNotes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

            var result = cmd.ExecuteScalar();

            if (result is null or DBNull)
                throw new Exception("Food phosphorus intake update failed. No matching record was updated.");
        }

        public List<FoodPhosphorusIntake> GetHistory(long personId)
        {
            var list = new List<FoodPhosphorusIntake>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureFoodPhosphorusIntakeColumns(conn);

            const string sql = @"
                SELECT
                    i.food_phosphorus_intake_id,
                    i.person_id,
                    i.food_name,
                    i.phosphorus_mg,
                    i.calories,
                    i.sodium_mg,
                    i.protein_g,
                    i.potassium_mg,
                    i.fluid_ml,
                    COALESCE(i.binders, 0) AS binders,
                    i.consumed_at::timestamp,
                    i.notes,
                    i.serving_description,
                    i.estimated_by_ai,
                    i.ai_provider,
                    i.ai_confidence,
                    i.source_notes,
                    i.created_at,
                    i.updated_at,
                    i.food_phosphorus_food_id,
                    n.note_text
                FROM food_phosphorus_intake i
                LEFT JOIN food_phosphorus_food f
                    ON f.food_phosphorus_food_id = i.food_phosphorus_food_id
                LEFT JOIN food_phosphorus_food_note n
                    ON n.food_phosphorus_food_id = f.food_phosphorus_food_id
                WHERE i.person_id = @person_id
                ORDER BY i.consumed_at DESC, i.food_phosphorus_intake_id DESC;
            ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FoodPhosphorusIntake
                {
                    FoodPhosphorusIntakeId = reader.GetInt64(0),
                    PersonId = reader.GetInt64(1),
                    FoodName = reader.GetString(2),
                    PhosphorusMg = reader.GetInt32(3),
                    Calories = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    SodiumMg = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    ProteinG = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                    PotassiumMg = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    FluidMl = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    Binders = reader.GetInt32(9),
                    ConsumedAt = reader.GetDateTime(10),
                    Notes = reader.IsDBNull(11) ? null : reader.GetString(11),
                    ServingDescription = reader.IsDBNull(12) ? null : reader.GetString(12),
                    EstimatedByAi = reader.GetBoolean(13),
                    AiProvider = reader.IsDBNull(14) ? null : reader.GetString(14),
                    AiConfidence = reader.IsDBNull(15) ? null : reader.GetString(15),
                    SourceNotes = reader.IsDBNull(16) ? null : reader.GetString(16),
                    CreatedAt = reader.GetDateTime(17),
                    UpdatedAt = reader.IsDBNull(18) ? null : reader.GetDateTime(18),
                    FoodPhosphorusFoodId = reader.IsDBNull(19) ? null : reader.GetInt64(19),
                    FoodNotes = reader.IsDBNull(20) ? null : reader.GetString(20)
                });
            }

            return list;
        }

        private static void EnsureFoodPhosphorusIntakeColumns(NpgsqlConnection conn)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS public.food_phosphorus_food (
                    food_phosphorus_food_id bigserial NOT NULL,
                    person_id int8 NOT NULL,
                    food_name varchar(200) NOT NULL,
                    default_phosphorus_mg int4 NULL,
                    default_calories int4 NULL,
                    default_sodium_mg int4 NULL,
                    default_protein_g numeric(8, 2) NULL,
                    default_potassium_mg int4 NULL,
                    default_binders int4 NULL,
                    default_serving_description varchar(200) NULL,
                    food_notes text NULL,
                    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT food_phosphorus_food_pkey PRIMARY KEY (food_phosphorus_food_id)
                );

                CREATE TABLE IF NOT EXISTS public.food_phosphorus_food_note (
                    food_phosphorus_food_note_id bigserial NOT NULL,
                    food_phosphorus_food_id int8 NOT NULL,
                    note_text text NULL,
                    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT food_phosphorus_food_note_pkey PRIMARY KEY (food_phosphorus_food_note_id)
                );

                ALTER TABLE public.food_phosphorus_intake
                    ADD COLUMN IF NOT EXISTS food_phosphorus_food_id int8 NULL,
                    ADD COLUMN IF NOT EXISTS binders integer NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS calories integer NULL,
                    ADD COLUMN IF NOT EXISTS sodium_mg integer NULL,
                    ADD COLUMN IF NOT EXISTS protein_g numeric(8, 2) NULL,
                    ADD COLUMN IF NOT EXISTS potassium_mg integer NULL,
                    ADD COLUMN IF NOT EXISTS fluid_ml integer NULL,
                    ADD COLUMN IF NOT EXISTS serving_description varchar(200) NULL,
                    ADD COLUMN IF NOT EXISTS estimated_by_ai boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS ai_provider varchar(50) NULL,
                    ADD COLUMN IF NOT EXISTS ai_confidence varchar(20) NULL,
                    ADD COLUMN IF NOT EXISTS source_notes text NULL,
                    ADD COLUMN IF NOT EXISTS created_at timestamp NULL,
                    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL;

                UPDATE public.food_phosphorus_intake
                SET created_at = COALESCE(created_at, consumed_at, CURRENT_TIMESTAMP)
                WHERE created_at IS NULL;

                UPDATE public.food_phosphorus_intake
                SET updated_at = COALESCE(updated_at, created_at, consumed_at, CURRENT_TIMESTAMP)
                WHERE updated_at IS NULL;

                ALTER TABLE public.food_phosphorus_intake
                    ALTER COLUMN created_at SET DEFAULT CURRENT_TIMESTAMP,
                    ALTER COLUMN created_at SET NOT NULL,
                    ALTER COLUMN updated_at SET DEFAULT CURRENT_TIMESTAMP,
                    ALTER COLUMN updated_at SET NOT NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'food_phosphorus_food_person_food_name_key'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_food
                            ADD CONSTRAINT food_phosphorus_food_person_food_name_key UNIQUE (person_id, food_name);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'food_phosphorus_intake_food_fk'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_intake
                            ADD CONSTRAINT food_phosphorus_intake_food_fk
                            FOREIGN KEY (food_phosphorus_food_id)
                            REFERENCES public.food_phosphorus_food(food_phosphorus_food_id);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'food_phosphorus_food_note_food_key'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_food_note
                            ADD CONSTRAINT food_phosphorus_food_note_food_key UNIQUE (food_phosphorus_food_id);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'food_phosphorus_food_note_food_fk'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_food_note
                            ADD CONSTRAINT food_phosphorus_food_note_food_fk
                            FOREIGN KEY (food_phosphorus_food_id)
                            REFERENCES public.food_phosphorus_food(food_phosphorus_food_id);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'food_phosphorus_intake_calories_check'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_intake
                            ADD CONSTRAINT food_phosphorus_intake_calories_check CHECK (calories IS NULL OR calories >= 0);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'food_phosphorus_intake_sodium_check'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_intake
                            ADD CONSTRAINT food_phosphorus_intake_sodium_check CHECK (sodium_mg IS NULL OR sodium_mg >= 0);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'food_phosphorus_intake_protein_check'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_intake
                            ADD CONSTRAINT food_phosphorus_intake_protein_check CHECK (protein_g IS NULL OR protein_g >= 0);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'food_phosphorus_intake_potassium_check'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_intake
                            ADD CONSTRAINT food_phosphorus_intake_potassium_check CHECK (potassium_mg IS NULL OR potassium_mg >= 0);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'food_phosphorus_intake_fluid_check'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_intake
                            ADD CONSTRAINT food_phosphorus_intake_fluid_check CHECK (fluid_ml IS NULL OR fluid_ml >= 0);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'food_phosphorus_intake_binders_check'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_intake
                            ADD CONSTRAINT food_phosphorus_intake_binders_check CHECK (binders >= 0);
                    END IF;
                END $$;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private static long UpsertFoodProfile(
            NpgsqlConnection conn,
            long personId,
            string foodName,
            int phosphorusMg,
            int? calories,
            int? sodiumMg,
            decimal? proteinG,
            int? potassiumMg,
            int binders,
            string? servingDescription)
        {
            const string sql = @"
                INSERT INTO public.food_phosphorus_food (
                    person_id,
                    food_name,
                    default_phosphorus_mg,
                    default_calories,
                    default_sodium_mg,
                    default_protein_g,
                    default_potassium_mg,
                    default_binders,
                    default_serving_description
                )
                VALUES (
                    @person_id,
                    TRIM(@food_name),
                    @phosphorus_mg,
                    @calories,
                    @sodium_mg,
                    @protein_g,
                    @potassium_mg,
                    @binders,
                    @serving_description
                )
                ON CONFLICT (person_id, food_name)
                DO UPDATE SET
                    default_phosphorus_mg = EXCLUDED.default_phosphorus_mg,
                    default_calories = EXCLUDED.default_calories,
                    default_sodium_mg = EXCLUDED.default_sodium_mg,
                    default_protein_g = EXCLUDED.default_protein_g,
                    default_potassium_mg = EXCLUDED.default_potassium_mg,
                    default_binders = EXCLUDED.default_binders,
                    default_serving_description = EXCLUDED.default_serving_description,
                    updated_at = CURRENT_TIMESTAMP
                RETURNING food_phosphorus_food_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("food_name", foodName);
            cmd.Parameters.AddWithValue("phosphorus_mg", phosphorusMg);
            cmd.Parameters.Add("calories", NpgsqlDbType.Integer).Value =
                (object?)calories ?? DBNull.Value;
            cmd.Parameters.Add("sodium_mg", NpgsqlDbType.Integer).Value =
                (object?)sodiumMg ?? DBNull.Value;
            cmd.Parameters.Add("protein_g", NpgsqlDbType.Numeric).Value =
                (object?)proteinG ?? DBNull.Value;
            cmd.Parameters.Add("potassium_mg", NpgsqlDbType.Integer).Value =
                (object?)potassiumMg ?? DBNull.Value;
            cmd.Parameters.AddWithValue("binders", binders);
            cmd.Parameters.AddWithValue("serving_description", (object?)servingDescription ?? DBNull.Value);

            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        public string GetFoodNote(long foodPhosphorusFoodId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureFoodPhosphorusIntakeColumns(conn);

            const string sql = @"
                SELECT note_text
                FROM public.food_phosphorus_food_note
                WHERE food_phosphorus_food_id = @food_phosphorus_food_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("food_phosphorus_food_id", foodPhosphorusFoodId);

            return cmd.ExecuteScalar() as string ?? string.Empty;
        }

        public long EnsureFoodProfileForIntake(FoodPhosphorusIntake intake)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureFoodPhosphorusIntakeColumns(conn);

            var foodProfileId = UpsertFoodProfile(
                conn,
                intake.PersonId,
                intake.FoodName,
                intake.PhosphorusMg,
                intake.Calories,
                intake.SodiumMg,
                intake.ProteinG,
                intake.PotassiumMg,
                intake.Binders,
                intake.ServingDescription);

            const string sql = @"
                UPDATE public.food_phosphorus_intake
                SET food_phosphorus_food_id = @food_phosphorus_food_id,
                    updated_at = CURRENT_TIMESTAMP
                WHERE food_phosphorus_intake_id = @food_phosphorus_intake_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("food_phosphorus_food_id", foodProfileId);
            cmd.Parameters.AddWithValue("food_phosphorus_intake_id", intake.FoodPhosphorusIntakeId);
            cmd.ExecuteNonQuery();

            return foodProfileId;
        }

        public void SaveFoodNote(long foodPhosphorusFoodId, string? noteText)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureFoodPhosphorusIntakeColumns(conn);

            const string sql = @"
                INSERT INTO public.food_phosphorus_food_note (
                    food_phosphorus_food_id,
                    note_text
                )
                VALUES (
                    @food_phosphorus_food_id,
                    @note_text
                )
                ON CONFLICT (food_phosphorus_food_id)
                DO UPDATE SET
                    note_text = EXCLUDED.note_text,
                    updated_at = CURRENT_TIMESTAMP;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("food_phosphorus_food_id", foodPhosphorusFoodId);
            cmd.Parameters.AddWithValue("note_text", (object?)noteText ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public int GetDailyTotal(long personId, DateTime intakeDate)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT COALESCE(SUM(phosphorus_mg), 0)
                FROM food_phosphorus_intake
                WHERE person_id = @person_id
                  AND consumed_at::date = @intake_date;
            ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("intake_date", intakeDate.Date);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<FoodPhosphorusRunningTotal> GetRunningDailyTotals(long personId)
        {
            var list = new List<FoodPhosphorusRunningTotal>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureFoodPhosphorusIntakeColumns(conn);

            const string sql = @"
                WITH binder_constants AS (
                    SELECT
                        800 AS mg_per_pill,
                        0.075 AS binding_efficiency
                ),
                calculated_intake AS (
                    SELECT
                        f.food_phosphorus_intake_id,
                        f.consumed_at::date AS intake_date,
                        f.consumed_at,
                        f.food_name,
                        f.phosphorus_mg,
                        COALESCE(f.calories, 0) AS calories,
                        COALESCE(f.sodium_mg, 0) AS sodium_mg,
                        COALESCE(f.protein_g, 0) AS protein_g,
                        COALESCE(f.potassium_mg, 0) AS potassium_mg,
                        COALESCE(f.fluid_ml, 0) AS fluid_ml,
                        COALESCE(f.binders, 0) AS binders,
                        GREATEST(
                            f.phosphorus_mg - (COALESCE(f.binders, 0) * bc.mg_per_pill * bc.binding_efficiency),
                            0
                        ) AS net_item_phos_mg
                    FROM food_phosphorus_intake f
                    CROSS JOIN binder_constants bc
                    WHERE f.person_id = @person_id
                )
                SELECT
                    intake_date,
                    consumed_at::timestamp,
                    food_name,
                    phosphorus_mg AS raw_phos_mg,
                    calories,
                    sodium_mg,
                    protein_g,
                    potassium_mg,
                    fluid_ml,
                    binders AS pills_taken,
                    net_item_phos_mg,
                    SUM(net_item_phos_mg) OVER (
                        PARTITION BY intake_date
                        ORDER BY consumed_at, food_phosphorus_intake_id
                    ) AS running_net_daily_mg,
                    SUM(calories) OVER (
                        PARTITION BY intake_date
                        ORDER BY consumed_at, food_phosphorus_intake_id
                    ) AS running_daily_calories,
                    SUM(sodium_mg) OVER (
                        PARTITION BY intake_date
                        ORDER BY consumed_at, food_phosphorus_intake_id
                    ) AS running_daily_sodium_mg,
                    SUM(protein_g) OVER (
                        PARTITION BY intake_date
                        ORDER BY consumed_at, food_phosphorus_intake_id
                    ) AS running_daily_protein_g,
                    SUM(potassium_mg) OVER (
                        PARTITION BY intake_date
                        ORDER BY consumed_at, food_phosphorus_intake_id
                    ) AS running_daily_potassium_mg,
                    SUM(fluid_ml) OVER (
                        PARTITION BY intake_date
                        ORDER BY consumed_at, food_phosphorus_intake_id
                    ) AS running_daily_fluid_ml
                FROM calculated_intake
                ORDER BY intake_date DESC, consumed_at ASC;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FoodPhosphorusRunningTotal
                {
                    IntakeDate = reader.GetDateTime(0),
                    ConsumedAt = reader.GetDateTime(1),
                    FoodName = reader.GetString(2),
                    RawPhosphorusMg = reader.GetInt32(3),
                    Calories = reader.GetInt32(4),
                    SodiumMg = reader.GetInt32(5),
                    ProteinG = reader.GetDecimal(6),
                    PotassiumMg = reader.GetInt32(7),
                    FluidMl = reader.GetInt32(8),
                    PillsTaken = reader.GetInt32(9),
                    NetItemPhosphorusMg = reader.GetDecimal(10),
                    RunningNetDailyMg = reader.GetDecimal(11),
                    RunningDailyCalories = reader.GetInt64(12),
                    RunningDailySodiumMg = reader.GetInt64(13),
                    RunningDailyProteinG = reader.GetDecimal(14),
                    RunningDailyPotassiumMg = reader.GetInt64(15),
                    RunningDailyFluidMl = reader.GetInt64(16)
                });
            }

            return list;
        }

        public void Delete(long foodPhosphorusIntakeId, string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "CALL sp_delete_food_phosphorus_intake(@p_food_phosphorus_intake_id, @p_entered_by)",
                conn);

            cmd.Parameters.AddWithValue("p_food_phosphorus_intake_id", foodPhosphorusIntakeId);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

            cmd.ExecuteNonQuery();
        }
    }
}
