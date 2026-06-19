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
            EnsurePersonColumns(conn);

            const string sql = @"
                SELECT
                    person_id,
                    first_name,
                    last_name,
                    height_ft,
                    birth_date,
                    gender,
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
                    CreatedAt = reader.GetDateTime(6),
                    UpdatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
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
            EnsurePersonColumns(conn);

            const string sql = @"
                SELECT
                    person_id,
                    first_name,
                    last_name,
                    height_ft,
                    birth_date,
                    gender,
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
                CreatedAt = reader.GetDateTime(6),
                UpdatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
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
            EnsurePersonColumns(conn);

            return PersonExists(conn, firstName, lastName, birthDate, heightFt);
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
            EnsurePersonColumns(conn);

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
                    gender
                )
                VALUES (
                    TRIM(@first_name),
                    TRIM(@last_name),
                    @height_ft,
                    @birth_date,
                    NULLIF(TRIM(@gender), '')
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
            string? gender)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsurePersonColumns(conn);

            const string sql = @"
                UPDATE public.person
                SET first_name = TRIM(@first_name),
                    last_name = TRIM(@last_name),
                    height_ft = @height_ft,
                    birth_date = @birth_date,
                    gender = NULLIF(TRIM(@gender), ''),
                    updated_at = CURRENT_TIMESTAMP
                WHERE person_id = @person_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("first_name", firstName);
            cmd.Parameters.AddWithValue("last_name", lastName);
            cmd.Parameters.AddWithValue("height_ft", (object?)heightFt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("birth_date", (object?)birthDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("gender", (object?)gender ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void DeletePerson(long personId)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsurePersonColumns(conn);

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

        private static void EnsurePersonColumns(NpgsqlConnection conn)
        {
            const string sql = @"
                ALTER TABLE public.person
                    ADD COLUMN IF NOT EXISTS height_ft numeric(5, 2) NULL,
                    ADD COLUMN IF NOT EXISTS birth_date date NULL,
                    ADD COLUMN IF NOT EXISTS gender text NULL,
                    ADD COLUMN IF NOT EXISTS created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL;

                UPDATE public.person p
                SET height_ft = latest.height_ft
                FROM (
                    SELECT DISTINCT ON (person_id)
                        person_id,
                        height_ft
                    FROM public.weight
                    WHERE height_ft IS NOT NULL
                    ORDER BY person_id, reading_time DESC, weight_id DESC
                ) latest
                WHERE p.person_id = latest.person_id
                  AND p.height_ft IS NULL;

                DO $$
                DECLARE
                    child_table text;
                    constraint_name text;
                BEGIN
                    FOREACH child_table IN ARRAY ARRAY[
                        'blood_pressure',
                        'blood_glucose',
                        'weight',
                        'kidney_lab_result',
                        'food_phosphorus_intake',
                        'food_phosphorus_food',
                        'exercise_session',
                        'medication',
                        'nutrition_goal',
                        'renal_diet_food'
                    ]
                    LOOP
                        constraint_name := child_table || '_person_id_fkey';

                        IF to_regclass('public.' || child_table) IS NOT NULL
                           AND NOT EXISTS (
                               SELECT 1
                               FROM pg_constraint c
                               JOIN pg_class t ON t.oid = c.conrelid
                               JOIN pg_namespace n ON n.oid = t.relnamespace
                               WHERE n.nspname = 'public'
                                 AND t.relname = child_table
                                 AND c.conname = constraint_name
                           )
                        THEN
                            EXECUTE format(
                                'ALTER TABLE public.%I ADD CONSTRAINT %I FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE RESTRICT NOT VALID',
                                child_table,
                                constraint_name
                            );
                        END IF;
                    END LOOP;
                END $$;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

    }
}
