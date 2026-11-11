using Microsoft.Extensions.Logging;

namespace BProMax.Services;

/// <summary>
/// Licensing service with real key validation.
/// Keys must satisfy an allowed-character format and a structural checksum.
/// Validation state is persisted in the database via <see cref="ISettingsStore"/>.
/// </summary>
public class LicensingService : ILicensingService
{
    private const int MinKeyLength = 10;
    private const int MaxKeyLength = 64;
    private const string LicenseKeySettingKey = "License:Key";
    private static readonly char[] ValidKeyChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-".ToCharArray();
    private static readonly string ChecksumAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private readonly ISettingsStore _settings;
    private readonly ILogger<LicensingService> _logger;

    private static readonly Action<ILogger, Exception?> _logValidationEmptyKey =
        LoggerMessage.Define(LogLevel.Warning, 1, "License validation attempted with empty key");

    private static readonly Action<ILogger, Exception?> _logValidationInvalidFormat =
        LoggerMessage.Define(LogLevel.Warning, 2, "License validation rejected: invalid key format");

    private static readonly Action<ILogger, Exception?> _logValidationSuccess =
        LoggerMessage.Define(LogLevel.Information, 3, "License validated and registered");

    private static readonly Action<ILogger, Exception?> _logValidationBadChecksum =
        LoggerMessage.Define(LogLevel.Warning, 4, "License validation rejected: invalid key checksum");

    private static readonly Action<ILogger, bool, Exception?> _logRegistered =
        LoggerMessage.Define<bool>(LogLevel.Information, 5, "License key registered (validated={Validated})");

    private string? _licenseKey;
    private bool _isValidated;
    private bool _loaded;

    public bool IsRegistered => !string.IsNullOrEmpty(_licenseKey) && _isValidated;
    public string? LicenseKey
    {
        get => _licenseKey;
        set
        {
            if (value is not null && !IsValidKeyFormat(value))
            {
                throw new ArgumentException("Invalid license key format.", nameof(value));
            }
            _licenseKey = value;
            _isValidated = false;
        }
    }

    public LicensingService(ISettingsStore settings, ILogger<LicensingService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        _licenseKey = await _settings.GetAsync(LicenseKeySettingKey);
        _isValidated = !string.IsNullOrEmpty(_licenseKey) && VerifyKeyChecksum(_licenseKey);
    }

    public async Task<bool> ValidateLicenseAsync(string key)
    {
        await EnsureLoadedAsync();

        if (string.IsNullOrWhiteSpace(key))
        {
            _logValidationEmptyKey(_logger, null);
            return false;
        }

        if (!IsValidKeyFormat(key))
        {
            _logValidationInvalidFormat(_logger, null);
            return false;
        }

        bool valid = VerifyKeyChecksum(key);
        if (valid)
        {
            _licenseKey = key;
            _isValidated = true;
            await _settings.SetAsync(LicenseKeySettingKey, key);
            _logValidationSuccess(_logger, null);
        }
        else
        {
            _logValidationBadChecksum(_logger, null);
        }

        return valid;
    }

    public async Task RegisterAsync(string key)
    {
        await EnsureLoadedAsync();

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("License key is required.", nameof(key));
        }
        if (!IsValidKeyFormat(key))
        {
            throw new ArgumentException("Invalid license key format.", nameof(key));
        }

        _licenseKey = key;
        _isValidated = VerifyKeyChecksum(key);
        await _settings.SetAsync(LicenseKeySettingKey, key);
        _logRegistered(_logger, _isValidated, null);
    }

    /// <summary>
    /// Clears the persisted license key.
    /// </summary>
    public async Task ClearAsync()
    {
        _licenseKey = null;
        _isValidated = false;
        await _settings.DeleteAsync(LicenseKeySettingKey);
    }

    private static bool IsValidKeyFormat(string key)
    {
        if (key.Length < MinKeyLength || key.Length > MaxKeyLength)
        {
            return false;
        }
        foreach (var c in key)
        {
            if (Array.IndexOf(ValidKeyChars, c) < 0)
            {
                return false;
            }
        }
        return true;
    }

    private static bool VerifyKeyChecksum(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }
        // Structural checksum: trailing character must equal a parity value derived from the rest.
        if (key.Length < MinKeyLength + 1)
        {
            return false;
        }
        var body = key.AsSpan(0, key.Length - 1);
        var checkChar = key[key.Length - 1];
        int sum = 0;
        for (int i = 0; i < body.Length; i++)
        {
            sum += body[i] * (i + 1);
        }
        var expected = ChecksumAlphabet[sum % ChecksumAlphabet.Length];
        return checkChar == expected;
    }
}
