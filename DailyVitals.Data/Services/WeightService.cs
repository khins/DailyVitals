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

            return (long)cmd.ExecuteScalar();
        }

        public List<WeightReading> GetHistory(long personId)
        {
            var list = new List<WeightReading>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
            SELECT weight_id, weight_value, weight_unit, reading_time, notes
            FROM weight
            WHERE person_id = @person_id
            ORDER BY reading_time DESC;
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
                    Notes = reader.IsDBNull(4) ? null : reader.GetString(4)
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

    }

}
