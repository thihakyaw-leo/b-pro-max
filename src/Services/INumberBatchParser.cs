namespace BProMax.Services;

/// <summary>
/// Formats and parses the continuous two-digit number list used by the Sale Panel batch entry.
/// </summary>
public interface INumberBatchParser
{
    /// <summary>
    /// Formats a number list with comma separators. Continuous digits are grouped in pairs.
    /// </summary>
    /// <param name="input">Raw input from the batch entry field.</param>
    /// <param name="appendTrailingSeparator">
    /// When true, appends a separator after a complete final pair so the user can keep typing.
    /// </param>
    string Format(string? input, bool appendTrailingSeparator = false);

    /// <summary>
    /// Parses a comma, space, semicolon, pipe, or newline separated list of two-digit numbers.
    /// Continuous digits (for example <c>453923</c>) are also accepted.
    /// </summary>
    /// <exception cref="FormatException">The input contains an invalid token.</exception>
    IReadOnlyList<string> Parse(string? input);

    /// <summary>
    /// Tries to parse a batch. Returns false and an error message when the input is invalid.
    /// </summary>
    bool TryParse(string? input, out IReadOnlyList<string> numbers, out string? validationError);
}
