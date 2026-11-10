namespace BProMax.Services;

public interface ITimeLockService
{
    bool IsEnabled { get; set; }
    int StartHour { get; set; }
    int EndHour { get; set; }
    bool IsWithinAllowedTime();
    Task<bool> RequestAccessAsync(string reason);
}
