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
            int glucose_value,
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
            cmd.Parameters.AddWithValue("p_glucose_value", glucose_value);
            cmd.Parameters.AddWithValue("p_reading_time", readingTime);
            cmd.Parameters.AddWithValue("p_notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

            return (long)cmd.ExecuteScalar();
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
                    notes
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
                        : reader.GetString(3)
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

    }

}
