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
                    person_id,
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

            var algorithm = reader.GetString(6);
            var isActive = reader.GetBoolean(7);
            if (!isActive)
                return null;

            var passwordHash = reader.GetString(3);
            var passwordSalt = reader.GetString(4);
            var iterations = reader.GetInt32(5);

            if (!string.Equals(algorithm, Algorithm, StringComparison.OrdinalIgnoreCase))
                return null;

            if (!VerifyPassword(password, passwordSalt, passwordHash, iterations))
                return null;

            var loginUser = new LoginUser
            {
                LoginUserId = reader.GetInt64(0),
                PersonId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                UserName = reader.GetString(2),
                IsActive = isActive,
                CreatedAt = reader.GetDateTime(8),
                UpdatedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                LastLoginAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10)
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

            if (TryGetLoginUserId(conn, userName.Trim(), out var existingLoginUserId))
            {
                EnsurePersonLink(conn, existingLoginUserId, userName);
                return;
            }

            InsertLoginUser(conn, userName, password, isActive);
        }

        public LoginUser CreateLoginUser(long personId, string userName, string password, bool isActive = true)
        {
            if (personId <= 0)
                throw new InvalidOperationException("Person is required.");

            if (string.IsNullOrWhiteSpace(userName))
                throw new InvalidOperationException("User name is required.");

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Password is required.");

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureLoginUserTable(conn);

            if (TryGetLoginUserId(conn, userName.Trim(), out _))
                throw new InvalidOperationException("That user name is already in use.");

            var loginUserId = InsertLoginUser(conn, personId, userName, password, isActive);

            return new LoginUser
            {
                LoginUserId = loginUserId,
                PersonId = personId,
                UserName = userName.Trim(),
                IsActive = isActive,
                CreatedAt = DateTime.Now
            };
        }

        public bool UserNameExists(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return false;

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureLoginUserTable(conn);

            return TryGetLoginUserId(conn, userName.Trim(), out _);
        }

        public bool ResetPassword(
            string userName,
            string firstName,
            string lastName,
            DateTime birthDate,
            string newPassword)
        {
            return TryResetPassword(
                userName,
                firstName,
                lastName,
                birthDate,
                newPassword,
                out _);
        }

        public bool TryResetPassword(
            string userName,
            string firstName,
            string lastName,
            DateTime birthDate,
            string newPassword,
            out string failureReason)
        {
            if (string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                failureReason = "Enter username, first name, last name, birth date, and a new password.";
                return false;
            }

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureLoginUserTable(conn);

            const string sql = @"
                SELECT lu.login_user_id, lu.person_id, p.person_id
                FROM public.login_user lu
                LEFT JOIN public.person p ON p.person_id = lu.person_id
                WHERE lower(lu.user_name) = lower(@user_name)
                  AND lu.is_active = true
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_name", userName.Trim());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                failureReason = "Username was not found.";
                return false;
            }

            var loginUserId = reader.GetInt64(0);
            if (reader.IsDBNull(1) || reader.IsDBNull(2))
            {
                failureReason = "This login is not linked to a person record.";
                return false;
            }

            reader.Close();

            if (!VerifyPersonForPasswordReset(conn, loginUserId, firstName, lastName, birthDate))
            {
                failureReason = "Person details did not match this login.";
                return false;
            }

            UpdateLoginUser(conn, loginUserId, newPassword, true);
            failureReason = string.Empty;
            return true;
        }

        public List<LoginUser> GetLoginUsers()
        {
            var users = new List<LoginUser>();

            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureLoginUserTable(conn);

            const string sql = @"
                SELECT login_user_id, person_id, user_name, is_active, created_at, updated_at, last_login_at
                FROM public.login_user
                ORDER BY user_name;";

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new LoginUser
                {
                    LoginUserId = reader.GetInt64(0),
                    PersonId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                    UserName = reader.GetString(2),
                    IsActive = reader.GetBoolean(3),
                    CreatedAt = reader.GetDateTime(4),
                    UpdatedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                    LastLoginAt = reader.IsDBNull(6) ? null : reader.GetDateTime(6)
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

        private static long InsertLoginUser(NpgsqlConnection conn, long? personId, string userName, string password, bool isActive)
        {
            var passwordSalt = GenerateSalt();
            var passwordHash = HashPassword(password, passwordSalt, Iterations);

            const string sql = @"
                INSERT INTO public.login_user (
                    user_name,
                    person_id,
                    password_hash,
                    password_salt,
                    password_iterations,
                    password_algorithm,
                    is_active
                )
                VALUES (
                    TRIM(@user_name),
                    @person_id,
                    @password_hash,
                    @password_salt,
                    @password_iterations,
                    @password_algorithm,
                    @is_active
                )
                RETURNING login_user_id;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_name", userName.Trim());
            cmd.Parameters.AddWithValue("person_id", (object?)personId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("password_hash", passwordHash);
            cmd.Parameters.AddWithValue("password_salt", passwordSalt);
            cmd.Parameters.AddWithValue("password_iterations", Iterations);
            cmd.Parameters.AddWithValue("password_algorithm", Algorithm);
            cmd.Parameters.AddWithValue("is_active", isActive);

            var result = cmd.ExecuteScalar();
            if (result is null or DBNull)
                throw new Exception("Login user insert failed. No ID returned.");

            return Convert.ToInt64(result);
        }

        private static long InsertLoginUser(NpgsqlConnection conn, string userName, string password, bool isActive)
        {
            return InsertLoginUser(conn, ResolvePersonId(conn, userName), userName, password, isActive);
        }

        private static bool VerifyPersonForPasswordReset(
            NpgsqlConnection conn,
            long loginUserId,
            string firstName,
            string lastName,
            DateTime birthDate)
        {
            const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM public.login_user lu
                    JOIN public.person p ON p.person_id = lu.person_id
                    WHERE lu.login_user_id = @login_user_id
                      AND lower(TRIM(p.first_name)) = lower(TRIM(@first_name))
                      AND lower(TRIM(p.last_name)) = lower(TRIM(@last_name))
                      AND p.birth_date::date = @birth_date
                );";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("login_user_id", loginUserId);
            cmd.Parameters.AddWithValue("first_name", firstName);
            cmd.Parameters.AddWithValue("last_name", lastName);
            cmd.Parameters.AddWithValue("birth_date", birthDate.Date);

            return cmd.ExecuteScalar() is true;
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

        private static void EnsurePersonLink(NpgsqlConnection conn, long loginUserId, string userName)
        {
            var personId = ResolvePersonId(conn, userName);
            if (!personId.HasValue)
                return;

            const string sql = @"
                UPDATE public.login_user
                SET person_id = @person_id,
                    updated_at = CURRENT_TIMESTAMP
                WHERE login_user_id = @login_user_id
                  AND person_id IS NULL;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("login_user_id", loginUserId);
            cmd.Parameters.AddWithValue("person_id", personId.Value);
            cmd.ExecuteNonQuery();
        }

        private static long? ResolvePersonId(NpgsqlConnection conn, string userName)
        {
            const string sql = @"
                SELECT person_id
                FROM public.person
                WHERE lower(first_name) = lower(@user_name)
                   OR lower(first_name || ' ' || last_name) = lower(@user_name)
                ORDER BY person_id
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user_name", userName.Trim());

            var result = cmd.ExecuteScalar();
            return result is long personId ? personId : null;
        }

        private static void EnsureLoginUserTable(NpgsqlConnection conn)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS public.login_user (
                    login_user_id bigserial NOT NULL,
                    person_id int8 NULL,
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
                    ADD COLUMN IF NOT EXISTS person_id int8 NULL,
                    ADD COLUMN IF NOT EXISTS password_algorithm varchar(50) NOT NULL DEFAULT 'PBKDF2-SHA256',
                    ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true,
                    ADD COLUMN IF NOT EXISTS created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ADD COLUMN IF NOT EXISTS updated_at timestamp NULL,
                    ADD COLUMN IF NOT EXISTS last_login_at timestamp NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint c
                        JOIN pg_class t ON t.oid = c.conrelid
                        JOIN pg_namespace n ON n.oid = t.relnamespace
                        WHERE n.nspname = 'public'
                          AND t.relname = 'login_user'
                          AND c.conname = 'login_user_person_id_fkey'
                    ) THEN
                        ALTER TABLE public.login_user
                            ADD CONSTRAINT login_user_person_id_fkey
                            FOREIGN KEY (person_id)
                            REFERENCES public.person(person_id)
                            ON DELETE RESTRICT
                            NOT VALID;
                    END IF;
                END $$;";

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
