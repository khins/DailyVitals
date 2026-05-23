using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
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
                    Binders = reader.GetInt32(4),
                    ConsumedAt = reader.GetDateTime(5),
                    Notes = reader.IsDBNull(6) ? null : reader.GetString(6),
                    ServingDescription = reader.IsDBNull(7) ? null : reader.GetString(7),
                    EstimatedByAi = reader.GetBoolean(8),
                    AiProvider = reader.IsDBNull(9) ? null : reader.GetString(9),
                    AiConfidence = reader.IsDBNull(10) ? null : reader.GetString(10),
                    SourceNotes = reader.IsDBNull(11) ? null : reader.GetString(11)
                });
            }

            return list;
        }

        private static void EnsureFoodPhosphorusIntakeColumns(NpgsqlConnection conn)
        {
            const string sql = @"
                ALTER TABLE public.food_phosphorus_intake
                    ADD COLUMN IF NOT EXISTS binders integer NOT NULL DEFAULT 0,
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
