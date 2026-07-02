using System.Collections.Generic;
using Npgsql;
using DailyVitals.Domain.Models;
using DailyVitals.Data.Configuration;
using System;

namespace DailyVitals.Data.Services
{
    public class PersonService
    {
        public List<Person> GetAllPersons()
        {
            var persons = new List<Person>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT
                    person_id,
                    first_name,
                    last_name,
                    height_ft,
                    birth_date,
                    gender,
                    is_diabetic,
                    glucose_target_mg_dl,
                    track_kidney_labs,
                    track_weight_loss,
                    created_at,
                    updated_at
                FROM person
                ORDER BY last_name, first_name;
            ";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                persons.Add(new Person
                {
                    PersonId = reader.GetInt64(0),
                    FirstName = reader.GetString(1),
                    LastName = reader.GetString(2),
                    HeightFt = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                    BirthDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    Gender = reader.IsDBNull(5) ? null : reader.GetString(5),
                    IsDiabetic = reader.GetBoolean(6),
                    GlucoseTargetMgDl = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    TrackKidneyLabs = reader.GetBoolean(8),
                    TrackWeightLoss = reader.GetBoolean(9),
                    CreatedAt = reader.GetDateTime(10),
                    UpdatedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11)
                });
            }

            return persons;
        }

        public List<Person> GetPeople()
        {
            return GetAllPersons();
        }

        public Person? GetPersonById(long personId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT
                    person_id,
                    first_name,
                    last_name,
                    height_ft,
                    birth_date,
                    gender,
                    is_diabetic,
                    glucose_target_mg_dl,
                    track_kidney_labs,
                    track_weight_loss,
                    created_at,
                    updated_at
                FROM public.person
                WHERE person_id = @person_id
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new Person
            {
                PersonId = reader.GetInt64(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                HeightFt = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                BirthDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                Gender = reader.IsDBNull(5) ? null : reader.GetString(5),
                IsDiabetic = reader.GetBoolean(6),
                GlucoseTargetMgDl = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                TrackKidneyLabs = reader.GetBoolean(8),
                TrackWeightLoss = reader.GetBoolean(9),
                CreatedAt = reader.GetDateTime(10),
                UpdatedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11)
            };
        }

        public bool PersonExists(
            string firstName,
            string lastName,
            DateTime birthDate,
            decimal heightFt)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            return PersonExists(conn, firstName, lastName, birthDate, heightFt);
        }

        public bool PersonExists(
            string firstName,
            string lastName,
            DateTime birthDate)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM public.person
                    WHERE lower(TRIM(first_name)) = lower(TRIM(@first_name))
                      AND lower(TRIM(last_name)) = lower(TRIM(@last_name))
                      AND birth_date = @birth_date
                );";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("first_name", firstName);
            cmd.Parameters.AddWithValue("last_name", lastName);
            cmd.Parameters.AddWithValue("birth_date", birthDate.Date);

            return cmd.ExecuteScalar() is true;
        }

        private static bool PersonExists(
            NpgsqlConnection conn,
            string firstName,
            string lastName,
            DateTime birthDate,
            decimal heightFt)
        {
            const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM public.person
                    WHERE lower(TRIM(first_name)) = lower(TRIM(@first_name))
                      AND lower(TRIM(last_name)) = lower(TRIM(@last_name))
                      AND birth_date = @birth_date
                      AND ROUND(height_ft, 2) = ROUND(@height_ft, 2)
                );";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("first_name", firstName);
            cmd.Parameters.AddWithValue("last_name", lastName);
            cmd.Parameters.AddWithValue("birth_date", birthDate.Date);
            cmd.Parameters.AddWithValue("height_ft", Math.Round(heightFt, 2));

            return cmd.ExecuteScalar() is true;
        }

        public long InsertPerson(
            string firstName,
            string lastName,
            decimal? heightFt,
            DateTime? birthDate,
            string? gender)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            if (birthDate.HasValue &&
                heightFt.HasValue &&
                PersonExists(conn, firstName, lastName, birthDate.Value, heightFt.Value))
            {
                throw new InvalidOperationException(
                    "A person with this name, birth date, and height already exists.");
            }

            const string sql = @"
                INSERT INTO public.person (
                    first_name,
                    last_name,
                    height_ft,
                    birth_date,
                    gender,
                    is_diabetic,
                    track_kidney_labs,
                    track_weight_loss
                )
                VALUES (
                    TRIM(@first_name),
                    TRIM(@last_name),
                    @height_ft,
                    @birth_date,
                    NULLIF(TRIM(@gender), ''),
                    false,
                    false,
                    false
                )
                RETURNING person_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("first_name", firstName);
            cmd.Parameters.AddWithValue("last_name", lastName);
            cmd.Parameters.AddWithValue("height_ft", (object?)heightFt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("birth_date", (object?)birthDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("gender", (object?)gender ?? DBNull.Value);

            var result = cmd.ExecuteScalar();
            if (result is null or DBNull)
                throw new Exception("Person insert failed. No ID returned.");

            return Convert.ToInt64(result);
        }

        public void UpdatePerson(
            long personId,
            string firstName,
            string lastName,
            decimal? heightFt,
            DateTime? birthDate,
            string? gender,
            bool isDiabetic = false,
            int? glucoseTargetMgDl = null,
            bool trackKidneyLabs = false,
            bool trackWeightLoss = false)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            const string sql = @"
                UPDATE public.person
                SET first_name = TRIM(@first_name),
                    last_name = TRIM(@last_name),
                    height_ft = @height_ft,
                    birth_date = @birth_date,
                    gender = NULLIF(TRIM(@gender), ''),
                    is_diabetic = @is_diabetic,
                    glucose_target_mg_dl = @glucose_target_mg_dl,
                    track_kidney_labs = @track_kidney_labs,
                    track_weight_loss = @track_weight_loss,
                    updated_at = CURRENT_TIMESTAMP
                WHERE person_id = @person_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("first_name", firstName);
            cmd.Parameters.AddWithValue("last_name", lastName);
            cmd.Parameters.AddWithValue("height_ft", (object?)heightFt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("birth_date", (object?)birthDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("gender", (object?)gender ?? DBNull.Value);
            cmd.Parameters.AddWithValue("is_diabetic", isDiabetic);
            cmd.Parameters.AddWithValue("glucose_target_mg_dl", (object?)glucoseTargetMgDl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("track_kidney_labs", trackKidneyLabs);
            cmd.Parameters.AddWithValue("track_weight_loss", trackWeightLoss);
            cmd.ExecuteNonQuery();
        }

        public void DeletePerson(long personId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();

            var blockers = GetPersonDeleteBlockers(conn, personId);
            if (blockers.Count > 0)
            {
                throw new InvalidOperationException(
                    "This person cannot be deleted because related records exist: " +
                    string.Join(", ", blockers) + ".");
            }

            const string sql = "DELETE FROM public.person WHERE person_id = @person_id;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.ExecuteNonQuery();
        }

        private static List<string> GetPersonDeleteBlockers(NpgsqlConnection conn, long personId)
        {
            var blockers = new List<string>();
            var tables = new (string TableName, string Label)[]
            {
                ("blood_pressure", "blood pressure"),
                ("blood_glucose", "blood glucose"),
                ("weight", "weight"),
                ("kidney_lab_result", "kidney lab results"),
                ("food_phosphorus_intake", "food phosphorus entries"),
                ("food_phosphorus_food", "food notes"),
                ("exercise_session", "exercise sessions"),
                ("medication", "medications"),
                ("nutrition_goal", "nutrition goals"),
                ("renal_diet_food", "renal diet foods")
            };

            foreach (var (tableName, label) in tables)
            {
                if (!TableExists(conn, tableName))
                    continue;

                using var cmd = new NpgsqlCommand(
                    $"SELECT EXISTS (SELECT 1 FROM public.{tableName} WHERE person_id = @person_id);",
                    conn);
                cmd.Parameters.AddWithValue("person_id", personId);

                if (cmd.ExecuteScalar() is true)
                    blockers.Add(label);
            }

            return blockers;
        }

        private static bool TableExists(NpgsqlConnection conn, string tableName)
        {
            const string sql = "SELECT to_regclass(@table_name) IS NOT NULL;";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("table_name", $"public.{tableName}");
            return cmd.ExecuteScalar() is true;
        }

    }
}
