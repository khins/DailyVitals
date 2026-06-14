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
                SELECT person_id, first_name, last_name, height_ft
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
                    HeightFt = reader.IsDBNull(3) ? null : reader.GetDecimal(3)
                });
            }

            return persons;
        }

        public List<Person> GetPeople()
        {
            return GetAllPersons();
        }

        public long InsertPerson(string firstName, string lastName, decimal? heightFt)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsurePersonColumns(conn);

            const string sql = @"
                INSERT INTO public.person (
                    first_name,
                    last_name,
                    height_ft
                )
                VALUES (
                    TRIM(@first_name),
                    TRIM(@last_name),
                    @height_ft
                )
                RETURNING person_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("first_name", firstName);
            cmd.Parameters.AddWithValue("last_name", lastName);
            cmd.Parameters.AddWithValue("height_ft", (object?)heightFt ?? DBNull.Value);

            var result = cmd.ExecuteScalar();
            if (result is null or DBNull)
                throw new Exception("Person insert failed. No ID returned.");

            return Convert.ToInt64(result);
        }

        public void UpdatePerson(long personId, string firstName, string lastName, decimal? heightFt)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsurePersonColumns(conn);

            const string sql = @"
                UPDATE public.person
                SET first_name = TRIM(@first_name),
                    last_name = TRIM(@last_name),
                    height_ft = @height_ft
                WHERE person_id = @person_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("first_name", firstName);
            cmd.Parameters.AddWithValue("last_name", lastName);
            cmd.Parameters.AddWithValue("height_ft", (object?)heightFt ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        private static void EnsurePersonColumns(NpgsqlConnection conn)
        {
            const string sql = @"
                ALTER TABLE public.person
                    ADD COLUMN IF NOT EXISTS height_ft numeric(5, 2) NULL;

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
                  AND p.height_ft IS NULL;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

    }
}
