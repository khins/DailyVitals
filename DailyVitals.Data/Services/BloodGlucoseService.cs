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

            const string sql = @"
            INSERT INTO blood_glucose
                (person_id, glucose_value, reading_time, notes )
            VALUES
                (@person_id, @glucose_value, @reading_time, @notes)
            RETURNING glucose_id;
        ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("glucose_value", glucose_value);
            cmd.Parameters.AddWithValue("reading_time", readingTime);
            cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
        
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
    }

}
