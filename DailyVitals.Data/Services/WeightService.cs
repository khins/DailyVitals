using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace DailyVitals.Data.Services
{
    public class WeightService
    {
        public long InsertWeight(
            long personId,
            decimal weightValue,
            string weightUnit,
            decimal heightFt,
            DateTime readingTime,
            string notes,
            string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_insert_weight(@p_person_id, @p_weight_value, @p_weight_unit, @p_reading_time, @p_notes, @p_entered_by)",
                conn);

            cmd.Parameters.AddWithValue("p_person_id", personId);
            cmd.Parameters.AddWithValue("p_weight_value", weightValue);
            cmd.Parameters.AddWithValue("p_weight_unit", weightUnit);
            cmd.Parameters.AddWithValue("p_reading_time", readingTime);
            cmd.Parameters.AddWithValue("p_notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

            var result = cmd.ExecuteScalar();

            if (result is null or DBNull)
                throw new Exception("Weight insert failed. No ID returned.");

            return Convert.ToInt64(result);
        }

        public List<WeightReading> GetHistory(long personId)
        {
            var list = new List<WeightReading>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
            SELECT
                w.weight_id,
                w.weight_value,
                w.weight_unit,
                w.reading_time,
                w.notes,
                COALESCE(p.height_ft, w.height_ft) AS height_ft,
                w.created_at,
                w.updated_at
            FROM weight w
            JOIN person p
                ON p.person_id = w.person_id
            WHERE w.person_id = @person_id
            ORDER BY w.reading_time DESC;
        ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new WeightReading
                {
                    WeightId = reader.GetInt64(0),
                    WeightValue = reader.GetDecimal(1),
                    WeightUnit = reader.GetString(2),
                    ReadingTime = reader.GetDateTime(3),
                    Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
                    HeightFt = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                    CreatedAt = reader.GetDateTime(6),
                    UpdatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                });
            }


            return list;
        }

        public void DeleteWeight(long weightId, string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_delete_weight(@p_weight_id, @p_entered_by)",
                conn);

            cmd.Parameters.AddWithValue("p_weight_id", weightId);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

            cmd.ExecuteNonQuery();
        }

        public bool DeleteWeightForPerson(long personId, long weightId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                DELETE FROM public.weight
                WHERE weight_id = @weight_id
                  AND person_id = @person_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("weight_id", weightId);

            return cmd.ExecuteNonQuery() == 1;
        }

        public void UpdateWeight(
                long weightId,
                decimal weightValue,
                string weightUnit,
                decimal heightFt,
                DateTime readingTime,
                string notes,
                string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_update_weight(@id,@val,@unit,@time,@notes,@by)", conn);

            cmd.Parameters.AddWithValue("id", weightId);
            cmd.Parameters.AddWithValue("val", weightValue);
            cmd.Parameters.AddWithValue("unit", weightUnit);
            cmd.Parameters.AddWithValue("time", readingTime);
            cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("by", enteredBy);

            cmd.ExecuteNonQuery();
            SetUpdatedAt(conn, weightId);
        }

        public bool UpdateWeightForPerson(
                long personId,
                long weightId,
                decimal weightValue,
                string weightUnit,
                DateTime readingTime,
                string notes,
                string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                UPDATE public.weight
                SET weight_value = @weight_value,
                    weight_unit = @weight_unit,
                    reading_time = @reading_time,
                    notes = @notes,
                    updated_at = CURRENT_TIMESTAMP
                WHERE weight_id = @weight_id
                  AND person_id = @person_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("weight_id", weightId);
            cmd.Parameters.AddWithValue("weight_value", weightValue);
            cmd.Parameters.AddWithValue("weight_unit", weightUnit);
            cmd.Parameters.AddWithValue("reading_time", readingTime);
            cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);

            return cmd.ExecuteNonQuery() == 1;
        }

        public WeightReading? GetLatestForPerson(long personId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                    SELECT
                        w.weight_id,
                        w.weight_value,
                        w.weight_unit,
                        COALESCE(p.height_ft, w.height_ft) AS height_ft,
                        w.reading_time,
                        w.notes,
                        w.created_at,
                        w.updated_at
                    FROM weight w
                    JOIN person p
                        ON p.person_id = w.person_id
                    WHERE w.person_id = @person_id
                    ORDER BY w.reading_time DESC
                    LIMIT 1;
                ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new WeightReading
            {
                WeightId = reader.GetInt64(0),
                WeightValue = reader.GetDecimal(1),
                WeightUnit = reader.GetString(2),
                HeightFt = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                ReadingTime = reader.GetDateTime(4),
                Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedAt = reader.GetDateTime(6),
                UpdatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            };
        }

        public List<TrendPoint> GetWeightTrend(long personId, int maxPoints = 10)
        {
            var list = new List<TrendPoint>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                    SELECT reading_time, weight_value
                    FROM weight
                    WHERE person_id = @person_id
                    ORDER BY reading_time DESC
                    LIMIT @limit;
                ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("limit", maxPoints);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TrendPoint
                {
                    Date = reader.GetDateTime(0),
                    Value = reader.GetDecimal(1)
                });
            }

            // Reverse so chart draws left → right chronologically
            list.Reverse();
            return list;
        }

        private static void SetUpdatedAt(NpgsqlConnection conn, long weightId)
        {
            using var cmd = new NpgsqlCommand(
                "UPDATE public.weight SET updated_at = CURRENT_TIMESTAMP WHERE weight_id = @weight_id",
                conn);

            cmd.Parameters.AddWithValue("weight_id", weightId);
            cmd.ExecuteNonQuery();
        }


    }

}
