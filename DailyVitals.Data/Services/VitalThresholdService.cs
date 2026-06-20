using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using System;

namespace DailyVitals.Data.Services
{
    public class VitalThresholdService
    {
        public const string BloodPressureSystolic = "blood_pressure_systolic";
        public const string BloodPressureDiastolic = "blood_pressure_diastolic";

        public VitalThreshold? GetActiveThreshold(long personId, string vitalType)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureVitalThresholdTable(conn);

            const string sql = @"
                SELECT threshold_id,
                       vital_type,
                       person_id,
                       min_value,
                       max_value,
                       severity,
                       is_active,
                       created_at
                FROM public.vital_threshold
                WHERE vital_type = @vital_type
                  AND is_active = true
                  AND (person_id = @person_id OR person_id IS NULL)
                ORDER BY CASE WHEN person_id = @person_id THEN 0 ELSE 1 END,
                         threshold_id DESC
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("vital_type", vitalType);

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? ReadThreshold(reader) : null;
        }

        public void SavePersonThreshold(long personId, string vitalType, decimal? minValue, decimal? maxValue, string severity = "medium")
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureVitalThresholdTable(conn);

            const string deactivateSql = @"
                UPDATE public.vital_threshold
                SET is_active = false
                WHERE person_id = @person_id
                  AND vital_type = @vital_type
                  AND is_active = true;";

            using (var deactivateCmd = new NpgsqlCommand(deactivateSql, conn))
            {
                deactivateCmd.Parameters.AddWithValue("person_id", personId);
                deactivateCmd.Parameters.AddWithValue("vital_type", vitalType);
                deactivateCmd.ExecuteNonQuery();
            }

            if (!minValue.HasValue && !maxValue.HasValue)
                return;

            const string insertSql = @"
                INSERT INTO public.vital_threshold (
                    vital_type,
                    person_id,
                    min_value,
                    max_value,
                    severity,
                    is_active
                )
                VALUES (
                    @vital_type,
                    @person_id,
                    @min_value,
                    @max_value,
                    @severity,
                    true
                );";

            using var insertCmd = new NpgsqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("vital_type", vitalType);
            insertCmd.Parameters.AddWithValue("person_id", personId);
            insertCmd.Parameters.AddWithValue("min_value", (object?)minValue ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("max_value", (object?)maxValue ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("severity", severity);
            insertCmd.ExecuteNonQuery();
        }

        private static VitalThreshold ReadThreshold(NpgsqlDataReader reader)
        {
            return new VitalThreshold
            {
                ThresholdId = reader.GetInt64(0),
                VitalType = reader.GetString(1),
                PersonId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                MinValue = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                MaxValue = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                Severity = reader.GetString(5),
                IsActive = reader.GetBoolean(6),
                CreatedAt = reader.GetDateTime(7)
            };
        }

        private static void EnsureVitalThresholdTable(NpgsqlConnection conn)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS public.vital_threshold (
                    threshold_id bigserial NOT NULL,
                    vital_type varchar(50) NOT NULL,
                    person_id int8 NULL,
                    min_value numeric NULL,
                    max_value numeric NULL,
                    severity varchar(20) NOT NULL DEFAULT 'medium',
                    is_active bool NOT NULL DEFAULT true,
                    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT vital_threshold_pkey PRIMARY KEY (threshold_id)
                );";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
    }
}
