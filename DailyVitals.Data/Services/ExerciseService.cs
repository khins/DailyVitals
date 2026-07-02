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

            public long GetOrCreateExerciseType(string exerciseName)
            {
                var normalizedName = NormalizeExerciseName(exerciseName);

                if (string.IsNullOrWhiteSpace(normalizedName))
                    throw new ArgumentException("Exercise name is required.", nameof(exerciseName));

                using var conn = DbConnectionFactory.Create();
                conn.Open();

                const string findSql = @"
                    SELECT exercise_type_id
                    FROM public.exercise_type
                    WHERE lower(trim(exercise_name)) = lower(trim(@exercise_name))
                    LIMIT 1;";

                using (var findCmd = new NpgsqlCommand(findSql, conn))
                {
                    findCmd.Parameters.Add("exercise_name", NpgsqlDbType.Varchar).Value = normalizedName;
                    var existing = findCmd.ExecuteScalar();

                    if (existing is not null && existing != DBNull.Value)
                        return Convert.ToInt64(existing);
                }

                const string insertSql = @"
                    INSERT INTO public.exercise_type (exercise_name, category)
                    VALUES (@exercise_name, @category)
                    RETURNING exercise_type_id;";

                using var insertCmd = new NpgsqlCommand(insertSql, conn);
                insertCmd.Parameters.Add("exercise_name", NpgsqlDbType.Varchar).Value = normalizedName;
                insertCmd.Parameters.Add("category", NpgsqlDbType.Varchar).Value = "Other";

                try
                {
                    return Convert.ToInt64(insertCmd.ExecuteScalar());
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    using var retryCmd = new NpgsqlCommand(findSql, conn);
                    retryCmd.Parameters.Add("exercise_name", NpgsqlDbType.Varchar).Value = normalizedName;
                    var existing = retryCmd.ExecuteScalar();

                    if (existing is not null && existing != DBNull.Value)
                        return Convert.ToInt64(existing);

                    throw;
                }
            }

            public List<ExerciseSession> GetHistory(long personId)
            {
                var list = new List<ExerciseSession>();

                using var conn = DbConnectionFactory.Create();
                conn.Open();

                const string sql = @"
                    SELECT
                        es.exercise_session_id,
                        es.exercise_type_id,
                        COALESCE(et.exercise_name, 'Exercise') AS exercise_name,
                        es.start_time,
                        es.duration_minutes,
                        es.calories_expended,
                        es.intensity,
                        es.notes,
                        es.created_at,
                        es.updated_at
                    FROM public.exercise_session es
                    LEFT JOIN public.exercise_type et
                        ON et.exercise_type_id = es.exercise_type_id
                    WHERE es.person_id = @p_person_id
                    ORDER BY es.start_time DESC, es.exercise_session_id DESC;";

                using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("p_person_id", personId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new ExerciseSession
                    {
                        ExerciseSessionId = reader.GetInt64(0),
                        ExerciseTypeId = reader.GetInt64(1),
                        ExerciseName = reader.GetString(2),
                        StartTime = reader.GetDateTime(3),
                        DurationMinutes = reader.GetDecimal(4),
                        CaloriesExpended = !reader.IsDBNull(5)
                            ? reader.GetDecimal(5)
                            : null,
                        Intensity = reader.GetString(6),
                        Notes = reader.IsDBNull(7)
                            ? null
                            : reader.GetString(7),
                        CreatedAt = reader.GetDateTime(8),
                        UpdatedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
                    });
                }

                return list;
            }

            public void UpdateExerciseSession(
                long exerciseSessionId,
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

                const string sql = @"
                    UPDATE public.exercise_session
                    SET person_id = @person_id,
                        exercise_type_id = @exercise_type_id,
                        start_time = @start_time,
                        duration_minutes = @duration_minutes,
                        calories_expended = @calories_expended,
                        intensity = @intensity,
                        notes = @notes,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE exercise_session_id = @exercise_session_id;";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("exercise_session_id", exerciseSessionId);
                cmd.Parameters.AddWithValue("person_id", personId);
                cmd.Parameters.AddWithValue("exercise_type_id", exerciseTypeId);
                cmd.Parameters.AddWithValue("start_time", startTime);
                cmd.Parameters.AddWithValue("duration_minutes", durationMinutes);
                cmd.Parameters.Add("calories_expended", NpgsqlDbType.Numeric).Value =
                    (object?)caloriesExpended ?? DBNull.Value;
                cmd.Parameters.Add("intensity", NpgsqlDbType.Varchar).Value = intensity;
                cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("entered_by", enteredBy);

                cmd.ExecuteNonQuery();
            }

            public bool UpdateExerciseSessionForPerson(
                long exerciseSessionId,
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

                const string sql = @"
                    UPDATE public.exercise_session
                    SET exercise_type_id = @exercise_type_id,
                        start_time = @start_time,
                        duration_minutes = @duration_minutes,
                        calories_expended = @calories_expended,
                        intensity = @intensity,
                        notes = @notes,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE exercise_session_id = @exercise_session_id
                      AND person_id = @person_id;";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("exercise_session_id", exerciseSessionId);
                cmd.Parameters.AddWithValue("person_id", personId);
                cmd.Parameters.AddWithValue("exercise_type_id", exerciseTypeId);
                cmd.Parameters.AddWithValue("start_time", startTime);
                cmd.Parameters.AddWithValue("duration_minutes", durationMinutes);
                cmd.Parameters.Add("calories_expended", NpgsqlDbType.Numeric).Value =
                    (object?)caloriesExpended ?? DBNull.Value;
                cmd.Parameters.Add("intensity", NpgsqlDbType.Varchar).Value = intensity;
                cmd.Parameters.AddWithValue("notes", (object?)notes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("entered_by", enteredBy);

                return cmd.ExecuteNonQuery() == 1;
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

            public bool DeleteExerciseSessionForPerson(long personId, long exerciseSessionId)
            {
                using var conn = DbConnectionFactory.Create();
                conn.Open();

                const string sql = @"
                    DELETE FROM public.exercise_session
                    WHERE exercise_session_id = @exercise_session_id
                      AND person_id = @person_id;";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("person_id", personId);
                cmd.Parameters.AddWithValue("exercise_session_id", exerciseSessionId);

                return cmd.ExecuteNonQuery() == 1;
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

            private static string NormalizeExerciseName(string exerciseName)
            {
                return string.Join(
                    ' ',
                    (exerciseName ?? string.Empty)
                        .Trim()
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries));
            }

        }
    }

}
