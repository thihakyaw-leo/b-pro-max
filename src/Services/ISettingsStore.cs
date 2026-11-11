namespace BProMax.Services;

/// <summary>
/// Key-value settings store backed by the database.
/// Used for persisting license, time-lock, sound, and regional settings.
/// </summary>
public interface ISettingsStore
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task DeleteAsync(string key);
}
