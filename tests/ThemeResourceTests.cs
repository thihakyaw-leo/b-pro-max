using System.Text.RegularExpressions;
using Xunit;

namespace BProMax.Tests;

/// <summary>
/// Guards against the WinUI 3 theme-resource lookup bug.
///
/// <para>
/// WinUI 3 removed the WinUI 2 <c>SystemControlSuccessTextForegroundBrush</c> and
/// <c>SystemControlWarningTextForegroundBrush</c> keys. Looking up a missing key through
/// <c>Application.Current.Resources[key]</c> raises a <c>COMException</c> instead of
/// returning <see langword="null"/>, so the usual
/// <c>resources[key] as Brush ?? fallback</c> idiom terminates the app the moment the
/// success or warning path runs.
/// </para>
/// <para>
/// These tests scan the view sources so the regression cannot come back silently. The
/// failure mode is a dead process with no console output, so it is very expensive to
/// diagnose from the outside.
/// </para>
/// </summary>
public class ThemeResourceTests
{
    /// <summary>Keys that exist in WinUI 2 but were never carried into WinUI 3.</summary>
    private static readonly string[] RemovedWinUi2Keys =
    {
        "SystemControlSuccessTextForegroundBrush",
        "SystemControlWarningTextForegroundBrush",
    };

    [Fact]
    public void ViewXamlDoesNotReferenceRemovedWinUi2BrushKeys()
    {
        var offenders = new List<string>();

        foreach (var file in EnumerateSourceFiles("*.xaml"))
        {
            // Ignore comments: a key may legitimately be named in documentation.
            var text = StripXmlComments(File.ReadAllText(file));
            foreach (var key in RemovedWinUi2Keys)
            {
                if (text.Contains(key, StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetFileName(file)}: {key}");
                }
            }
        }

        Assert.True(
            offenders.Count == 0,
            "These XAML files reference WinUI 2 brush keys that do not exist in WinUI 3 "
            + "(loading the page throws): " + string.Join(", ", offenders));
    }

    private static string StripXmlComments(string xaml)
    {
        return Regex.Replace(xaml, "<!--.*?-->", string.Empty, RegexOptions.Singleline, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ViewCodeDoesNotIndexResourceDictionaryWithStringLiteral()
    {
        // The ResourceDictionary indexer throws COMException for an unknown key.
        // TryGetValue is the only safe way to probe for an optional resource.
        var indexerPattern = new Regex(
            @"Application\.Current\.Resources\s*\[\s*""",
            RegexOptions.CultureInvariant);

        var offenders = new List<string>();

        foreach (var file in EnumerateSourceFiles("*.cs"))
        {
            var text = File.ReadAllText(file);
            if (indexerPattern.IsMatch(text))
            {
                offenders.Add(Path.GetFileName(file));
            }
        }

        Assert.True(
            offenders.Count == 0,
            "These files index Application.Current.Resources with a string literal; use "
            + "TryGetValue (see ThemeBrush) so a missing key cannot terminate the app: "
            + string.Join(", ", offenders));
    }

    private static IEnumerable<string> EnumerateSourceFiles(string pattern)
    {
        var srcDir = Path.Combine(FindRepositoryRoot(), "src");
        Assert.True(Directory.Exists(srcDir), $"Source directory not found: {srcDir}");

        return Directory.EnumerateFiles(srcDir, pattern, SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src"))
                && Directory.Exists(Path.Combine(dir.FullName, "tests")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate the repository root starting from {AppContext.BaseDirectory}.");
    }
}
