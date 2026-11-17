using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace BProMax.Views;

/// <summary>
/// Resolves the status-message brushes used by the views.
/// </summary>
/// <remarks>
/// WinUI 3 dropped the WinUI 2 <c>SystemControl*TextForegroundBrush</c> keys, and
/// <see cref="ResourceDictionary"/>'s indexer raises
/// <see cref="System.Runtime.InteropServices.COMException"/> for an unknown key instead of
/// returning <see langword="null"/>. A direct lookup of a missing theme key therefore
/// terminates the app instead of falling back to a colour, so every lookup here goes
/// through <c>TryGetValue</c> and falls back to an explicit colour.
/// </remarks>
internal static class ThemeBrush
{
    /// <summary>Brush for a successful operation.</summary>
    public static Brush Success => Resolve("SuccessTextBrush", Microsoft.UI.Colors.Green);

    /// <summary>Brush for a failed operation.</summary>
    public static Brush Error => Resolve("ErrorTextBrush", Microsoft.UI.Colors.Red);

    /// <summary>Brush for a warning or partial result.</summary>
    public static Brush Warning => Resolve("WarningTextBrush", Microsoft.UI.Colors.Orange);

    private static Brush Resolve(string key, Color fallback)
    {
        if (Application.Current is { } app
            && app.Resources.TryGetValue(key, out var value)
            && value is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(fallback);
    }
}
