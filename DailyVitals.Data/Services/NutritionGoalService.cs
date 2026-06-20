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

        public long SaveGoal(NutritionGoal goal)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            EnsureNutritionGoalTable(conn);

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
                        fluid_limit_ml = @fluid_limit_ml
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
                    fluid_limit_ml
                )
                VALUES (
                    @person_id,
                    @sodium_limit_mg,
                    @phosphorus_limit_mg,
                    @calorie_limit,
                    @effective_date,
                    @protein_target_g,
                    @potassium_limit_mg,
                    @fluid_limit_ml
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
        }

        private static void EnsureNutritionGoalTable(NpgsqlConnection conn)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS public.nutrition_goal (
                    nutrition_goal_id bigserial NOT NULL,
                    person_id int8 NOT NULL,
                    sodium_limit_mg int4 NOT NULL,
                    phosphorus_limit_mg int4 NOT NULL,
                    calorie_limit int4 NOT NULL,
                    effective_date date NOT NULL,
                    protein_target_g int4 NULL,
                    potassium_limit_mg int4 NULL,
                    fluid_limit_ml int4 NULL,
                    CONSTRAINT nutrition_goal_pkey PRIMARY KEY (nutrition_goal_id)
                );

                ALTER TABLE public.nutrition_goal
                    ADD COLUMN IF NOT EXISTS protein_target_g int4 NULL,
                    ADD COLUMN IF NOT EXISTS potassium_limit_mg int4 NULL,
                    ADD COLUMN IF NOT EXISTS fluid_limit_ml int4 NULL;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }
    }
}
