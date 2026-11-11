using System.Security.Cryptography;
using System.Text;
using BProMax.Configuration;
using BProMax.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BProMax.Services;

/// <summary>
/// Authentication service with real credential verification backed by PostgreSQL.
/// Credentials are salted/hashed (PBKDF2-HMAC-SHA256) and compared in constant time.
/// Failed attempts and lockout state are persisted per username in the database.
/// </summary>
public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int DefaultIterations = 210000;

    private readonly IDatabaseService _db;
    private readonly ITimeLockService _timeLock;
    private readonly ILogger<AuthService> _logger;

    private static readonly Action<ILogger, Exception?> _logLoginEmptyUsername =
        LoggerMessage.Define(LogLevel.Warning, 1, "Login attempt with empty username");

    private static readonly Action<ILogger, string, Exception?> _logLoginEmptyPassword =
        LoggerMessage.Define<string>(LogLevel.Warning, 2, "Login attempt with empty password for user {Username}");

    private static readonly Action<ILogger, string, DateTimeOffset, Exception?> _logLoginLockedOut =
        LoggerMessage.Define<string, DateTimeOffset>(LogLevel.Warning, 3, "Login blocked: user {Username} is locked out until {LockedUntil}");

    private static readonly Action<ILogger, string, Exception?> _logLoginSuccess =
        LoggerMessage.Define<string>(LogLevel.Information, 4, "User {Username} authenticated successfully");

    private static readonly Action<ILogger, int, string, Exception?> _logLoginFailed =
        LoggerMessage.Define<int, string>(LogLevel.Warning, 5, "Failed login attempt {FailureCount} for user {Username}");

    private static readonly Action<ILogger, string, Exception?> _logLogout =
        LoggerMessage.Define<string>(LogLevel.Information, 6, "User {User} logged out");

    private static readonly Action<ILogger, string, Exception?> _logLoginOutsideHours =
        LoggerMessage.Define<string>(LogLevel.Warning, 7, "Login blocked: user {Username} attempted outside allowed hours");

    public bool IsAuthenticated { get; private set; }
    public string? CurrentUser { get; private set; }

    public AuthService(IDatabaseService db, ITimeLockService timeLock, ILogger<AuthService> logger)
    {
        _db = db;
        _timeLock = timeLock;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            _logLoginEmptyUsername(_logger, null);
            return new AuthResult(false, AuthFailureReason.EmptyInput);
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            _logLoginEmptyPassword(_logger, username, null);
            return new AuthResult(false, AuthFailureReason.EmptyInput);
        }

        var now = DateTimeOffset.UtcNow;

        // Look up user by case-insensitive username.
        var user = await GetUserByUsernameAsync(username);

        if (user == null)
        {
            // No such user: treat as generic invalid-credentials to avoid user enumeration.
            _logLoginFailed(_logger, 0, username, null);
            return new AuthResult(false, AuthFailureReason.InvalidCredentials);
        }

        // Enforce per-user lockout.
        if (user.IsLocked)
        {
            var lockedUntilOffset = new DateTimeOffset(user.LockedUntil!.Value);
            _logLoginLockedOut(_logger, user.Username, lockedUntilOffset, null);
            return new AuthResult(false, AuthFailureReason.LockedOut, lockedUntilOffset);
        }

        // Enforce time-lock access control.
        if (!_timeLock.IsWithinAllowedTime())
        {
            _logLoginOutsideHours(_logger, user.Username, null);
            return new AuthResult(false, AuthFailureReason.OutsideAllowedHours);
        }

        bool valid = VerifyPassword(password, user);

        if (valid)
        {
            // Reset failure counter on success.
            await ResetFailuresAsync(user.Id);
            IsAuthenticated = true;
            CurrentUser = user.Username;
            _logLoginSuccess(_logger, user.Username, null);
            return new AuthResult(true);
        }

        // Record failure and apply lockout when threshold is reached.
        await RecordFailureAsync(user.Id, user.FailedAttempts + 1, now);

        int nextCount = user.FailedAttempts + 1;
        if (nextCount >= MaxFailedAttempts)
        {
            var lockedUntil = now.Add(LockoutDuration);
            _logLoginLockedOut(_logger, user.Username, lockedUntil, null);
            return new AuthResult(false, AuthFailureReason.LockedOut, lockedUntil);
        }

        _logLoginFailed(_logger, nextCount, user.Username, null);
        return new AuthResult(false, AuthFailureReason.InvalidCredentials);
    }

    public Task LogoutAsync()
    {
        var previous = CurrentUser;
        IsAuthenticated = false;
        CurrentUser = null;
        if (previous is not null)
        {
            _logLogout(_logger, previous, null);
        }
        return Task.CompletedTask;
    }

    public async Task<int?> GetCurrentUserIdAsync()
    {
        if (!IsAuthenticated || CurrentUser is null)
        {
            return null;
        }

        const string sql = """
SELECT id
FROM users
WHERE LOWER(username) = LOWER(@username);
""";

        var id = await _db.QuerySingleAsync(
            sql,
            reader => reader.GetInt32(0),
            new NpgsqlParameter("@username", CurrentUser));

        return id;
    }

    /// <summary>
    /// Provisions a new user credential (PBKDF2-HMAC-SHA256 hashed).
    /// </summary>
    public async Task RegisterCredentialAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        var existing = await GetUserByUsernameAsync(username);
        if (existing != null)
        {
            throw new InvalidOperationException($"User '{username}' already exists.");
        }

        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = ComputeHash(password, salt, DefaultIterations);

        const string sql = @"
INSERT INTO users (username, password_hash, salt, iterations, failed_attempts, locked_until, created_at, updated_at)
VALUES (@username, @password_hash, @salt, @iterations, 0, NULL, NOW(), NOW())
RETURNING id";

        var newId = await _db.QuerySingleAsync(
            sql,
            r => r.GetInt32(0),
            new NpgsqlParameter("@username", username),
            new NpgsqlParameter("@password_hash", hash),
            new NpgsqlParameter("@salt", salt),
            new NpgsqlParameter("@iterations", DefaultIterations));

        _logLoginSuccess(_logger, username, null);
    }

    /// <summary>
    /// Returns true if at least one user account exists in the database.
    /// </summary>
    public async Task<bool> AnyUserExistsAsync()
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM users LIMIT 1);";
        var result = await _db.QuerySingleAsync(sql, r => r.GetBoolean(0));
        return result;
    }

    private async Task<User?> GetUserByUsernameAsync(string username)
    {
        const string sql = @"
SELECT id, username, password_hash, salt, iterations, failed_attempts, locked_until, created_at, updated_at
FROM users
WHERE LOWER(username) = LOWER(@username)
LIMIT 1;";

        return await _db.QuerySingleAsync(sql, MapUser,
            new NpgsqlParameter("@username", username));
    }

    private async Task ResetFailuresAsync(int userId)
    {
        const string sql = "UPDATE users SET failed_attempts = 0, locked_until = NULL, updated_at = NOW() WHERE id = @id;";
        await _db.ExecuteNonQueryAsync(sql, new NpgsqlParameter("@id", userId));
    }

    private async Task RecordFailureAsync(int userId, int failedAttempts, DateTimeOffset now)
    {
        DateTime? lockedUntil = null;
        if (failedAttempts >= MaxFailedAttempts)
        {
            lockedUntil = now.Add(LockoutDuration).UtcDateTime;
        }

        const string sql = @"
UPDATE users
SET failed_attempts = @failed_attempts, locked_until = @locked_until, updated_at = NOW()
WHERE id = @id;";

        await _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@id", userId),
            new NpgsqlParameter("@failed_attempts", failedAttempts),
            new NpgsqlParameter("@locked_until", (object?)lockedUntil ?? DBNull.Value));
    }

    private static bool VerifyPassword(string password, User user)
    {
        var hash = ComputeHash(password, user.Salt, user.Iterations);
        return CryptographicOperations.FixedTimeEquals(hash, user.PasswordHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt, int iterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }

    private static User MapUser(NpgsqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Username = reader.GetString(reader.GetOrdinal("username")),
            PasswordHash = reader.IsDBNull(reader.GetOrdinal("password_hash"))
                ? Array.Empty<byte>()
                : (byte[])reader.GetValue(reader.GetOrdinal("password_hash")),
            Salt = reader.IsDBNull(reader.GetOrdinal("salt"))
                ? Array.Empty<byte>()
                : (byte[])reader.GetValue(reader.GetOrdinal("salt")),
            Iterations = reader.GetInt32(reader.GetOrdinal("iterations")),
            FailedAttempts = reader.GetInt32(reader.GetOrdinal("failed_attempts")),
            LockedUntil = reader.IsDBNull(reader.GetOrdinal("locked_until"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("locked_until")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }
}
