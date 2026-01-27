using DailyVitals.Domain.Models;
using global::DailyVitals.Data.Configuration;
using Npgsql;
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
                int durationMinutes,
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
                cmd.Parameters.AddWithValue("p_intensity", intensity);
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

                const string sql = """
                SELECT es.exercise_session_id,
                       es.exercise_type_id,
                       et.exercise_name,
                       es.start_time,
                       es.duration_minutes,
                       es.intensity,
                       es.notes
                FROM exercise_session es
                JOIN exercise_type et ON et.exercise_type_id = es.exercise_type_id
                WHERE es.person_id = @person_id
                ORDER BY es.start_time DESC;
            """;

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("person_id", personId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new ExerciseSession
                    {
                        ExerciseSessionId = reader.GetInt64(0),
                        ExerciseTypeId = reader.GetInt64(1),
                        ExerciseName = reader.GetString(2),
                        StartTime = reader.GetDateTime(3),
                        DurationMinutes = reader.GetInt32(4),
                        Intensity = reader.GetString(5),
                        Notes = reader.IsDBNull(6) ? null : reader.GetString(6)
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

        }
    }

}
