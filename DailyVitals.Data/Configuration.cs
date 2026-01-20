using System.Configuration;
using Npgsql;

namespace DailyVitals.Data.Configuration
{
    public static class DbConnectionFactory
    {
        public static NpgsqlConnection Create()
        {
            var connectionString =
                ConfigurationManager
                    .ConnectionStrings["DailyVitals"]
                    ?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ConfigurationErrorsException(
                    "Connection string 'DailyVitalsDb' not found."
                );

            return new NpgsqlConnection(connectionString);
        }

        public static void TestConnection()
        {
            using var conn = Create();
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT 1", conn);
            var result = cmd.ExecuteScalar();

            if ((int)result != 1)
                throw new Exception("Unexpected test query result");

            using var cmd2 = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM person",
                    conn
);

            var count = (long)cmd2.ExecuteScalar();

        }


    }
}



