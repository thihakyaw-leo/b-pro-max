using BProMax.Services;
using BProMax.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace BProMax.Tests;

public class TimeLockServiceTests
{
    [Fact]
    public void IsEnabledDefaultsToFalse()
    {
        // Arrange
        var settings = Options.Create(new TimeLockSettings { Enabled = false });
        var service = new TimeLockService(settings);

        // Assert
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public void IsWithinAllowedTimeWhenDisabledReturnsTrue()
    {
        // Arrange
        var settings = Options.Create(new TimeLockSettings { Enabled = false });
        var service = new TimeLockService(settings);

        // Act
        var result = service.IsWithinAllowedTime();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void StartHourDefaultsTo9()
    {
        // Arrange
        var settings = Options.Create(new TimeLockSettings());
        var service = new TimeLockService(settings);

        // Assert
        Assert.Equal(9, service.StartHour);
    }

    [Fact]
    public void EndHourDefaultsTo17()
    {
        // Arrange
        var settings = Options.Create(new TimeLockSettings());
        var service = new TimeLockService(settings);

        // Assert
        Assert.Equal(17, service.EndHour);
    }

    [Fact]
    public async Task RequestAccessAsyncReturnsTrue()
    {
        // Arrange
        var settings = Options.Create(new TimeLockSettings { Enabled = false });
        var service = new TimeLockService(settings);

        // Act
        var result = await service.RequestAccessAsync("Maintenance");

        // Assert
        Assert.True(result);
    }
}

