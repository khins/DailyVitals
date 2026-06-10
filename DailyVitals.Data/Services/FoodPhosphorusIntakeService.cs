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

            using var cmd = new NpgsqlCommand(
                @"
                WITH inserted AS (
                    INSERT INTO food_phosphorus_intake (
                        person_id,
                        food_name,
                        phosphorus_mg,
                        calories,
                        sodium_mg,
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
                        @p_person_id,
                        TRIM(@p_food_name),
                        @p_phosphorus_mg,
                        @p_calories,
                        @p_sodium_mg,
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
            cmd.Parameters.AddWithValue("p_food_name", foodName);
            cmd.Parameters.AddWithValue("p_phosphorus_mg", phosphorusMg);
            cmd.Parameters.Add("p_calories", NpgsqlDbType.Integer).Value =
                (object?)calories ?? DBNull.Value;
            cmd.Parameters.Add("p_sodium_mg", NpgsqlDbType.Integer).Value =
                (object?)sodiumMg ?? DBNull.Value;
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

        public List<FoodPhosphorusIntake> GetHistory(long personId)
        {
            var list = new List<FoodPhosphorusIntake>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureFoodPhosphorusIntakeColumns(conn);

            const string sql = @"
                SELECT
                    food_phosphorus_intake_id,
                    person_id,
                    food_name,
                    phosphorus_mg,
                    calories,
                    sodium_mg,
                    COALESCE(binders, 0) AS binders,
                    consumed_at::timestamp,
                    notes,
                    serving_description,
                    estimated_by_ai,
                    ai_provider,
                    ai_confidence,
                    source_notes
                FROM food_phosphorus_intake
                WHERE person_id = @person_id
                ORDER BY consumed_at DESC, food_phosphorus_intake_id DESC;
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
                    Binders = reader.GetInt32(6),
                    ConsumedAt = reader.GetDateTime(7),
                    Notes = reader.IsDBNull(8) ? null : reader.GetString(8),
                    ServingDescription = reader.IsDBNull(9) ? null : reader.GetString(9),
                    EstimatedByAi = reader.GetBoolean(10),
                    AiProvider = reader.IsDBNull(11) ? null : reader.GetString(11),
                    AiConfidence = reader.IsDBNull(12) ? null : reader.GetString(12),
                    SourceNotes = reader.IsDBNull(13) ? null : reader.GetString(13)
                });
            }

            return list;
        }

        private static void EnsureFoodPhosphorusIntakeColumns(NpgsqlConnection conn)
        {
            const string sql = @"
                ALTER TABLE public.food_phosphorus_intake
                    ADD COLUMN IF NOT EXISTS binders integer NOT NULL DEFAULT 0,
                    ADD COLUMN IF NOT EXISTS calories integer NULL,
                    ADD COLUMN IF NOT EXISTS sodium_mg integer NULL,
                    ADD COLUMN IF NOT EXISTS serving_description varchar(200) NULL,
                    ADD COLUMN IF NOT EXISTS estimated_by_ai boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS ai_provider varchar(50) NULL,
                    ADD COLUMN IF NOT EXISTS ai_confidence varchar(20) NULL,
                    ADD COLUMN IF NOT EXISTS source_notes text NULL;

                DO $$
                BEGIN
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
                        WHERE conname = 'food_phosphorus_intake_binders_check'
                    ) THEN
                        ALTER TABLE public.food_phosphorus_intake
                            ADD CONSTRAINT food_phosphorus_intake_binders_check CHECK (binders >= 0);
                    END IF;
                END $$;";

            using var cmd = new NpgsqlCommand(sql, conn);
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
                    ) AS running_daily_sodium_mg
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
                    PillsTaken = reader.GetInt32(6),
                    NetItemPhosphorusMg = reader.GetDecimal(7),
                    RunningNetDailyMg = reader.GetDecimal(8),
                    RunningDailyCalories = reader.GetInt64(9),
                    RunningDailySodiumMg = reader.GetInt64(10)
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
