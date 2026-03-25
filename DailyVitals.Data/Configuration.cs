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
                    "Connection string 'DailyVitals' not found."
                );

            return new NpgsqlConnection(connectionString);
        }

        public static void TestConnection()
        {
            using var conn = Create();
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT 1", conn);
            var result = cmd.ExecuteScalar();

            if (result is not int scalar || scalar != 1)
                throw new Exception("Unexpected test query result");

            using var cmd2 = new NpgsqlCommand(
                    "SELECT COUNT(*) FROM person",
                    conn
);

            var count = cmd2.ExecuteScalar();

            if (count is not long)
                throw new Exception("Unexpected person count result.");

        }


    }
}



