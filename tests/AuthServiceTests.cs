using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsyncWithValidCredentialsReturnsTrue()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = await service.LoginAsync("admin", "password");

        // Assert
        Assert.True(result);
        Assert.True(service.IsAuthenticated);
        Assert.Equal("admin", service.CurrentUser);
    }

    [Fact]
    public async Task LoginAsyncWithEmptyUsernameReturnsFalse()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = await service.LoginAsync("", "password");

        // Assert
        Assert.False(result);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task LoginAsyncWithEmptyPasswordReturnsFalse()
    {
        // Arrange
        var service = new AuthService();

        // Act
        var result = await service.LoginAsync("admin", "");

        // Assert
        Assert.False(result);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task LogoutAsyncClearsAuthentication()
    {
        // Arrange
        var service = new AuthService();
        await service.LoginAsync("admin", "password");
        Assert.True(service.IsAuthenticated);

        // Act
        await service.LogoutAsync();

        // Assert
        Assert.False(service.IsAuthenticated);
        Assert.Null(service.CurrentUser);
    }

    [Fact]
    public void IsAuthenticatedDefaultsToFalse()
    {
        // Arrange
        var service = new AuthService();

        // Assert
        Assert.False(service.IsAuthenticated);
        Assert.Null(service.CurrentUser);
    }
}

