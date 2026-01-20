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
        }

    }
}



