namespace BProMax.Configuration;

/// <summary>
/// Regional and locale settings for the application.
/// Controls date, number, currency, and language formatting.
/// </summary>
public class RegionalSettings
{
    public string Culture { get; set; } = "en-US";
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    public string CurrencySymbol { get; set; } = "฿";
    public string CurrencyCode { get; set; } = "THB";
    public int DecimalDigits { get; set; } = 2;
    public bool Use24HourTime { get; set; } = true;
    public int FirstDayOfWeek { get; set; } = 1; // 0=Sunday, 1=Monday
    public string FontFamily { get; set; } = "Segoe UI"; // Default system font; set to "Pyidaungsu" for Myanmar Unicode
}
