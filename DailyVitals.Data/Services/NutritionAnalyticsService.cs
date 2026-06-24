using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace DailyVitals.Data.Services
{
    public class NutritionAnalyticsService
    {
        public List<NutritionAnalyticsDailyRow> GetDailyAnalytics(long personId, int days = 45)
        {
            var list = new List<NutritionAnalyticsDailyRow>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                WITH date_window AS (
                    SELECT generate_series(
                        CURRENT_DATE - (@days - 1) * INTERVAL '1 day',
                        CURRENT_DATE,
                        INTERVAL '1 day'
                    )::date AS day
                ),
                food_daily AS (
                    SELECT
                        f.consumed_at::date AS day,
                        COALESCE(SUM(f.calories), 0)::int AS calories_in,
                        COALESCE(SUM(f.sodium_mg), 0)::int AS sodium_mg,
                        COALESCE(SUM(f.phosphorus_mg), 0)::int AS phosphorus_mg,
                        COALESCE(SUM(f.phosphorus_mg), 0) AS net_phosphorus_mg
                    FROM public.food_phosphorus_intake f
                    WHERE f.person_id = @person_id
                      AND f.consumed_at::date >= CURRENT_DATE - (@days - 1) * INTERVAL '1 day'
                    GROUP BY f.consumed_at::date
                ),
                exercise_daily AS (
                    SELECT
                        start_time::date AS day,
                        COALESCE(SUM(calories_expended), 0)::int AS exercise_calories
                    FROM public.exercise_session
                    WHERE person_id = @person_id
                      AND start_time::date >= CURRENT_DATE - (@days - 1) * INTERVAL '1 day'
                    GROUP BY start_time::date
                ),
                weight_daily AS (
                    SELECT DISTINCT ON (reading_time::date)
                        reading_time::date AS day,
                        weight_value
                    FROM public.weight
                    WHERE person_id = @person_id
                      AND reading_time::date >= CURRENT_DATE - (@days - 1) * INTERVAL '1 day'
                    ORDER BY reading_time::date, reading_time DESC, weight_id DESC
                )
                SELECT
                    dw.day,
                    COALESCE(fd.calories_in, 0) AS calories_in,
                    COALESCE(ed.exercise_calories, 0) AS exercise_calories,
                    COALESCE(fd.calories_in, 0) - COALESCE(ed.exercise_calories, 0) AS calorie_balance,
                    COALESCE(fd.sodium_mg, 0) AS sodium_mg,
                    COALESCE(fd.phosphorus_mg, 0) AS phosphorus_mg,
                    COALESCE(fd.net_phosphorus_mg, 0) AS net_phosphorus_mg,
                    wd.weight_value,
                    ng.calorie_limit,
                    ng.sodium_limit_mg,
                    ng.phosphorus_limit_mg
                FROM date_window dw
                LEFT JOIN food_daily fd ON fd.day = dw.day
                LEFT JOIN exercise_daily ed ON ed.day = dw.day
                LEFT JOIN weight_daily wd ON wd.day = dw.day
                LEFT JOIN LATERAL (
                    SELECT calorie_limit, sodium_limit_mg, phosphorus_limit_mg
                    FROM public.nutrition_goal
                    WHERE person_id = @person_id
                      AND effective_date <= dw.day
                    ORDER BY effective_date DESC, nutrition_goal_id DESC
                    LIMIT 1
                ) ng ON TRUE
                ORDER BY dw.day;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("days", days);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new NutritionAnalyticsDailyRow
                {
                    Date = reader.GetDateTime(0),
                    CaloriesIn = reader.GetInt32(1),
                    ExerciseCalories = reader.GetInt32(2),
                    CalorieBalance = reader.GetInt32(3),
                    SodiumMg = reader.GetInt32(4),
                    PhosphorusMg = reader.GetInt32(5),
                    NetPhosphorusMg = reader.GetDecimal(6),
                    WeightValue = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                    CalorieLimit = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    SodiumLimitMg = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    PhosphorusLimitMg = reader.IsDBNull(10) ? null : reader.GetInt32(10)
                });
            }

            return list;
        }

        public List<ExerciseAnalyticsSessionRow> GetExerciseSessions(long personId)
        {
            var list = new List<ExerciseAnalyticsSessionRow>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT
                    e.start_time,
                    COALESCE(et.exercise_name, 'Exercise') AS exercise_name,
                    COALESCE(e.duration_minutes, 0) AS duration_minutes,
                    COALESCE(e.calories_expended, 0) AS calories_expended
                FROM public.exercise_session e
                LEFT JOIN public.exercise_type et
                    ON et.exercise_type_id = e.exercise_type_id
                WHERE e.person_id = @person_id
                ORDER BY e.start_time;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ExerciseAnalyticsSessionRow
                {
                    StartTime = reader.GetDateTime(0),
                    ExerciseName = reader.GetString(1),
                    DurationMinutes = reader.GetDecimal(2),
                    CaloriesExpended = reader.GetDecimal(3)
                });
            }

            return list;
        }

        public List<ExerciseAnalyticsMonthlyRow> GetMonthlyExerciseAnalytics(long personId)
        {
            var list = new List<ExerciseAnalyticsMonthlyRow>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT
                    date_trunc('month', start_time)::date AS month,
                    COUNT(*)::int AS sessions,
                    COALESCE(SUM(calories_expended), 0) AS total_calories,
                    COALESCE(AVG(calories_expended), 0) AS average_calories
                FROM public.exercise_session
                WHERE person_id = @person_id
                GROUP BY date_trunc('month', start_time)::date
                ORDER BY month;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ExerciseAnalyticsMonthlyRow
                {
                    Month = reader.GetDateTime(0),
                    Sessions = reader.GetInt32(1),
                    TotalCalories = reader.GetDecimal(2),
                    AverageCalories = reader.GetDecimal(3)
                });
            }

            return list;
        }
    }
}
