namespace BProMax.Services;

/// <summary>
/// Service for managing regional and locale settings.
/// Provides culture-aware formatting for dates, numbers, and currency.
/// </summary>
public interface IRegionalSettingsService
{
    string Culture { get; set; }
    string DateFormat { get; set; }
    string DateTimeFormat { get; set; }
    string CurrencySymbol { get; set; }
    string CurrencyCode { get; set; }
    int DecimalDigits { get; set; }
    bool Use24HourTime { get; set; }
    int FirstDayOfWeek { get; set; }
    string FontFamily { get; set; }

    /// <summary>
    /// Gets the <see cref="System.Globalization.CultureInfo"/> for the current culture.
    /// </summary>
    System.Globalization.CultureInfo GetCultureInfo();

    /// <summary>
    /// Formats a date using the configured date format.
    /// </summary>
    string FormatDate(DateTime dateValue);

    /// <summary>
    /// Formats a date and time using the configured date/time format.
    /// </summary>
    string FormatDateTime(DateTime dateTimeValue);

    /// <summary>
    /// Formats a decimal amount as currency using the configured currency symbol and decimal digits.
    /// </summary>
    string FormatCurrency(decimal amount);

    /// <summary>
    /// Formats a decimal number using the configured decimal digits.
    /// </summary>
    string FormatNumber(decimal amount);
}
