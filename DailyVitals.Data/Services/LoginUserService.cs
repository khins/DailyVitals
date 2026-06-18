using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace DailyVitals.Data.Services
{
    public class LoginUserService
    {
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;
        private const int Iterations = 210_000;
        private const string Algorithm = "PBKDF2-SHA256";

        public LoginUser? ValidateCredentials(string? userName, string? password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureLoginUserTable(conn);

            const string sql = @"
                SELECT
                    login_user_id,
                    user_name,
                    password_hash,
                    password_salt,
                    password_iterations,
                    password_algorithm,
                    is_active,
                    created_at,
                    updated_at,
                    last_login_at
                FROM public.login_user
                WHERE lower(user_name) = lower(@user_name)
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_name", userName.Trim());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var algorithm = reader.GetString(5);
            var isActive = reader.GetBoolean(6);
            if (!isActive)
                return null;

            var passwordHash = reader.GetString(2);
            var passwordSalt = reader.GetString(3);
            var iterations = reader.GetInt32(4);

            if (!string.Equals(algorithm, Algorithm, StringComparison.OrdinalIgnoreCase))
                return null;

            if (!VerifyPassword(password, passwordSalt, passwordHash, iterations))
                return null;

            var loginUser = new LoginUser
            {
                LoginUserId = reader.GetInt64(0),
                UserName = reader.GetString(1),
                IsActive = isActive,
                CreatedAt = reader.GetDateTime(7),
                UpdatedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                LastLoginAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
            };

            reader.Close();
            RecordSuccessfulLogin(conn, loginUser.LoginUserId);

            return loginUser;
        }

        public void UpsertLoginUser(string userName, string password, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(userName))
                throw new InvalidOperationException("User name is required.");

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Password is required.");

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureLoginUserTable(conn);

            if (TryGetLoginUserId(conn, userName.Trim(), out var loginUserId))
            {
                UpdateLoginUser(conn, loginUserId, password, isActive);
                return;
            }

            InsertLoginUser(conn, userName, password, isActive);
        }

        public void EnsureLoginUserExists(string userName, string password, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return;

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureLoginUserTable(conn);

            if (TryGetLoginUserId(conn, userName.Trim(), out _))
                return;

            InsertLoginUser(conn, userName, password, isActive);
        }

        public List<LoginUser> GetLoginUsers()
        {
            var users = new List<LoginUser>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureLoginUserTable(conn);

            const string sql = @"
                SELECT login_user_id, user_name, is_active, created_at, updated_at, last_login_at
                FROM public.login_user
                ORDER BY user_name;";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new LoginUser
                {
                    LoginUserId = reader.GetInt64(0),
                    UserName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2),
                    CreatedAt = reader.GetDateTime(3),
                    UpdatedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    LastLoginAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5)
                });
            }

            return users;
        }

        public bool HasAnyLoginUsers()
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureLoginUserTable(conn);

            const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM public.login_user
                    WHERE is_active = true
                );";

            using var cmd = new NpgsqlCommand(sql, conn);
            return cmd.ExecuteScalar() is bool hasUsers && hasUsers;
        }

        private static void InsertLoginUser(NpgsqlConnection conn, string userName, string password, bool isActive)
        {
            var passwordSalt = GenerateSalt();
            var passwordHash = HashPassword(password, passwordSalt, Iterations);

            const string sql = @"
                INSERT INTO public.login_user (
                    user_name,
                    password_hash,
                    password_salt,
                    password_iterations,
                    password_algorithm,
                    is_active
                )
                VALUES (
                    TRIM(@user_name),
                    @password_hash,
                    @password_salt,
                    @password_iterations,
                    @password_algorithm,
                    @is_active
                );";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_name", userName.Trim());
            cmd.Parameters.AddWithValue("password_hash", passwordHash);
            cmd.Parameters.AddWithValue("password_salt", passwordSalt);
            cmd.Parameters.AddWithValue("password_iterations", Iterations);
            cmd.Parameters.AddWithValue("password_algorithm", Algorithm);
            cmd.Parameters.AddWithValue("is_active", isActive);
            cmd.ExecuteNonQuery();
        }

        private static void UpdateLoginUser(NpgsqlConnection conn, long loginUserId, string password, bool isActive)
        {
            var passwordSalt = GenerateSalt();
            var passwordHash = HashPassword(password, passwordSalt, Iterations);

            const string sql = @"
                UPDATE public.login_user
                SET
                    password_hash = @password_hash,
                    password_salt = @password_salt,
                    password_iterations = @password_iterations,
                    password_algorithm = @password_algorithm,
                    is_active = @is_active,
                    updated_at = CURRENT_TIMESTAMP
                WHERE login_user_id = @login_user_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("login_user_id", loginUserId);
            cmd.Parameters.AddWithValue("password_hash", passwordHash);
            cmd.Parameters.AddWithValue("password_salt", passwordSalt);
            cmd.Parameters.AddWithValue("password_iterations", Iterations);
            cmd.Parameters.AddWithValue("password_algorithm", Algorithm);
            cmd.Parameters.AddWithValue("is_active", isActive);
            cmd.ExecuteNonQuery();
        }

        private static bool TryGetLoginUserId(NpgsqlConnection conn, string userName, out long loginUserId)
        {
            const string sql = @"
                SELECT login_user_id
                FROM public.login_user
                WHERE lower(user_name) = lower(@user_name)
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_name", userName.Trim());

            var result = cmd.ExecuteScalar();
            if (result is long id)
            {
                loginUserId = id;
                return true;
            }

            loginUserId = 0;
            return false;
        }

        private static void EnsureLoginUserTable(NpgsqlConnection conn)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS public.login_user (
                    login_user_id bigserial NOT NULL,
                    user_name varchar(100) NOT NULL,
                    password_hash text NOT NULL,
                    password_salt text NOT NULL,
                    password_iterations int4 NOT NULL,
                    password_algorithm varchar(50) NOT NULL,
                    is_active boolean NOT NULL DEFAULT true,
                    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    updated_at timestamp NULL,
                    last_login_at timestamp NULL,
                    CONSTRAINT login_user_pkey PRIMARY KEY (login_user_id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS login_user_user_name_lower_key
                    ON public.login_user (lower(user_name));

                ALTER TABLE public.login_user
                    ADD COLUMN IF NOT EXISTS password_algorithm varchar(50) NOT NULL DEFAULT 'PBKDF2-SHA256',
                    ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true,
                    ADD COLUMN IF NOT EXISTS created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL,
                    ADD COLUMN IF NOT EXISTS last_login_at timestamp NULL;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private static void RecordSuccessfulLogin(NpgsqlConnection conn, long loginUserId)
        {
            const string sql = @"
                UPDATE public.login_user
                SET last_login_at = CURRENT_TIMESTAMP
                WHERE login_user_id = @login_user_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("login_user_id", loginUserId);
            cmd.ExecuteNonQuery();
        }

        private static string GenerateSalt()
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            return Convert.ToBase64String(salt);
        }

        private static string HashPassword(string password, string salt, int iterations)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                iterations,
                HashAlgorithmName.SHA256,
                HashSizeBytes);

            return Convert.ToBase64String(hashBytes);
        }

        private static bool VerifyPassword(string password, string salt, string expectedHash, int iterations)
        {
            var computedHash = HashPassword(password, salt, iterations);
            var computedBytes = Convert.FromBase64String(computedHash);
            var expectedBytes = Convert.FromBase64String(expectedHash);

            return computedBytes.Length == expectedBytes.Length &&
                CryptographicOperations.FixedTimeEquals(computedBytes, expectedBytes);
        }
    }
}
