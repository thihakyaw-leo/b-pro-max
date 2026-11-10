namespace BProMax.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(string username, string password);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    string? CurrentUser { get; }
}
