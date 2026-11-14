using System.Globalization;
using BProMax.Configuration;
using Microsoft.Extensions.Options;

namespace BProMax.Services;

/// <summary>
/// Implementation of <see cref="IRegionalSettingsService"/> backed by <see cref="RegionalSettings"/>.
/// Supports Myanmar (Burma) localization: Burmese culture (my-MM), Myanmar Kyat (MMK) currency,
/// Burmese Era (ME) dates, and Myanmar locale formatting.
/// Settings can be persisted to and loaded from the database via <see cref="ISettingsStore"/>.
/// </summary>
public class RegionalSettingsService : IRegionalSettingsService
{
    /// <summary>
    /// Burmese Era (Myanmar Era) offset from Gregorian year.
    /// Gregorian year 2026 = Burmese Era year 1388 (2026 - 638).
    /// </summary>
    private const int BurmeseEraOffset = 638;

    private const string CultureKey = "Regional:Culture";
    private const string DateFormatKey = "Regional:DateFormat";
    private const string DateTimeFormatKey = "Regional:DateTimeFormat";
    private const string CurrencySymbolKey = "Regional:CurrencySymbol";
    private const string CurrencyCodeKey = "Regional:CurrencyCode";
    private const string DecimalDigitsKey = "Regional:DecimalDigits";
    private const string Use24HourTimeKey = "Regional:Use24HourTime";
    private const string FirstDayOfWeekKey = "Regional:FirstDayOfWeek";
    private const string FontFamilyKey = "Regional:FontFamily";

    private readonly RegionalSettings _settings;
    private readonly ISettingsStore? _settingsStore;

    public RegionalSettingsService(IOptions<RegionalSettings> settings, ISettingsStore? settingsStore = null)
    {
        _settings = settings.Value;
        _settingsStore = settingsStore;
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
        return dateValue.ToString(DateFormat, GetDateTimeFormatInfo());
    }

    public string FormatDateTime(DateTime dateTimeValue)
    {
        return dateTimeValue.ToString(DateTimeFormat, GetDateTimeFormatInfo());
    }

    /// <summary>
    /// Gets the <see cref="DateTimeFormatInfo"/> for the current culture with a fixed date separator.
    /// In a custom date format, "/" is a placeholder replaced by the culture's
    /// <see cref="DateTimeFormatInfo.DateSeparator"/>, which varies by platform (this machine's ICU
    /// data resolves en-US to "-"). Pinning it to "/" keeps configured formats such as "dd/MM/yyyy"
    /// rendering exactly as written on every host.
    /// </summary>
    private DateTimeFormatInfo GetDateTimeFormatInfo()
    {
        var formatInfo = (DateTimeFormatInfo)GetCultureInfo().DateTimeFormat.Clone();
        formatInfo.DateSeparator = "/";
        return formatInfo;
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
        return dateValue.ToString("dd/MM/" + burmeseYear, GetDateTimeFormatInfo()) + " ME";
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

    /// <summary>
    /// Loads regional settings from the database settings store, overriding in-memory values.
    /// Missing keys leave the current value unchanged.
    /// </summary>
    public async Task LoadFromStoreAsync()
    {
        if (_settingsStore == null) return;

        var culture = await _settingsStore.GetAsync(CultureKey);
        if (!string.IsNullOrEmpty(culture)) Culture = culture;

        var dateFormat = await _settingsStore.GetAsync(DateFormatKey);
        if (!string.IsNullOrEmpty(dateFormat)) DateFormat = dateFormat;

        var dateTimeFormat = await _settingsStore.GetAsync(DateTimeFormatKey);
        if (!string.IsNullOrEmpty(dateTimeFormat)) DateTimeFormat = dateTimeFormat;

        var currencySymbol = await _settingsStore.GetAsync(CurrencySymbolKey);
        if (!string.IsNullOrEmpty(currencySymbol)) CurrencySymbol = currencySymbol;

        var currencyCode = await _settingsStore.GetAsync(CurrencyCodeKey);
        if (!string.IsNullOrEmpty(currencyCode)) CurrencyCode = currencyCode;

        var decimalDigits = await _settingsStore.GetAsync(DecimalDigitsKey);
        if (!string.IsNullOrEmpty(decimalDigits) && int.TryParse(decimalDigits, out var dd))
            DecimalDigits = dd;

        var use24Hour = await _settingsStore.GetAsync(Use24HourTimeKey);
        if (!string.IsNullOrEmpty(use24Hour) && bool.TryParse(use24Hour, out var u24))
            Use24HourTime = u24;

        var firstDay = await _settingsStore.GetAsync(FirstDayOfWeekKey);
        if (!string.IsNullOrEmpty(firstDay) && int.TryParse(firstDay, out var fd))
            FirstDayOfWeek = fd;

        var fontFamily = await _settingsStore.GetAsync(FontFamilyKey);
        if (!string.IsNullOrEmpty(fontFamily)) FontFamily = fontFamily;
    }

    /// <summary>
    /// Persists all regional settings to the database settings store.
    /// </summary>
    public async Task SaveToStoreAsync()
    {
        if (_settingsStore == null) return;

        await _settingsStore.SetAsync(CultureKey, Culture);
        await _settingsStore.SetAsync(DateFormatKey, DateFormat);
        await _settingsStore.SetAsync(DateTimeFormatKey, DateTimeFormat);
        await _settingsStore.SetAsync(CurrencySymbolKey, CurrencySymbol);
        await _settingsStore.SetAsync(CurrencyCodeKey, CurrencyCode);
        await _settingsStore.SetAsync(DecimalDigitsKey, DecimalDigits.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await _settingsStore.SetAsync(Use24HourTimeKey, Use24HourTime.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await _settingsStore.SetAsync(FirstDayOfWeekKey, FirstDayOfWeek.ToString(System.Globalization.CultureInfo.InvariantCulture));
        await _settingsStore.SetAsync(FontFamilyKey, FontFamily);
    }
}

