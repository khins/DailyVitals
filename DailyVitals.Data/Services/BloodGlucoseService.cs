using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace DailyVitals.Data.Services
{
    public class BloodGlucoseService
    {
        public long Insert(
            long personId,
            int glucoseValue,
            DateTime readingTime,
            string notes,
            string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
              "SELECT sp_insert_blood_glucose(@p_person_id, @p_glucose_value, @p_reading_time, @p_notes, @p_entered_by)",
              conn);

            cmd.Parameters.AddWithValue("p_person_id", personId);
            cmd.Parameters.AddWithValue("p_glucose_value", glucoseValue);
            cmd.Parameters.AddWithValue("p_reading_time", readingTime);
            cmd.Parameters.AddWithValue("p_notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

            var result = cmd.ExecuteScalar();

            if (result is null or DBNull)
                throw new Exception("Blood glucose insert failed. No ID returned.");

            return Convert.ToInt64(result);
        }

        public void Update(
            long glucoseId,
            long personId,
            int glucoseValue,
            DateTime readingTime,
            string notes,
            string updatedBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                WITH updated AS (
                    UPDATE blood_glucose
                    SET
                        glucose_value = @glucose_value,
                        reading_time = @reading_time,
                        notes = @notes,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE glucose_id = @glucose_id
                      AND person_id = @person_id
                    RETURNING glucose_id
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
                        'blood_glucose',
                        glucose_id,
                        'UPDATE',
                        @updated_by,
                        jsonb_build_object(
                            'glucose_value', @glucose_value,
                            'reading_time', @reading_time,
                            'notes', @notes
                        )
                    FROM updated
                    RETURNING 1
                )
                SELECT glucose_id
                FROM updated;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("glucose_id", glucoseId);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("glucose_value", glucoseValue);
            cmd.Parameters.AddWithValue("reading_time", readingTime);
            cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("updated_by", updatedBy);

            var result = cmd.ExecuteScalar();
            if (result is null or DBNull)
                throw new Exception("Blood glucose update failed. No matching record was updated.");
        }

        public List<BloodGlucoseReading> GetHistory(long personId)
        {
            var list = new List<BloodGlucoseReading>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT
                    glucose_id,
                    glucose_value,
                    reading_time::timestamp,
                    notes,
                    created_at,
                    updated_at
                FROM blood_glucose
                WHERE person_id = @person_id
                ORDER BY reading_time DESC;
            ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BloodGlucoseReading
                {
                    GlucoseId = reader.GetInt64(0),
                    GlucoseValue = reader.GetInt32(1),
                    ReadingTime = reader.GetDateTime(2),
                    Notes = reader.IsDBNull(3)
                        ? null
                        : reader.GetString(3),
                    CreatedAt = reader.IsDBNull(4) ? reader.GetDateTime(2) : reader.GetDateTime(4),
                    UpdatedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
                });
            }


            return list;
        }

        public void DeleteBloodGlucose(long glucoseId, string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_delete_blood_glucose(@p_glucose_id, @p_entered_by)",
                conn);

            cmd.Parameters.AddWithValue("p_glucose_id", glucoseId);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

            cmd.ExecuteNonQuery();
        }

        public bool DeleteBloodGlucoseForPerson(long personId, long glucoseId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                DELETE FROM public.blood_glucose
                WHERE glucose_id = @glucose_id
                  AND person_id = @person_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("glucose_id", glucoseId);
            cmd.Parameters.AddWithValue("person_id", personId);

            return cmd.ExecuteNonQuery() == 1;
        }

        public BloodGlucoseReading? GetLatestForPerson(long personId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                    SELECT glucose_id,
                           glucose_value,
                           reading_time,
                           notes,
                           created_at,
                           updated_at
                    FROM blood_glucose
                    WHERE person_id = @person_id
                    ORDER BY reading_time DESC
                    LIMIT 1;
                ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new BloodGlucoseReading
            {
                GlucoseId = reader.GetInt64(0),
                GlucoseValue = reader.GetInt32(1),
                ReadingTime = reader.GetDateTime(2),
                Notes = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt = reader.IsDBNull(4) ? reader.GetDateTime(2) : reader.GetDateTime(4),
                UpdatedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
            };
        }

    }

}
