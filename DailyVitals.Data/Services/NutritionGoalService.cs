using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using System;

namespace DailyVitals.Data.Services
{
    public class NutritionGoalService
    {
        public NutritionGoal? GetActiveGoal(long personId, DateTime asOfDate)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT
                    nutrition_goal_id,
                    person_id,
                    sodium_limit_mg,
                    phosphorus_limit_mg,
                    calorie_limit,
                    effective_date,
                    protein_target_g,
                    potassium_limit_mg,
                    fluid_limit_ml
                FROM public.nutrition_goal
                WHERE person_id = @person_id
                  AND effective_date <= @as_of_date
                ORDER BY effective_date DESC, nutrition_goal_id DESC
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("as_of_date", asOfDate.Date);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new NutritionGoal
            {
                NutritionGoalId = reader.GetInt64(0),
                PersonId = reader.GetInt64(1),
                SodiumLimitMg = reader.GetInt32(2),
                PhosphorusLimitMg = reader.GetInt32(3),
                CalorieLimit = reader.GetInt32(4),
                EffectiveDate = reader.GetDateTime(5),
                ProteinTargetG = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                PotassiumLimitMg = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                FluidLimitMl = reader.IsDBNull(8) ? null : reader.GetInt32(8)
            };
        }
    }
}
