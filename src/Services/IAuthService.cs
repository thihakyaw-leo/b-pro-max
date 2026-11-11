namespace BProMax.Services;

/// <summary>
/// Result of an authentication attempt.
/// </summary>
/// <param name="Success">Whether authentication succeeded.</param>
/// <param name="FailureReason">Reason for failure, if any.</param>
/// <param name="LockedUntil">Lockout expiration, if locked out.</param>
public sealed record AuthResult(
    bool Success,
    AuthFailureReason FailureReason = AuthFailureReason.None,
    DateTimeOffset? LockedUntil = null
);

/// <summary>
/// Reasons an authentication attempt can fail.
/// </summary>
public enum AuthFailureReason
{
    None = 0,
    EmptyInput = 1,
    InvalidCredentials = 2,
    LockedOut = 3,
    NotProvisioned = 4,
    OutsideAllowedHours = 5,
}

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    string? CurrentUser { get; }

    /// <summary>
    /// Returns the database id of the signed-in user, or <c>null</c> when signed out.
    /// </summary>
    Task<int?> GetCurrentUserIdAsync();

    /// <summary>
    /// Provisions a new username/password credential (PBKDF2-HMAC-SHA256 hashed).
    /// </summary>
    Task RegisterCredentialAsync(string username, string password);

    /// <summary>
    /// Returns true if at least one user account exists in the database.
    /// </summary>
    Task<bool> AnyUserExistsAsync();
}
