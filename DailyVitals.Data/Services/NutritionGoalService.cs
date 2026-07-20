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
                    fluid_limit_ml,
                    phosphorus_enabled,
                    sodium_enabled,
                    calorie_enabled,
                    protein_enabled,
                    potassium_enabled,
                    fluid_enabled,
                    sugar_limit_g
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
                FluidLimitMl = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                PhosphorusEnabled = reader.GetBoolean(9),
                SodiumEnabled = reader.GetBoolean(10),
                CalorieEnabled = reader.GetBoolean(11),
                ProteinEnabled = reader.GetBoolean(12),
                PotassiumEnabled = reader.GetBoolean(13),
                FluidEnabled = reader.GetBoolean(14),
                SugarLimitG = reader.IsDBNull(15) ? null : reader.GetInt32(15)
            };
        }

        public long SaveGoal(NutritionGoal goal)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();


            if (goal.NutritionGoalId > 0)
            {
                const string updateSql = @"
                    UPDATE public.nutrition_goal
                    SET sodium_limit_mg = @sodium_limit_mg,
                        phosphorus_limit_mg = @phosphorus_limit_mg,
                        calorie_limit = @calorie_limit,
                        effective_date = @effective_date,
                        protein_target_g = @protein_target_g,
                        potassium_limit_mg = @potassium_limit_mg,
                        fluid_limit_ml = @fluid_limit_ml,
                        phosphorus_enabled = @phosphorus_enabled,
                        sodium_enabled = @sodium_enabled,
                        calorie_enabled = @calorie_enabled,
                        protein_enabled = @protein_enabled,
                        potassium_enabled = @potassium_enabled,
                        fluid_enabled = @fluid_enabled,
                        sugar_limit_g = @sugar_limit_g
                    WHERE nutrition_goal_id = @nutrition_goal_id
                      AND person_id = @person_id
                    RETURNING nutrition_goal_id;";

                using var updateCmd = new NpgsqlCommand(updateSql, conn);
                AddGoalParameters(updateCmd, goal);
                var updatedId = updateCmd.ExecuteScalar();
                if (updatedId is not null and not DBNull)
                    return Convert.ToInt64(updatedId);
            }

            const string insertSql = @"
                INSERT INTO public.nutrition_goal (
                    person_id,
                    sodium_limit_mg,
                    phosphorus_limit_mg,
                    calorie_limit,
                    effective_date,
                    protein_target_g,
                    potassium_limit_mg,
                    fluid_limit_ml,
                    phosphorus_enabled,
                    sodium_enabled,
                    calorie_enabled,
                    protein_enabled,
                    potassium_enabled,
                    fluid_enabled,
                    sugar_limit_g
                )
                VALUES (
                    @person_id,
                    @sodium_limit_mg,
                    @phosphorus_limit_mg,
                    @calorie_limit,
                    @effective_date,
                    @protein_target_g,
                    @potassium_limit_mg,
                    @fluid_limit_ml,
                    @phosphorus_enabled,
                    @sodium_enabled,
                    @calorie_enabled,
                    @protein_enabled,
                    @potassium_enabled,
                    @fluid_enabled,
                    @sugar_limit_g
                )
                RETURNING nutrition_goal_id;";

            using var insertCmd = new NpgsqlCommand(insertSql, conn);
            AddGoalParameters(insertCmd, goal);
            var insertedId = insertCmd.ExecuteScalar();
            if (insertedId is null or DBNull)
                throw new InvalidOperationException("Nutrition goal insert failed. No ID returned.");

            return Convert.ToInt64(insertedId);
        }

        private static void AddGoalParameters(NpgsqlCommand cmd, NutritionGoal goal)
        {
            cmd.Parameters.AddWithValue("nutrition_goal_id", goal.NutritionGoalId);
            cmd.Parameters.AddWithValue("person_id", goal.PersonId);
            cmd.Parameters.AddWithValue("sodium_limit_mg", goal.SodiumLimitMg);
            cmd.Parameters.AddWithValue("phosphorus_limit_mg", goal.PhosphorusLimitMg);
            cmd.Parameters.AddWithValue("calorie_limit", goal.CalorieLimit);
            cmd.Parameters.AddWithValue("effective_date", goal.EffectiveDate.Date);
            cmd.Parameters.AddWithValue("protein_target_g", (object?)goal.ProteinTargetG ?? DBNull.Value);
            cmd.Parameters.AddWithValue("potassium_limit_mg", (object?)goal.PotassiumLimitMg ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fluid_limit_ml", (object?)goal.FluidLimitMl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("phosphorus_enabled", goal.PhosphorusEnabled);
            cmd.Parameters.AddWithValue("sodium_enabled", goal.SodiumEnabled);
            cmd.Parameters.AddWithValue("calorie_enabled", goal.CalorieEnabled);
            cmd.Parameters.AddWithValue("protein_enabled", goal.ProteinEnabled);
            cmd.Parameters.AddWithValue("potassium_enabled", goal.PotassiumEnabled);
            cmd.Parameters.AddWithValue("fluid_enabled", goal.FluidEnabled);
            cmd.Parameters.AddWithValue("sugar_limit_g", (object?)goal.SugarLimitG ?? DBNull.Value);
        }

    }
}
