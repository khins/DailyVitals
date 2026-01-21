using System;
using System.Data;
using Npgsql;
using DailyVitals.Data.Configuration;

namespace DailyVitals.Data.Services
{
    public class BloodPressureService
    {
        public long InsertBloodPressure(
            long personId,
            int systolic,
            int diastolic,
            int pulse,
            DateTime readingTime,
            string notes,
            string enteredBy
        )
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_insert_blood_pressure(@p_person_id, @p_systolic, @p_diastolic, @p_pulse, @p_reading_time, @p_notes, @p_entered_by)",
                conn
            );

            cmd.Parameters.AddWithValue("p_person_id", personId);
            cmd.Parameters.AddWithValue("p_systolic", systolic);
            cmd.Parameters.AddWithValue("p_diastolic", diastolic);
            cmd.Parameters.AddWithValue("p_pulse", pulse);
            cmd.Parameters.AddWithValue("p_reading_time", readingTime);
            cmd.Parameters.AddWithValue("p_notes", (object?)notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                throw new Exception("Blood pressure insert failed. No ID returned.");

            return Convert.ToInt64(result);
        }
    }
}
