using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class LicensingServiceTests
{
    [Fact]
    public void IsRegisteredDefaultsToFalse()
    {
        // Arrange
        var service = new LicensingService();

        // Assert
        Assert.False(service.IsRegistered);
        Assert.Null(service.LicenseKey);
    }

    [Fact]
    public async Task ValidateLicenseAsyncWithValidKeyReturnsTrue()
    {
        // Arrange
        var service = new LicensingService();

        // Act
        var result = await service.ValidateLicenseAsync("ABCDEFGHIJ");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateLicenseAsyncWithShortKeyReturnsFalse()
    {
        // Arrange
        var service = new LicensingService();

        // Act
        var result = await service.ValidateLicenseAsync("SHORT");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateLicenseAsyncWithEmptyKeyReturnsFalse()
    {
        // Arrange
        var service = new LicensingService();

        // Act
        var result = await service.ValidateLicenseAsync("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RegisterAsyncSetsLicenseKey()
    {
        // Arrange
        var service = new LicensingService();

        // Act
        await service.RegisterAsync("ABCDEFGHIJKLMNOP");

        // Assert
        Assert.True(service.IsRegistered);
        Assert.Equal("ABCDEFGHIJKLMNOP", service.LicenseKey);
    }
}

