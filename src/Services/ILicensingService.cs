namespace BProMax.Services;

public interface ILicensingService
{
    bool IsRegistered { get; }
    string? LicenseKey { get; set; }
    Task<bool> ValidateLicenseAsync(string key);
    Task RegisterAsync(string key);
}
