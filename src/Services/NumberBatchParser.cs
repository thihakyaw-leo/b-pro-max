using System.Text;

namespace BProMax.Services;

/// <summary>
/// Formats and parses the continuous two-digit number list used by the Sale Panel batch entry.
/// </summary>
public class NumberBatchParser : INumberBatchParser
{
    public string Format(string? input, bool appendTrailingSeparator = false)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var digits = new StringBuilder(input.Length);
        foreach (var character in input)
        {
            if (char.IsAsciiDigit(character))
            {
                digits.Append(character);
            }
            else if (!IsSeparator(character))
            {
                // Leave invalid input untouched so Parse can report the exact problem.
                return input;
            }
        }

        if (digits.Length == 0)
        {
            return string.Empty;
        }

        var formatted = new StringBuilder(digits.Length + (digits.Length / 2 * 2));
        for (var index = 0; index < digits.Length; index += 2)
        {
            if (index > 0)
            {
                formatted.Append(", ");
            }

            formatted.Append(digits[index]);
            if (index + 1 < digits.Length)
            {
                formatted.Append(digits[index + 1]);
            }
        }

        if (appendTrailingSeparator && digits.Length % 2 == 0)
        {
            formatted.Append(", ");
        }

        return formatted.ToString();
    }

    public IReadOnlyList<string> Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new FormatException("Enter at least one two-digit number.");
        }

        var numbers = new List<string>();
        var token = new StringBuilder(2);

        foreach (var character in input)
        {
            if (char.IsAsciiDigit(character))
            {
                token.Append(character);
                continue;
            }

            if (!IsSeparator(character))
            {
                throw new FormatException("Numbers may contain only digits and separators.");
            }

            FlushToken(token, numbers);
        }

        FlushToken(token, numbers);

        if (numbers.Count == 0)
        {
            throw new FormatException("Enter at least one two-digit number.");
        }

        return numbers;
    }

    public bool TryParse(string? input, out IReadOnlyList<string> numbers, out string? validationError)
    {
        try
        {
            numbers = Parse(input);
            validationError = null;
            return true;
        }
        catch (FormatException exception)
        {
            numbers = Array.Empty<string>();
            validationError = exception.Message;
            return false;
        }
    }

    private static void FlushToken(StringBuilder token, List<string> numbers)
    {
        if (token.Length == 0)
        {
            return;
        }

        if (token.Length % 2 != 0)
        {
            throw new FormatException($"'{token}' is not a valid two-digit number.");
        }

        for (var index = 0; index < token.Length; index += 2)
        {
            numbers.Add(token.ToString(index, 2));
        }

        token.Clear();
    }

    private static bool IsSeparator(char character)
    {
        return character is ',' or ';' or '|' or ' ' or '\t' or '\r' or '\n';
    }
}
