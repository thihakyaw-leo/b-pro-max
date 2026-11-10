namespace BProMax.Services;

public class LicensingService : ILicensingService
{
    public bool IsRegistered => !string.IsNullOrEmpty(LicenseKey);
    public string? LicenseKey { get; set; }

    public async Task<bool> ValidateLicenseAsync(string key)
    {
        // TODO: Implement actual license validation
        await Task.Delay(50);
        return !string.IsNullOrWhiteSpace(key) && key.Length >= 10;
    }

    public Task RegisterAsync(string key)
    {
        LicenseKey = key;
        return Task.CompletedTask;
    }
}