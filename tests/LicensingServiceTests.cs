using BProMax.Services;
using Moq;
using Xunit;

namespace BProMax.Tests;

public class LicensingServiceTests
{
    private readonly LicensingService _service;
    private readonly Mock<ISettingsStore> _mockSettings;

    // "AAAAAAAAAAB": body of 10 'A's has weighted checksum 3575, 3575 % 36 = 11 -> 'B'.
    private const string ValidKey = "AAAAAAAAAAB";

    public LicensingServiceTests()
    {
        _mockSettings = new Mock<ISettingsStore>();
        _mockSettings.Setup(x => x.GetAsync("License:Key")).ReturnsAsync((string?)null);
        _service = new LicensingService(_mockSettings.Object, new TestLogger<LicensingService>());
    }

    [Fact]
    public void IsRegisteredDefaultsToFalse()
    {
        // Assert
        Assert.False(_service.IsRegistered);
        Assert.Null(_service.LicenseKey);
    }

    [Fact]
    public async Task ValidateLicenseAsyncWithValidKeyReturnsTrue()
    {
        // Act
        var result = await _service.ValidateLicenseAsync(ValidKey);

        // Assert
        Assert.True(result);
        Assert.True(_service.IsRegistered);
        Assert.Equal(ValidKey, _service.LicenseKey);
        _mockSettings.Verify(x => x.SetAsync("License:Key", ValidKey), Times.Once);
    }

    [Fact]
    public async Task ValidateLicenseAsyncWithShortKeyReturnsFalse()
    {
        // Act
        var result = await _service.ValidateLicenseAsync("SHORT");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateLicenseAsyncWithEmptyKeyReturnsFalse()
    {
        // Act
        var result = await _service.ValidateLicenseAsync("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateLicenseAsyncWithInvalidChecksumReturnsFalse()
    {
        // Act
        var result = await _service.ValidateLicenseAsync("AAAAAAAAAAC");

        // Assert
        Assert.False(result);
        Assert.False(_service.IsRegistered);
    }

    [Fact]
    public async Task RegisterAsyncSetsLicenseKey()
    {
        // Act
        await _service.RegisterAsync(ValidKey);

        // Assert
        Assert.True(_service.IsRegistered);
        Assert.Equal(ValidKey, _service.LicenseKey);
        _mockSettings.Verify(x => x.SetAsync("License:Key", ValidKey), Times.Once);
    }

    [Fact]
    public async Task ClearAsyncRemovesLicense()
    {
        // Arrange
        await _service.RegisterAsync(ValidKey);
        Assert.True(_service.IsRegistered);

        // Act
        await _service.ClearAsync();

        // Assert
        Assert.False(_service.IsRegistered);
        Assert.Null(_service.LicenseKey);
        _mockSettings.Verify(x => x.DeleteAsync("License:Key"), Times.Once);
    }

    [Fact]
    public async Task LicenseKeyPersistsAcrossInstances()
    {
        // Arrange
        _mockSettings.Setup(x => x.GetAsync("License:Key")).ReturnsAsync(ValidKey);
        var service2 = new LicensingService(_mockSettings.Object, new TestLogger<LicensingService>());
        await service2.ValidateLicenseAsync(ValidKey); // Triggers EnsureLoadedAsync

        // Act & Assert
        Assert.True(service2.IsRegistered);
        Assert.Equal(ValidKey, service2.LicenseKey);
    }
}
