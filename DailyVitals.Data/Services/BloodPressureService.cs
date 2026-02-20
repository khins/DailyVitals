using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;

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

        public BloodPressureReading GetLatestForPerson(long personId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                    SELECT bp_id, systolic, diastolic, pulse, reading_time, notes
                    FROM blood_pressure
                    WHERE person_id = @person_id
                    ORDER BY reading_time DESC
                    LIMIT 1;
                ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new BloodPressureReading
            {
                BloodPressureId = reader.GetInt64(0),
                Systolic = reader.GetInt32(1),
                Diastolic = reader.GetInt32(2),
                Pulse = reader.GetInt32(3),
                ReadingTime = reader.GetDateTime(4),
                Notes = reader.IsDBNull(5) ? null : reader.GetString(5)
            };
        }

        public void UpdateBloodPressure(
                    long bpId,
                    int systolic,
                    int diastolic,
                    int pulse,
                    DateTime readingTime,
                    string notes,
                    string updatedBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                    UPDATE blood_pressure
                    SET systolic = @systolic,
                        diastolic = @diastolic,
                        pulse = @pulse,
                        reading_time = @reading_time,
                        notes = @notes
                    WHERE bp_id = @bp_id;
                ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("bp_id", bpId);
            cmd.Parameters.AddWithValue("systolic", systolic);
            cmd.Parameters.AddWithValue("diastolic", diastolic);
            cmd.Parameters.AddWithValue("pulse", pulse);
            cmd.Parameters.AddWithValue("reading_time", readingTime);
            cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
            
            cmd.ExecuteNonQuery();
        }

        public List<BloodPressureReading> GetHistoryForPerson(long personId)
        {
            var list = new List<BloodPressureReading>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                    SELECT bp_id, systolic, diastolic, pulse, reading_time, notes
                    FROM blood_pressure
                    WHERE person_id = @person_id
                    ORDER BY reading_time DESC;
                ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BloodPressureReading
                {
                    BloodPressureId = reader.GetInt64(0),
                    Systolic = reader.GetInt32(1),
                    Diastolic = reader.GetInt32(2),
                    Pulse = reader.GetInt32(3),
                    ReadingTime = reader.GetDateTime(4),
                    Notes = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return list;
        }

        public void DeleteBloodPressure(long bpId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                    DELETE FROM blood_pressure
                    WHERE bp_id = @bp_id;
                ";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("bp_id", bpId);

            cmd.ExecuteNonQuery();
        }

        public List<BloodPressureReading> GetTrend(
    long personId,
    DateTime startDate,
    DateTime endDate)
        {
            var list = new List<BloodPressureReading>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT * FROM sp_get_bp_trend(@p_person_id, @p_start_date, @p_end_date)",
                conn);

            cmd.Parameters.AddWithValue("p_person_id", personId);
            cmd.Parameters.AddWithValue("p_start_date", startDate.Date);
            cmd.Parameters.AddWithValue("p_end_date", endDate.Date.AddDays(1));
            // AddDays(1) makes the end date inclusive

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new BloodPressureReading
                {
                    ReadingTime = reader.GetDateTime(0),
                    Systolic = reader.GetInt32(1),
                    Diastolic = reader.GetInt32(2)
                });
            }

            return list;
        }



    }
}
