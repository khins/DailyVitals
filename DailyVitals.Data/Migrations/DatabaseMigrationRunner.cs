using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DailyVitals.Data.Configuration;
using Npgsql;

namespace DailyVitals.Data.Migrations;

public sealed class DatabaseMigrationRunner
{
    private const string BaselineId = "0000-core-baseline";
    private const string MigrationTable = "dailyvitals_schema_migration";
    private static readonly Regex IncludeDirective = new(
        """^\s*\\i\s+['"]?([^'"\r\n]+)['"]?\s*$""",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);
    private static readonly Regex EarlyTrigger = new(
        @"^\s*create\s+trigger\s+.*?;\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly string _databaseScriptsPath;

    public DatabaseMigrationRunner(string? databaseScriptsPath = null)
    {
        _databaseScriptsPath = databaseScriptsPath
            ?? Path.Combine(AppContext.BaseDirectory, "Database");
    }

    public async Task<IReadOnlyList<string>> GetPendingMigrationIdsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = DbConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await EnsureMigrationTableAsync(connection, cancellationToken);

        var migrations = LoadMigrations();
        var applied = await LoadAppliedMigrationsAsync(connection, cancellationToken);
        ValidateAppliedChecksums(migrations, applied);
        return migrations
            .Where(migration => !applied.ContainsKey(migration.Id))
            .Select(migration => migration.Id)
            .ToArray();
    }

    public async Task<IReadOnlyList<string>> RunAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = DbConnectionFactory.Create();
        await connection.OpenAsync(cancellationToken);
        await AcquireLockAsync(connection, cancellationToken);

        try
        {
            await EnsureMigrationTableAsync(connection, cancellationToken);
            var migrations = LoadMigrations();
            var applied = await LoadAppliedMigrationsAsync(connection, cancellationToken);
            ValidateAppliedChecksums(migrations, applied);

            var appliedNow = new List<string>();
            foreach (var migration in migrations.Where(item => !applied.ContainsKey(item.Id)))
            {
                await ApplyAsync(connection, migration, cancellationToken);
                appliedNow.Add(migration.Id);
            }

            return appliedNow;
        }
        finally
        {
            await ReleaseLockAsync(connection, cancellationToken);
        }
    }

    private IReadOnlyList<Migration> LoadMigrations()
    {
        var baselinePath = Path.Combine(_databaseScriptsPath, "one complete DDL script.sql");
        if (!File.Exists(baselinePath))
            throw new FileNotFoundException("The database baseline SQL file was not deployed.", baselinePath);

        var migrationsPath = Path.Combine(_databaseScriptsPath, "migrations");
        if (!Directory.Exists(migrationsPath))
            throw new DirectoryNotFoundException($"The database migrations directory was not deployed: {migrationsPath}");

        var migrations = new List<Migration>
        {
            CreateBaselineMigration(baselinePath)
        };

        migrations.AddRange(Directory
            .EnumerateFiles(migrationsPath, "*.sql", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(Path.GetFileName(path), ".gitkeep", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Select(path => CreateMigration(Path.GetFileNameWithoutExtension(path), path)));

        var duplicate = migrations.GroupBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate database migration ID: {duplicate.Key}");

        return migrations;
    }

    private static Migration CreateBaselineMigration(string path)
    {
        var sql = File.ReadAllText(path);
        var start = sql.IndexOf("-- public.data_entry_log definition", StringComparison.Ordinal);
        if (start < 0)
            throw new InvalidOperationException("The core baseline marker was not found.");

        sql = EarlyTrigger.Replace(sql[start..], string.Empty);
        sql += """

            DROP TRIGGER IF EXISTS trg_medication_updated ON public.medication;
            CREATE TRIGGER trg_medication_updated
                BEFORE UPDATE ON public.medication
                FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

            DROP TRIGGER IF EXISTS trg_log_bp_insert ON public.blood_pressure;
            CREATE TRIGGER trg_log_bp_insert
                AFTER INSERT ON public.blood_pressure
                FOR EACH ROW EXECUTE FUNCTION public.log_blood_pressure_insert();

            DROP TRIGGER IF EXISTS trg_escalate_severity ON public.vital_alert;
            CREATE TRIGGER trg_escalate_severity
                AFTER INSERT ON public.vital_alert
                FOR EACH ROW EXECUTE FUNCTION public.evaluate_severity_escalation();
            """;

        return new Migration(BaselineId, path, sql, ComputeChecksum(sql), IsBaseline: true);
    }

    private static Migration CreateMigration(string id, string path)
    {
        var sql = ExpandIncludes(path, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        return new Migration(id, path, sql, ComputeChecksum(sql), IsBaseline: false);
    }

    private static string ExpandIncludes(string path, HashSet<string> includeStack)
    {
        path = Path.GetFullPath(path);
        if (!includeStack.Add(path))
            throw new InvalidOperationException($"Circular SQL include detected at {path}.");

        try
        {
            var sql = File.ReadAllText(path);
            return IncludeDirective.Replace(sql, match =>
            {
                var includePath = match.Groups[1].Value.Trim();
                var resolvedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, includePath));
                if (!File.Exists(resolvedPath))
                    throw new FileNotFoundException($"SQL include was not found for {path}.", resolvedPath);
                return ExpandIncludes(resolvedPath, includeStack);
            });
        }
        finally
        {
            includeStack.Remove(path);
        }
    }

    private static async Task ApplyAsync(
        NpgsqlConnection connection,
        Migration migration,
        CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (!migration.IsBaseline || !await HasExistingCoreSchemaAsync(connection, transaction, cancellationToken))
        {
            await using var migrationCommand = new NpgsqlCommand(migration.Sql, connection, transaction)
            {
                CommandTimeout = 300
            };
            await migrationCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        const string recordSql = $"""
            INSERT INTO public.{MigrationTable} (migration_id, checksum)
            VALUES (@migration_id, @checksum);
            """;
        await using var recordCommand = new NpgsqlCommand(recordSql, connection, transaction);
        recordCommand.Parameters.AddWithValue("migration_id", migration.Id);
        recordCommand.Parameters.AddWithValue("checksum", migration.Checksum);
        await recordCommand.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<bool> HasExistingCoreSchemaAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT to_regclass('public.person') IS NOT NULL;",
            connection,
            transaction);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    private static async Task EnsureMigrationTableAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = $"""
            CREATE TABLE IF NOT EXISTS public.{MigrationTable} (
                migration_id varchar(200) NOT NULL PRIMARY KEY,
                checksum char(64) NOT NULL,
                applied_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, string>> LoadAppliedMigrationsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = $"SELECT migration_id, checksum FROM public.{MigrationTable};";
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetString(0), reader.GetString(1).Trim());
        return result;
    }

    private static void ValidateAppliedChecksums(
        IEnumerable<Migration> migrations,
        IReadOnlyDictionary<string, string> applied)
    {
        foreach (var migration in migrations)
        {
            if (applied.TryGetValue(migration.Id, out var checksum) &&
                !string.Equals(checksum, migration.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Applied migration '{migration.Id}' no longer matches its deployed SQL. " +
                    "Create a new migration instead of editing an applied migration.");
            }
        }
    }

    private static string ComputeChecksum(string sql) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql))).ToLowerInvariant();

    private static async Task AcquireLockAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_advisory_lock(hashtext('dailyvitals.schema_migrations'));",
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ReleaseLockAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_advisory_unlock(hashtext('dailyvitals.schema_migrations'));",
            connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record Migration(
        string Id,
        string SourcePath,
        string Sql,
        string Checksum,
        bool IsBaseline);
}
