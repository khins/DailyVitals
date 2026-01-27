using System.Collections.Generic;
using Npgsql;
using DailyVitals.Domain.Models;
using DailyVitals.Data.Configuration;

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
                SELECT person_id, first_name, last_name
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
                    LastName = reader.GetString(2)
                });
            }

            return persons;
        }

        public List<Person> GetPeople()
        {
            return GetAllPersons();
        }

    }
}
