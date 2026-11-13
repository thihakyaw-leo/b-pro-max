using System.Security.Cryptography;
using BProMax.Models;
using BProMax.Services;
using Moq;
using Npgsql;
using Xunit;

namespace BProMax.Tests;

public class AuthServiceTests
{
    private readonly Mock<IDatabaseService> _mockDb;
    private readonly Mock<ITimeLockService> _mockTimeLock;
    private readonly AuthService _service;
    private const int TestIterations = 1000; // Low iteration count for fast tests

    public AuthServiceTests()
    {
        _mockDb = new Mock<IDatabaseService>();
        _mockTimeLock = new Mock<ITimeLockService>();
        _mockTimeLock.Setup(x => x.IsWithinAllowedTime()).Returns(true);
        _service = new AuthService(_mockDb.Object, _mockTimeLock.Object, new TestLogger<AuthService>());
    }

    private static byte[] ComputeTestHash(string password, byte[] salt, int iterations)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }

    private static User CreateTestUser(
        string username = "admin",
        string password = "password",
        int iterations = TestIterations,
        int failedAttempts = 0,
        DateTime? lockedUntil = null)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = ComputeTestHash(password, salt, iterations);
        return new User
        {
            Id = 1,
            Username = username,
            PasswordHash = hash,
            Salt = salt,
            Iterations = iterations,
            FailedAttempts = failedAttempts,
            LockedUntil = lockedUntil,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task LoginAsyncWithValidCredentialsReturnsSuccess()
    {
        // Arrange
        var user = CreateTestUser("admin", "password");
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("LOWER(username)")),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(user);
        _mockDb
            .Setup(x => x.ExecuteNonQueryAsync(
                It.Is<string>(s => s.Contains("failed_attempts = 0")),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.LoginAsync("admin", "password");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(AuthFailureReason.None, result.FailureReason);
        Assert.True(_service.IsAuthenticated);
        Assert.Equal("admin", _service.CurrentUser);
    }

    [Fact]
    public async Task LoginAsyncWithWrongPasswordReturnsInvalidCredentials()
    {
        // Arrange
        var user = CreateTestUser("admin", "password");
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("LOWER(username)")),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(user);
        _mockDb
            .Setup(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.LoginAsync("admin", "wrongpassword");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AuthFailureReason.InvalidCredentials, result.FailureReason);
        Assert.False(_service.IsAuthenticated);
        Assert.Null(_service.CurrentUser);
    }

    [Fact]
    public async Task LoginAsyncWithUnknownUserReturnsInvalidCredentials()
    {
        // Arrange
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("LOWER(username)")),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.LoginAsync("unknown", "password");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AuthFailureReason.InvalidCredentials, result.FailureReason);
        Assert.False(_service.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsyncWithEmptyUsernameReturnsEmptyInput()
    {
        // Act
        var result = await _service.LoginAsync("", "password");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AuthFailureReason.EmptyInput, result.FailureReason);
    }

    [Fact]
    public async Task LoginAsyncWithEmptyPasswordReturnsEmptyInput()
    {
        // Act
        var result = await _service.LoginAsync("admin", "");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AuthFailureReason.EmptyInput, result.FailureReason);
    }

    [Fact]
    public async Task LogoutAsyncClearsAuthentication()
    {
        // Arrange
        var user = CreateTestUser("admin", "password");
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(user);
        _mockDb
            .Setup(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        await _service.LoginAsync("admin", "password");
        Assert.True(_service.IsAuthenticated);

        // Act
        await _service.LogoutAsync();

        // Assert
        Assert.False(_service.IsAuthenticated);
        Assert.Null(_service.CurrentUser);
    }

    [Fact]
    public void IsAuthenticatedDefaultsToFalse()
    {
        // Arrange
        var service = new AuthService(_mockDb.Object, _mockTimeLock.Object, new TestLogger<AuthService>());

        // Assert
        Assert.False(service.IsAuthenticated);
        Assert.Null(service.CurrentUser);
    }

    [Fact]
    public async Task LoginAsyncLocksOutAfterRepeatedFailures()
    {
        // Arrange
        var user = CreateTestUser("admin", "password", failedAttempts: 0);
        var now = DateTimeOffset.UtcNow;

        // First 4 failures: increment count, no lockout yet
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(user);
        _mockDb
            .Setup(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        for (int i = 0; i < 4; i++)
        {
            var loginResult = await _service.LoginAsync("admin", "wrong");
            Assert.False(loginResult.Success);
        }

        // 5th failure: should lock out
        var lockedUntil = now.AddMinutes(15);
        var lockedUser = CreateTestUser("admin", "password", failedAttempts: 4, lockedUntil: lockedUntil.UtcDateTime);
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(lockedUser);

        var result = await _service.LoginAsync("admin", "password");
        Assert.False(result.Success);
        Assert.Equal(AuthFailureReason.LockedOut, result.FailureReason);
        Assert.NotNull(result.LockedUntil);
        Assert.False(_service.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsyncWhileLockedOutReturnsLockedOut()
    {
        // Arrange
        var lockedUntil = DateTimeOffset.UtcNow.AddMinutes(15);
        var lockedUser = CreateTestUser("admin", "password", lockedUntil: lockedUntil.UtcDateTime);
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(lockedUser);

        // Act
        var result = await _service.LoginAsync("admin", "password");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AuthFailureReason.LockedOut, result.FailureReason);
        Assert.NotNull(result.LockedUntil);
        Assert.False(_service.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsyncOutsideAllowedHoursReturnsOutsideAllowedHours()
    {
        // Arrange
        _mockTimeLock.Setup(x => x.IsWithinAllowedTime()).Returns(false);
        var user = CreateTestUser("admin", "password");
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.IsAny<string>(),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.LoginAsync("admin", "password");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(AuthFailureReason.OutsideAllowedHours, result.FailureReason);
        Assert.False(_service.IsAuthenticated);
    }

    [Fact]
    public async Task AnyUserExistsAsyncReturnsTrueWhenUsersExist()
    {
        // Arrange
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("EXISTS")),
                It.IsAny<Func<NpgsqlDataReader, bool>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.AnyUserExistsAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AnyUserExistsAsyncReturnsFalseWhenNoUsers()
    {
        // Arrange
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("EXISTS")),
                It.IsAny<Func<NpgsqlDataReader, bool>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.AnyUserExistsAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RegisterCredentialAsyncCallsDatabase()
    {
        // Arrange
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("LOWER(username)")),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync((User?)null);
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("RETURNING id")),
                It.IsAny<Func<NpgsqlDataReader, int>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        // Act
        await _service.RegisterCredentialAsync("newuser", "Password123!");

        // Assert: Verify that QuerySingleAsync with INSERT was called
        _mockDb.Verify(
            x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("INSERT INTO users")),
                It.IsAny<Func<NpgsqlDataReader, int>>(),
                It.IsAny<NpgsqlParameter[]>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterCredentialAsyncRejectsDuplicateUsername()
    {
        // Arrange: an existing user (different case) is found by the case-insensitive lookup.
        var existing = CreateTestUser("Admin", "password");
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("LOWER(username)")),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(existing);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegisterCredentialAsync("admin", "Password123!"));
        Assert.Contains("already exists", ex.Message);
    }

    [Fact]
    public async Task RegisterCredentialAsyncRejectsEmptyUsername()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RegisterCredentialAsync("  ", "Password123!"));
        Assert.StartsWith("Username is required.", ex.Message);
    }

    [Fact]
    public async Task RegisterCredentialAsyncRejectsEmptyPassword()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RegisterCredentialAsync("newuser", "   "));
        Assert.StartsWith("Password is required.", ex.Message);
    }

    [Fact]
    public async Task UserPersistsAcrossServiceReInstantiation()
    {
        // Arrange: first service instance creates a user.
        var user = CreateTestUser("admin", "password");
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("LOWER(username)")),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(user);
        _mockDb
            .Setup(x => x.ExecuteNonQueryAsync(
                It.Is<string>(s => s.Contains("failed_attempts = 0")),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        var service1 = new AuthService(_mockDb.Object, _mockTimeLock.Object, new TestLogger<AuthService>());
        var result1 = await service1.LoginAsync("admin", "password");
        Assert.True(result1.Success);

        // Second service instance re-reads the same user from the database.
        _mockDb
            .Setup(x => x.QuerySingleAsync(
                It.Is<string>(s => s.Contains("LOWER(username)")),
                It.IsAny<Func<NpgsqlDataReader, User?>>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(user);
        _mockDb
            .Setup(x => x.ExecuteNonQueryAsync(
                It.Is<string>(s => s.Contains("failed_attempts = 0")),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        var service2 = new AuthService(_mockDb.Object, _mockTimeLock.Object, new TestLogger<AuthService>());
        var result2 = await service2.LoginAsync("admin", "password");

        // Assert: the account persists across service re-instantiation.
        Assert.True(result2.Success);
        Assert.Equal("admin", service2.CurrentUser);
        Assert.True(service2.IsAuthenticated);
    }
}
