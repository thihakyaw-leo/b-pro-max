using System.Globalization;
using System.Text;

namespace BProMax;

/// <summary>
/// Writes unhandled exceptions to a log file under %LOCALAPPDATA%\b-pro-max.
/// A WinUI 3 app terminates without any console output when an exception escapes
/// an event handler, so this file is the only way to diagnose a crash in the field.
/// </summary>
internal static class CrashLogger
{
    private const int MaxLogBytes = 512 * 1024;

    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "b-pro-max");

    public static string LogPath { get; } = Path.Combine(LogDirectory, "crash.log");

    public static void Write(Exception? exception, string source)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);

            var entry = new StringBuilder();
            entry.AppendLine(CultureInfo.InvariantCulture, $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {source}");
            if (exception is not null)
            {
                entry.AppendLine(exception.ToString());
            }

            var root = exception;
            while (root?.InnerException is not null)
            {
                root = root.InnerException;
                entry.AppendLine("--- inner ---");
                entry.AppendLine(root.ToString());
            }

            AppendWithRotation(entry.ToString());
        }
        catch
        {
            // Never let crash logging itself crash the app.
        }
    }

    private static void AppendWithRotation(string text)
    {
        try
        {
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxLogBytes)
            {
                File.Delete(LogPath);
            }

            File.AppendAllText(LogPath, text, Encoding.UTF8);
        }
        catch
        {
            // Ignore IO errors during crash handling.
        }
    }
}
