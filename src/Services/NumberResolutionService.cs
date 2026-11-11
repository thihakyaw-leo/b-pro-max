using System.Globalization;
using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Resolves a bet type and its selection into the exact two-digit numbers that are sold.
/// </summary>
public class NumberResolutionService : INumberResolutionService
{
    public bool TryResolve(
        NumberResolutionRequest request,
        out NumberResolutionResult result,
        out string? validationError)
    {
        if (request is null)
        {
            result = new NumberResolutionResult();
            validationError = "A number resolution request is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RuleType))
        {
            result = new NumberResolutionResult();
            validationError = "A rule type is required.";
            return false;
        }

        try
        {
            result = ResolveCore(request);
            validationError = null;
            return true;
        }
        catch (FormatException exception)
        {
            result = new NumberResolutionResult();
            validationError = exception.Message;
            return false;
        }
    }

    private static NumberResolutionResult ResolveCore(NumberResolutionRequest request)
    {
        return request.RuleType switch
        {
            NumberRuleTypes.Exact => ResolveExact(request),
            NumberRuleTypes.ExactAndReverse => ResolveExactAndReverse(request),
            NumberRuleTypes.DigitInEitherPosition => ResolveDigitInEitherPosition(request),
            NumberRuleTypes.DigitInPosition => ResolveDigitInPosition(request),
            NumberRuleTypes.DigitSum => ResolveDigitSum(request),
            NumberRuleTypes.FixedGroup => ResolveFixedGroup(request),
            NumberRuleTypes.UnorderedPair => ResolveUnorderedPair(request),
            NumberRuleTypes.Doubles => ResolveDoubles(request),
            NumberRuleTypes.Parity => ResolveParity(request),
            NumberRuleTypes.Range => ResolveRange(request),
            _ => throw new FormatException($"Unknown rule type '{request.RuleType}'.")
        };
    }

    private static NumberResolutionResult ResolveExact(NumberResolutionRequest request)
    {
        var number = RequireTwoDigitNumber(request.Selection, "number");
        return CreateResult([number], NumberMatchMode.Exact);
    }

    private static NumberResolutionResult ResolveExactAndReverse(NumberResolutionRequest request)
    {
        var number = RequireTwoDigitNumber(request.Selection, "number");
        return CreateResult(ExpandReverse([number]), NumberMatchMode.Exact);
    }

    private static NumberResolutionResult ResolveDigitInEitherPosition(NumberResolutionRequest request)
    {
        var digit = RequireDigit(request.Selection, "digit");

        var numbers = new List<string>(19);
        for (var tens = 0; tens <= 9; tens++)
        {
            for (var ones = 0; ones <= 9; ones++)
            {
                if (tens != digit && ones != digit)
                {
                    continue;
                }

                if (request.RuleConfig.ExcludeDoubles && tens == ones)
                {
                    continue;
                }

                numbers.Add(NumberAt(tens, ones));
            }
        }

        return CreateResult(Finish(numbers, request.IncludeReverse), NumberMatchMode.Exact);
    }

    private static NumberResolutionResult ResolveDigitInPosition(NumberResolutionRequest request)
    {
        var digit = RequireDigit(request.Selection, "digit");

        var numbers = new List<string>(10);
        for (var otherDigit = 0; otherDigit <= 9; otherDigit++)
        {
            numbers.Add(request.RuleConfig.Position == NumberPosition.Tens
                ? NumberAt(digit, otherDigit)
                : NumberAt(otherDigit, digit));
        }

        return CreateResult(Finish(numbers, request.IncludeReverse), NumberMatchMode.Exact);
    }

    private static NumberResolutionResult ResolveDigitSum(NumberResolutionRequest request)
    {
        var digit = RequireDigit(request.Selection, "digit");

        var numbers = new List<string>(10);
        for (var tens = 0; tens <= 9; tens++)
        {
            for (var ones = 0; ones <= 9; ones++)
            {
                if ((tens + ones) % 10 != digit)
                {
                    continue;
                }

                if (request.RuleConfig.ExcludeDoubles && tens == ones)
                {
                    continue;
                }

                numbers.Add(NumberAt(tens, ones));
            }
        }

        return CreateResult(Finish(numbers, request.IncludeReverse), NumberMatchMode.Exact);
    }

    private static NumberResolutionResult ResolveFixedGroup(NumberResolutionRequest request)
    {
        var source = request.FixedNumbers ?? request.RuleConfig.Numbers;
        if (source is null || source.Count == 0)
        {
            throw new FormatException("A fixed group must contain at least one number.");
        }

        var numbers = new List<string>(source.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in source)
        {
            var number = RequireTwoDigitNumber(value, "fixed-group number");
            if (seen.Add(number))
            {
                numbers.Add(number);
            }
        }

        return CreateResult(Finish(numbers, request.IncludeReverse), NumberMatchMode.Exact);
    }

    private static NumberResolutionResult ResolveUnorderedPair(NumberResolutionRequest request)
    {
        return ResolveParitySet(request, ordered: false);
    }

    private static NumberResolutionResult ResolveParity(NumberResolutionRequest request)
    {
        return ResolveParitySet(request, request.RuleConfig.Ordered);
    }

    private static NumberResolutionResult ResolveParitySet(NumberResolutionRequest request, bool ordered)
    {
        var config = request.RuleConfig;
        if (config.TensParity is null && config.OnesParity is null)
        {
            throw new FormatException("A parity rule must define the required parity.");
        }

        if (!ordered && (config.TensParity is null || config.OnesParity is null))
        {
            throw new FormatException("An unordered parity rule must define both digit parities.");
        }

        var numbers = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var tens = 0; tens <= 9; tens++)
        {
            for (var ones = 0; ones <= 9; ones++)
            {
                if (ordered)
                {
                    if (!MatchesParity(tens, config.TensParity) || !MatchesParity(ones, config.OnesParity))
                    {
                        continue;
                    }

                    numbers.Add(NumberAt(tens, ones));
                    continue;
                }

                if (!MatchesUnorderedParity(tens, ones, config))
                {
                    continue;
                }

                var canonical = CanonicalPair(NumberAt(tens, ones));
                if (seen.Add(canonical))
                {
                    numbers.Add(canonical);
                }
            }
        }

        var matchMode = ordered ? NumberMatchMode.Exact : NumberMatchMode.UnorderedPair;
        return CreateResult(numbers.ToArray(), matchMode);
    }

    private static bool MatchesUnorderedParity(int tens, int ones, NumberRuleConfig config)
    {
        if (config.TensParity == config.OnesParity)
        {
            return MatchesParity(tens, config.TensParity) && MatchesParity(ones, config.TensParity);
        }

        return (MatchesParity(tens, config.TensParity) && MatchesParity(ones, config.OnesParity))
            || (MatchesParity(tens, config.OnesParity) && MatchesParity(ones, config.TensParity));
    }

    private static NumberResolutionResult ResolveDoubles(NumberResolutionRequest request)
    {
        var config = request.RuleConfig;
        var numbers = new List<string>(10);
        for (var digit = 0; digit <= 9; digit++)
        {
            if (!MatchesParity(digit, config.TensParity) || !MatchesParity(digit, config.OnesParity))
            {
                continue;
            }

            numbers.Add(NumberAt(digit, digit));
        }

        return CreateResult(numbers.ToArray(), NumberMatchMode.Exact);
    }

    private static NumberResolutionResult ResolveRange(NumberResolutionRequest request)
    {
        var config = request.RuleConfig;
        if (config.Minimum < 0 || config.Maximum > 99 || config.Minimum > config.Maximum)
        {
            throw new FormatException("A number range must be between 00 and 99.");
        }

        var numbers = new List<string>(config.Maximum - config.Minimum + 1);
        for (var value = config.Minimum; value <= config.Maximum; value++)
        {
            var tens = value / 10;
            var ones = value % 10;

            if (!MatchesParity(tens, config.TensParity) || !MatchesParity(ones, config.OnesParity))
            {
                continue;
            }

            if (config.ExcludeDoubles && tens == ones)
            {
                continue;
            }

            numbers.Add(NumberAt(tens, ones));
        }

        return CreateResult(numbers.ToArray(), NumberMatchMode.Exact);
    }

    private static string[] Finish(List<string> numbers, bool includeReverse)
    {
        return includeReverse ? ExpandReverse(numbers) : numbers.ToArray();
    }

    private static string[] ExpandReverse(IEnumerable<string> numbers)
    {
        var expanded = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var number in numbers)
        {
            if (seen.Add(number))
            {
                expanded.Add(number);
            }

            var reverse = Reverse(number);
            if (seen.Add(reverse))
            {
                expanded.Add(reverse);
            }
        }

        return expanded.ToArray();
    }

    private static string RequireTwoDigitNumber(string? value, string label)
    {
        var trimmed = value?.Trim();
        if (trimmed is null || trimmed.Length != 2 || !char.IsAsciiDigit(trimmed[0]) || !char.IsAsciiDigit(trimmed[1]))
        {
            throw new FormatException($"The {label} must be exactly two digits from 00 to 99.");
        }

        return trimmed;
    }

    private static int RequireDigit(string? value, string label)
    {
        var trimmed = value?.Trim();
        if (trimmed is null || trimmed.Length != 1 || !char.IsAsciiDigit(trimmed[0]))
        {
            throw new FormatException($"The {label} must be a single digit from 0 to 9.");
        }

        return trimmed[0] - '0';
    }

    private static bool MatchesParity(int digit, NumberParity? parity)
    {
        if (parity is null)
        {
            return true;
        }

        var isEven = digit % 2 == 0;
        return isEven == (parity == NumberParity.Even);
    }

    private static string Reverse(string number)
    {
        return new string([number[1], number[0]]);
    }

    private static string CanonicalPair(string number)
    {
        return number[0] <= number[1]
            ? number
            : new string([number[1], number[0]]);
    }

    private static string NumberAt(int tens, int ones)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{tens}{ones}");
    }

    private static NumberResolutionResult CreateResult(string[] numbers, NumberMatchMode matchMode)
    {
        return new NumberResolutionResult
        {
            Numbers = numbers,
            MatchMode = matchMode
        };
    }
}
