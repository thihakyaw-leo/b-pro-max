using System.Globalization;
using BProMax.Configuration;
using Microsoft.Extensions.Options;

namespace BProMax.Services;

/// <summary>
/// Implementation of <see cref="IRegionalSettingsService"/> backed by <see cref="RegionalSettings"/>.
/// Supports Myanmar (Burma) localization: Burmese culture (my-MM), Myanmar Kyat (MMK) currency,
/// Burmese Era (ME) dates, and Myanmar locale formatting.
/// </summary>
public class RegionalSettingsService : IRegionalSettingsService
{
    /// <summary>
    /// Burmese Era (Myanmar Era) offset from Gregorian year.
    /// Gregorian year 2026 = Burmese Era year 1388 (2026 - 638).
    /// </summary>
    private const int BurmeseEraOffset = 638;

    private readonly RegionalSettings _settings;

    public RegionalSettingsService(IOptions<RegionalSettings> settings)
    {
        _settings = settings.Value;
    }

    public string Culture
    {
        get => _settings.Culture;
        set => _settings.Culture = value;
    }

    public string DateFormat
    {
        get => _settings.DateFormat;
        set => _settings.DateFormat = value;
    }

    public string DateTimeFormat
    {
        get => _settings.DateTimeFormat;
        set => _settings.DateTimeFormat = value;
    }

    public string CurrencySymbol
    {
        get => _settings.CurrencySymbol;
        set => _settings.CurrencySymbol = value;
    }

    public string CurrencyCode
    {
        get => _settings.CurrencyCode;
        set => _settings.CurrencyCode = value;
    }

    public int DecimalDigits
    {
        get => _settings.DecimalDigits;
        set => _settings.DecimalDigits = Math.Clamp(value, 0, 10);
    }

    public bool Use24HourTime
    {
        get => _settings.Use24HourTime;
        set => _settings.Use24HourTime = value;
    }

    public int FirstDayOfWeek
    {
        get => _settings.FirstDayOfWeek;
        set => _settings.FirstDayOfWeek = Math.Clamp(value, 0, 6);
    }

    public string FontFamily
    {
        get => _settings.FontFamily;
        set => _settings.FontFamily = value;
    }

    /// <summary>
    /// Gets the <see cref="CultureInfo"/> for the current culture, falling back to invariant if invalid.
    /// </summary>
    public CultureInfo GetCultureInfo()
    {
        try
        {
            return new CultureInfo(Culture);
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }

    public string FormatDate(DateTime dateValue)
    {
        return dateValue.ToString(DateFormat, GetCultureInfo());
    }

    public string FormatDateTime(DateTime dateTimeValue)
    {
        return dateTimeValue.ToString(DateTimeFormat, GetCultureInfo());
    }

    public string FormatCurrency(decimal amount)
    {
        var culture = GetCultureInfo();
        var nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
        nfi.CurrencySymbol = CurrencySymbol;
        nfi.CurrencyDecimalDigits = DecimalDigits;
        return amount.ToString("C", nfi);
    }

    public string FormatNumber(decimal amount)
    {
        var culture = GetCultureInfo();
        var nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
        nfi.NumberDecimalDigits = DecimalDigits;
        return amount.ToString("N", nfi);
    }

    /// <summary>
    /// Formats a date in Burmese Era (ME) year, e.g. "11/11/1388 ME" for 2026-11-11.
    /// Burmese Era year = Gregorian year - 638.
    /// </summary>
    public string FormatDateBurmeseEra(DateTime dateValue)
    {
        int burmeseYear = dateValue.Year - BurmeseEraOffset;
        return dateValue.ToString("dd/MM/" + burmeseYear, GetCultureInfo()) + " ME";
    }

    /// <summary>
    /// Formats an amount as Myanmar Kyat (MMK) using the configured Kyat symbol.
    /// </summary>
    public string FormatMyanmarKyat(decimal amount)
    {
        var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nfi.CurrencySymbol = string.IsNullOrEmpty(CurrencySymbol) ? "Ks" : CurrencySymbol;
        nfi.CurrencyDecimalDigits = DecimalDigits;
        return amount.ToString("C", nfi);
    }

    /// <summary>
    /// Applies a Myanmar (Burma) preset: my-MM culture, DD/MM/YYYY date, Kyat currency,
    /// Sunday first day of week, and Pyidaungsu font for Myanmar Unicode rendering.
    /// </summary>
    public void ApplyMyanmarPreset()
    {
        Culture = "my-MM";
        DateFormat = "dd/MM/yyyy";
        DateTimeFormat = "dd/MM/yyyy HH:mm:ss";
        CurrencySymbol = "Ks";
        CurrencyCode = "MMK";
        DecimalDigits = 0;
        Use24HourTime = true;
        FirstDayOfWeek = 0; // Sunday
        FontFamily = "Pyidaungsu"; // Myanmar Unicode font
    }

    /// <summary>
    /// Applies a Myanmar Unicode font preset: sets FontFamily to Pyidaungsu while keeping
    /// other settings intact. Use this when the Pyidaungsu font pack is installed on Windows
    /// and you need Myanmar Unicode text to render correctly.
    /// </summary>
    public void ApplyMyanmarUnicodeFont()
    {
        FontFamily = "Pyidaungsu";
    }
}

