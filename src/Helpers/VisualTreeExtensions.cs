using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace BProMax.Helpers;

/// <summary>
/// Helper to locate XAML controls by name at runtime when compiled .g.cs fields are unavailable.
/// </summary>
public static class VisualTreeExtensions
{
    public static T? FindChildByName<T>(this DependencyObject parent, string name) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed && ((FrameworkElement)child).Name == name)
                return typed;
            var result = FindChildByName<T>(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Overload for types that are not DependencyObject (e.g. Window in WinUI 3).
    /// </summary>
    public static T? FindChildByName<T>(this object parent, string name) where T : DependencyObject
    {
        if (parent is DependencyObject dep)
            return FindChildByName<T>(dep, name);
        return null;
    }
}
