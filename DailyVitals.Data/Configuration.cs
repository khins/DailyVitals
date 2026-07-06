using System.Configuration;
using Npgsql;

namespace DailyVitals.Data.Configuration
{
    public static class DbConnectionFactory
    {
        private const string DeploymentConnectionStringVariable = "DAILYVITALS_CONNECTION_STRING";
        private const string AspNetConnectionStringVariable = "ConnectionStrings__DailyVitals";
        private const string MigrationConnectionStringVariable = "DAILYVITALS_MIGRATION_CONNECTION_STRING";
        private const string AspNetMigrationConnectionStringVariable = "ConnectionStrings__DailyVitalsMigrations";
        private static string? _configuredConnectionString;
        private static string? _configuredMigrationConnectionString;

        public static void Configure(string? connectionString, string? migrationConnectionString = null)
        {
            _configuredConnectionString = string.IsNullOrWhiteSpace(connectionString)
                ? null
                : connectionString;
            _configuredMigrationConnectionString = string.IsNullOrWhiteSpace(migrationConnectionString)
                ? null
                : migrationConnectionString;
        }

        public static NpgsqlConnection Create()
        {
            var connectionString =
                Environment.GetEnvironmentVariable(DeploymentConnectionStringVariable)
                ?? _configuredConnectionString
                ?? Environment.GetEnvironmentVariable(AspNetConnectionStringVariable)
                ?? ConfigurationManager
                    .ConnectionStrings["DailyVitals"]
                    ?.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ConfigurationErrorsException(
                    $"Connection string 'DailyVitals' not found. Set {DeploymentConnectionStringVariable}, " +
                    $"{AspNetConnectionStringVariable}, ASP.NET configuration, or a desktop App.config."
                );

            return new NpgsqlConnection(connectionString);
        }

        public static NpgsqlConnection CreateMigration()
        {
            var connectionString =
                Environment.GetEnvironmentVariable(MigrationConnectionStringVariable)
                ?? _configuredMigrationConnectionString
                ?? Environment.GetEnvironmentVariable(AspNetMigrationConnectionStringVariable)
                ?? ConfigurationManager
                    .ConnectionStrings["DailyVitalsMigrations"]
                    ?.ConnectionString;

            return string.IsNullOrWhiteSpace(connectionString)
                ? Create()
                : new NpgsqlConnection(connectionString);
        }

        public static async Task ValidateRuntimeSecurityAsync(
            CancellationToken cancellationToken = default)
        {
            await using var connection = Create();
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                """
                SELECT r.rolsuper, s.ssl
                FROM pg_roles r
                CROSS JOIN pg_stat_ssl s
                WHERE r.rolname = current_user
                  AND s.pid = pg_backend_pid()
                """,
                connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                throw new ConfigurationErrorsException(
                    "Could not verify the PostgreSQL runtime connection security.");
            if (reader.GetBoolean(0))
                throw new ConfigurationErrorsException(
                    "The PostgreSQL runtime connection must not use a superuser role.");
            if (!reader.GetBoolean(1))
                throw new ConfigurationErrorsException(
                    "The PostgreSQL runtime connection must use TLS.");
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



