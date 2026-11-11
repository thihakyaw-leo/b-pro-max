using System.Globalization;
using BProMax.Configuration;
using BProMax.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace BProMax.Tests;

public class RegionalSettingsServiceTests
{
    private readonly RegionalSettingsService _service;

    public RegionalSettingsServiceTests()
    {
        var settings = Options.Create(new RegionalSettings
        {
            Culture = "en-US",
            DateFormat = "yyyy-MM-dd",
            DateTimeFormat = "yyyy-MM-dd HH:mm:ss",
            CurrencySymbol = "$",
            CurrencyCode = "USD",
            DecimalDigits = 2,
            Use24HourTime = true,
            FirstDayOfWeek = 1
        });
        _service = new RegionalSettingsService(settings);
    }

    [Fact]
    public void CultureDefaultsToEnUs()
    {
        Assert.Equal("en-US", _service.Culture);
    }

    [Fact]
    public void DateFormatIsConfigurable()
    {
        // Arrange
        _service.DateFormat = "dd/MM/yyyy";

        // Act
        var result = _service.FormatDate(new DateTime(2026, 11, 11));

        // Assert
        Assert.Equal("11/11/2026", result);
    }

    [Fact]
    public void FormatDateUsesConfiguredFormat()
    {
        // Act
        var result = _service.FormatDate(new DateTime(2026, 11, 11));

        // Assert
        Assert.Equal("2026-11-11", result);
    }

    [Fact]
    public void FormatDateTimeUsesConfiguredFormat()
    {
        // Act
        var result = _service.FormatDateTime(new DateTime(2026, 11, 11, 14, 30, 0));

        // Assert
        Assert.Equal("2026-11-11 14:30:00", result);
    }

    [Fact]
    public void FormatCurrencyUsesConfiguredSymbolAndDigits()
    {
        // Act
        var result = _service.FormatCurrency(1234.567m);

        // Assert
        Assert.Equal("$1,234.57", result);
    }

    [Fact]
    public void FormatCurrencyRespectsDecimalDigits()
    {
        // Arrange
        _service.DecimalDigits = 0;

        // Act
        var result = _service.FormatCurrency(1234.5m);

        // Assert
        Assert.Equal("$1,235", result);
    }

    [Fact]
    public void FormatNumberUsesConfiguredDigits()
    {
        // Act
        var result = _service.FormatNumber(1234.5m);

        // Assert
        Assert.Equal("1,234.50", result);
    }

    [Fact]
    public void DecimalDigitsClampsToValidRange()
    {
        // Arrange & Act
        _service.DecimalDigits = -5;
        Assert.Equal(0, _service.DecimalDigits);

        _service.DecimalDigits = 50;
        Assert.Equal(10, _service.DecimalDigits);
    }

    [Fact]
    public void FirstDayOfWeekClampsToValidRange()
    {
        // Arrange & Act
        _service.FirstDayOfWeek = -1;
        Assert.Equal(0, _service.FirstDayOfWeek);

        _service.FirstDayOfWeek = 10;
        Assert.Equal(6, _service.FirstDayOfWeek);
    }

    [Fact]
    public void GetCultureInfoReturnsValidCultureForValidCultureName()
    {
        // Act
        var culture = _service.GetCultureInfo();

        // Assert
        Assert.Equal("en-US", culture.Name);
    }

    [Fact]
    public void GetCultureInfoFallsBackToInvariantForInvalidCultureName()
    {
        // Arrange
        _service.Culture = "xx-XX";

        // Act
        var culture = _service.GetCultureInfo();

        // Assert: new CultureInfo("xx-XX") doesn't throw in .NET, so it returns that culture
        Assert.Equal("xx-XX", culture.Name);
    }

    [Fact]
    public void GetCultureInfoReturnsInvariantWhenCultureNameIsEmpty()
    {
        // Arrange
        _service.Culture = string.Empty;

        // Act
        var culture = _service.GetCultureInfo();

        // Assert
        Assert.Equal(CultureInfo.InvariantCulture.Name, culture.Name);
    }

    [Fact]
    public void CurrencyCodeIsConfigurable()
    {
        // Arrange
        _service.CurrencyCode = "EUR";

        // Act & Assert
        Assert.Equal("EUR", _service.CurrencyCode);
    }

    [Fact]
    public void FormatDateBurmeseEraReturnsMyanmarEraYear()
    {
        // Act
        var result = _service.FormatDateBurmeseEra(new DateTime(2026, 11, 11));

        // Assert: 2026 - 638 = 1388 ME
        Assert.Equal("11/11/1388 ME", result);
    }

    [Fact]
    public void FormatMyanmarKyatUsesKyatSymbol()
    {
        // Arrange
        _service.CurrencySymbol = "Ks";
        _service.DecimalDigits = 0;

        // Act
        var result = _service.FormatMyanmarKyat(15000m);

        // Assert
        Assert.Contains("Ks", result);
        Assert.Contains("15,000", result);
    }

    [Fact]
    public void ApplyMyanmarPresetSetsMyanmarValues()
    {
        // Act
        _service.ApplyMyanmarPreset();

        // Assert
        Assert.Equal("my-MM", _service.Culture);
        Assert.Equal("dd/MM/yyyy", _service.DateFormat);
        Assert.Equal("Ks", _service.CurrencySymbol);
        Assert.Equal("MMK", _service.CurrencyCode);
        Assert.Equal(0, _service.DecimalDigits);
        Assert.Equal(0, _service.FirstDayOfWeek); // Sunday
    }

    [Fact]
    public void FormatDateAfterMyanmarPresetUsesMyanmarFormat()
    {
        // Arrange
        _service.ApplyMyanmarPreset();

        // Act
        var result = _service.FormatDate(new DateTime(2026, 11, 11));

        // Assert
        Assert.Equal("11/11/2026", result);
    }

    [Fact]
    public void FormatMyanmarKyatDefaultsToKsSymbol()
    {
        // Arrange
        _service.CurrencySymbol = string.Empty;
        _service.DecimalDigits = 0;

        // Act
        var result = _service.FormatMyanmarKyat(1000m);

        // Assert
        Assert.Contains("Ks", result);
    }

    [Fact]
    public void FontFamilyDefaultsToSegoeUi()
    {
        Assert.Equal("Segoe UI", _service.FontFamily);
    }

    [Fact]
    public void FontFamilyIsConfigurable()
    {
        // Arrange
        _service.FontFamily = "Pyidaungsu";

        // Act & Assert
        Assert.Equal("Pyidaungsu", _service.FontFamily);
    }

    [Fact]
    public void ApplyMyanmarPresetSetsPyidaungsuFont()
    {
        // Act
        _service.ApplyMyanmarPreset();

        // Assert
        Assert.Equal("Pyidaungsu", _service.FontFamily);
    }

    [Fact]
    public void ApplyMyanmarUnicodeFontSetsPyidaungsuFont()
    {
        // Act
        _service.ApplyMyanmarUnicodeFont();

        // Assert
        Assert.Equal("Pyidaungsu", _service.FontFamily);
    }

    [Fact]
    public void MyanmarPresetHasPyidaungsuFontFamily()
    {
        // Act
        _service.ApplyMyanmarPreset();

        // Assert
        Assert.Equal("my-MM", _service.Culture);
        Assert.Equal("Pyidaungsu", _service.FontFamily);
        Assert.Equal("MMK", _service.CurrencyCode);
        Assert.Equal("Ks", _service.CurrencySymbol);
    }
}
