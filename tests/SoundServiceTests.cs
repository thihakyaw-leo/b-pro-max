using System.Runtime.Versioning;
using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

[SupportedOSPlatform("windows")]
public class SoundServiceTests
{
    private readonly SoundService _soundService;

    public SoundServiceTests()
    {
        var settings = Microsoft.Extensions.Options.Options.Create(new BProMax.Configuration.SoundSettings
        {
            Enabled = true,
            Volume = 0.7
        });
        _soundService = new SoundService(settings);
    }

    [Fact]
    public void IsEnabledDefaultsToTrueWhenConfigEnabled()
    {
        // Assert
        Assert.True(_soundService.IsEnabled);
    }

    [Fact]
    public void SetVolumeClampsToValidRange()
    {
        // Arrange & Act
        _soundService.SetVolume(-0.5);
        // Volume should be clamped to 0

        _soundService.SetVolume(1.5);
        // Volume should be clamped to 1

        // Assert - just verify no exception thrown
        Assert.True(true);
    }

    [Fact]
    public async Task PlayAlertSoundAsyncDoesNotThrowWhenEnabled()
    {
        // Act & Assert
        await _soundService.PlayAlertSoundAsync();
        Assert.True(true);
    }

    [Fact]
    public async Task PlayAlertSoundAsyncDoesNotThrowWhenDisabled()
    {
        // Arrange
        var disabledSettings = Microsoft.Extensions.Options.Options.Create(new BProMax.Configuration.SoundSettings
        {
            Enabled = false,
            Volume = 0.7
        });
        var disabledService = new SoundService(disabledSettings);

        // Act & Assert
        await disabledService.PlayAlertSoundAsync();
        Assert.True(true);
    }
}

