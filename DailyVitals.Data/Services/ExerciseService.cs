using DailyVitals.Domain.Models;
using global::DailyVitals.Data.Configuration;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;

namespace DailyVitals.Data.Services
{
    namespace DailyVitals.App.Services
    {
        public class ExerciseService
        {
            public void InsertExerciseSession(
                long personId,
                long exerciseTypeId,
                DateTime startTime,
                decimal durationMinutes,
                decimal? caloriesExpended,
                string intensity,
                string notes,
                string enteredBy)
            {
                try
                {
                    InsertExerciseSessionWithCalories(
                        personId,
                        exerciseTypeId,
                        startTime,
                        durationMinutes,
                        caloriesExpended,
                        intensity,
                        notes,
                        enteredBy);
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedFunction)
                {
                    InsertExerciseSessionLegacy(
                        personId,
                        exerciseTypeId,
                        startTime,
                        durationMinutes,
                        intensity,
                        notes,
                        enteredBy);
                }
            }

            private static void InsertExerciseSessionWithCalories(
                long personId,
                long exerciseTypeId,
                DateTime startTime,
                decimal durationMinutes,
                decimal? caloriesExpended,
                string intensity,
                string notes,
                string enteredBy)
            {
                using var conn = DbConnectionFactory.Create();
                conn.Open();

                using var cmd = new NpgsqlCommand(
                    "CALL sp_insert_exercise_session(" +
                    "@p_person_id, @p_exercise_type_id, @p_start_time, " +
                    "@p_duration_minutes, @p_calories_expended, @p_intensity, @p_notes, @p_entered_by)", conn);

                cmd.Parameters.AddWithValue("p_person_id", personId);
                cmd.Parameters.AddWithValue("p_exercise_type_id", exerciseTypeId);
                cmd.Parameters.AddWithValue("p_start_time", startTime);
                cmd.Parameters.AddWithValue("p_duration_minutes", durationMinutes);
                cmd.Parameters.Add("p_calories_expended", NpgsqlDbType.Numeric).Value =
                    (object?)caloriesExpended ?? DBNull.Value;
                cmd.Parameters.Add("p_intensity", NpgsqlDbType.Varchar).Value = intensity;
                cmd.Parameters.AddWithValue("p_notes", (object?)notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

                cmd.ExecuteNonQuery();
            }

            private static void InsertExerciseSessionLegacy(
                long personId,
                long exerciseTypeId,
                DateTime startTime,
                decimal durationMinutes,
                string intensity,
                string notes,
                string enteredBy)
            {
                using var conn = DbConnectionFactory.Create();
                conn.Open();

                using var cmd = new NpgsqlCommand(
                    "CALL sp_insert_exercise_session(" +
                    "@p_person_id, @p_exercise_type_id, @p_start_time, " +
                    "@p_duration_minutes, @p_intensity, @p_notes, @p_entered_by)", conn);

                cmd.Parameters.AddWithValue("p_person_id", personId);
                cmd.Parameters.AddWithValue("p_exercise_type_id", exerciseTypeId);
                cmd.Parameters.AddWithValue("p_start_time", startTime);
                cmd.Parameters.AddWithValue("p_duration_minutes", durationMinutes);
                cmd.Parameters.Add("p_intensity", NpgsqlDbType.Varchar).Value = intensity;
                cmd.Parameters.AddWithValue("p_notes", (object?)notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

                cmd.ExecuteNonQuery();
            }

            public List<ExerciseType> GetExerciseTypes()
            {
                var list = new List<ExerciseType>();

                using var conn = DbConnectionFactory.Create();
                conn.Open();

                const string sql = """
                SELECT exercise_type_id, exercise_name
                FROM exercise_type
                ORDER BY exercise_name;
            """;

                using var cmd = new NpgsqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new ExerciseType
                    {
                        ExerciseTypeId = reader.GetInt64(0),
                        ExerciseName = reader.GetString(1)
                    });
                }

                return list;
            }

            public List<ExerciseSession> GetHistory(long personId)
            {
                var list = new List<ExerciseSession>();

                using var conn = DbConnectionFactory.Create();
                conn.Open();

                using var cmd = new NpgsqlCommand(
                    "SELECT * FROM sp_get_exercise_history(@p_person_id)", conn);

                cmd.Parameters.AddWithValue("p_person_id", personId);

                using var reader = cmd.ExecuteReader();
                bool hasCaloriesExpended = reader.FieldCount > 7 &&
                    string.Equals(reader.GetName(5), "calories_expended", StringComparison.OrdinalIgnoreCase);

                while (reader.Read())
                {
                    list.Add(new ExerciseSession
                    {
                        ExerciseSessionId = reader.GetInt64(0),
                        ExerciseTypeId = reader.GetInt64(1),
                        ExerciseName = reader.GetString(2),
                        StartTime = reader.GetDateTime(3),
                        DurationMinutes = reader.GetDecimal(4),
                        CaloriesExpended = hasCaloriesExpended && !reader.IsDBNull(5)
                            ? reader.GetDecimal(5)
                            : null,
                        Intensity = reader.GetString(hasCaloriesExpended ? 6 : 5),
                        Notes = reader.IsDBNull(hasCaloriesExpended ? 7 : 6)
                            ? null
                            : reader.GetString(hasCaloriesExpended ? 7 : 6)
                    });
                }

                return list;
            }

            public void DeleteExerciseSession(long exerciseSessionId, string enteredBy)
            {
                using var conn = DbConnectionFactory.Create();
                conn.Open();

                using var cmd = new NpgsqlCommand(
                    "CALL sp_delete_exercise_session(@p_exercise_session_id, @p_entered_by)", conn);

                cmd.Parameters.AddWithValue("p_exercise_session_id", exerciseSessionId);
                cmd.Parameters.AddWithValue("p_entered_by", enteredBy);

                cmd.ExecuteNonQuery();
            }

            public int GetWeeklyTotalMinutes(long personId)
            {
                using var conn = DbConnectionFactory.Create();
                conn.Open();

                const string sql = @"
                        SELECT COALESCE(SUM(duration_minutes), 0)
                        FROM exercise_session
                        WHERE person_id = @person_id
                          AND start_time >= date_trunc('week', CURRENT_DATE) - INTERVAL '7 days'
                          AND start_time <  date_trunc('week', CURRENT_DATE);
                    ";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("person_id", personId);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }

            //GetLastWeekTotalMinutes
            public int GetLastWeekTotalMinutes(long personId)
            {
                using var conn = DbConnectionFactory.Create();
                conn.Open();

                using var cmd = new NpgsqlCommand(@"
                        SELECT COALESCE(SUM(duration_minutes), 0)
                        FROM exercise_session
                        WHERE person_id = @person_id
                          AND start_time >= date_trunc('week', CURRENT_TIMESTAMP);
                    ", conn);

                cmd.Parameters.AddWithValue("person_id", personId);

                return Convert.ToInt32(cmd.ExecuteScalar());
            }

        }
    }

}
