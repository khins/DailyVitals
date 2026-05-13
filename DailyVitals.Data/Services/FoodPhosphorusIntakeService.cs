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
            DateTime consumedAt,
            string? notes,
            string enteredBy)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT sp_insert_food_phosphorus_intake(@p_person_id, @p_food_name, @p_phosphorus_mg, @p_consumed_at, @p_notes, @p_entered_by)",
                conn);

            cmd.Parameters.AddWithValue("p_person_id", personId);
            cmd.Parameters.AddWithValue("p_food_name", foodName);
            cmd.Parameters.AddWithValue("p_phosphorus_mg", phosphorusMg);
            cmd.Parameters.AddWithValue("p_consumed_at", consumedAt);
            cmd.Parameters.AddWithValue("p_notes", (object?)notes ?? DBNull.Value);
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

            const string sql = @"
                SELECT
                    food_phosphorus_intake_id,
                    person_id,
                    food_name,
                    phosphorus_mg,
                    consumed_at::timestamp,
                    notes
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
                    ConsumedAt = reader.GetDateTime(4),
                    Notes = reader.IsDBNull(5) ? null : reader.GetString(5)
                });
            }

            return list;
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
