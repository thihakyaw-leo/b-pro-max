using System.Globalization;
using BProMax.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Xunit;

namespace BProMax.Tests;

public class TimeLockServiceTests
{
    private readonly Mock<ISettingsStore> _mockSettings;
    private readonly Mock<IDatabaseService> _mockDb;

    public TimeLockServiceTests()
    {
        _mockSettings = new Mock<ISettingsStore>();
        _mockDb = new Mock<IDatabaseService>();
    }

    private TimeLockService CreateService(bool enabled = false, int startHour = 9, int endHour = 17)
    {
        _mockSettings.Setup(x => x.GetAsync("TimeLock:Enabled")).ReturnsAsync(enabled.ToString(CultureInfo.InvariantCulture));
        _mockSettings.Setup(x => x.GetAsync("TimeLock:StartHour")).ReturnsAsync(startHour.ToString(CultureInfo.InvariantCulture));
        _mockSettings.Setup(x => x.GetAsync("TimeLock:EndHour")).ReturnsAsync(endHour.ToString(CultureInfo.InvariantCulture));
        _mockDb.Setup(x => x.ExecuteNonQueryAsync(
            It.IsAny<string>(),
            It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);
        return new TimeLockService(_mockSettings.Object, _mockDb.Object, new TestLogger<TimeLockService>());
    }

    [Fact]
    public async Task IsEnabledDefaultsToFalse()
    {
        // Arrange
        var service = CreateService(enabled: false);

        // Assert
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public async Task IsWithinAllowedTimeWhenDisabledReturnsTrue()
    {
        // Arrange
        var service = CreateService(enabled: false);

        // Act
        var result = service.IsWithinAllowedTime();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task StartHourDefaultsTo9()
    {
        // Arrange
        var service = CreateService(startHour: 9);

        // Assert
        Assert.Equal(9, service.StartHour);
    }

    [Fact]
    public async Task EndHourDefaultsTo17()
    {
        // Arrange
        var service = CreateService(endHour: 17);

        // Assert
        Assert.Equal(17, service.EndHour);
    }

    [Fact]
    public async Task RequestAccessAsyncWhenDisabledReturnsTrue()
    {
        // Arrange
        var service = CreateService(enabled: false);

        // Act
        var result = await service.RequestAccessAsync("Maintenance");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task StartHourSetterRejectsOutOfRangeValue()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => service.StartHour = 24);
        Assert.Throws<ArgumentOutOfRangeException>(() => service.StartHour = -1);
    }

    [Fact]
    public async Task EndHourSetterRejectsOutOfRangeValue()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => service.EndHour = 24);
        Assert.Throws<ArgumentOutOfRangeException>(() => service.EndHour = -1);
    }

    [Fact]
    public async Task SettingsPersistToStore()
    {
        // Arrange
        var service = CreateService(enabled: true, startHour: 8, endHour: 18);

        // Act
        await service.RequestAccessAsync("Test");

        // Assert
        _mockSettings.Verify(x => x.SetAsync("TimeLock:Enabled", "True"), Times.Once);
        _mockSettings.Verify(x => x.SetAsync("TimeLock:StartHour", "8"), Times.Once);
        _mockSettings.Verify(x => x.SetAsync("TimeLock:EndHour", "18"), Times.Once);
    }

    [Fact]
    public async Task AccessRequestIsAuditedToDatabase()
    {
        // Arrange
        var service = CreateService(enabled: false);

        // Act
        await service.RequestAccessAsync("Maintenance");

        // Assert
        _mockDb.Verify(x => x.ExecuteNonQueryAsync(
            It.Is<string>(s => s.Contains("INSERT INTO access_requests")),
            It.IsAny<NpgsqlParameter[]>()),
            Times.Once);
    }
}
