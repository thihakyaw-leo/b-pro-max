using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Builds a validated preview for a batch of direct or R numbers.
/// </summary>
public class BatchSaleBuilder : IBatchSaleBuilder
{
    private readonly INumberBatchParser _parser;

    public BatchSaleBuilder(INumberBatchParser parser)
    {
        _parser = parser;
    }

    public bool TryBuild(BatchSaleRequest request, out BatchSalePreview preview, out string? validationError)
    {
        if (request is null)
        {
            preview = new BatchSalePreview();
            validationError = "A batch request is required.";
            return false;
        }

        if (request.Amount <= 0)
        {
            preview = new BatchSalePreview();
            validationError = "Amount must be greater than zero.";
            return false;
        }

        if (decimal.Round(request.Amount, 2) != request.Amount)
        {
            preview = new BatchSalePreview();
            validationError = "Amount may have at most two decimal places.";
            return false;
        }

        if (request.PayoutMultiplier <= 0)
        {
            preview = new BatchSalePreview();
            validationError = "Payout multiplier must be greater than zero.";
            return false;
        }

        if (request.MaxNumbers <= 0)
        {
            preview = new BatchSalePreview();
            validationError = "Maximum batch size must be greater than zero.";
            return false;
        }

        IReadOnlyList<string> numbers;
        try
        {
            numbers = _parser.Parse(request.NumbersInput);
        }
        catch (FormatException exception)
        {
            preview = new BatchSalePreview();
            validationError = exception.Message;
            return false;
        }

        if (numbers.Count > request.MaxNumbers)
        {
            preview = new BatchSalePreview();
            validationError = $"A batch may contain at most {request.MaxNumbers} numbers.";
            return false;
        }

        var warnings = new List<string>();
        var groups = BuildGroups(numbers, request.IncludeReverse, warnings);
        var groupAmounts = AllocateAmounts(groups, numbers.Count, request, out var totalStake);

        var lines = new List<BatchSaleLine>(groups.Count);
        foreach (var group in groups)
        {
            var lineAmount = groupAmounts[group.Key];
            var stakes = MoneySplitter.Split(lineAmount, group.ResolvedNumbers.Count);

            var resolved = new List<BatchSaleNumber>(group.ResolvedNumbers.Count);
            for (var index = 0; index < group.ResolvedNumbers.Count; index++)
            {
                resolved.Add(new BatchSaleNumber
                {
                    Number = group.ResolvedNumbers[index],
                    Stake = stakes[index],
                    IsReverse = index > 0
                });
            }

            var maxStake = resolved.Max(number => number.Stake);
            lines.Add(new BatchSaleLine
            {
                BaseNumber = group.BaseNumber,
                Numbers = resolved,
                Amount = lineAmount,
                MaxPayout = maxStake * request.PayoutMultiplier
            });
        }

        preview = new BatchSalePreview
        {
            Lines = lines,
            TotalStake = totalStake,
            Warnings = warnings
        };
        validationError = null;
        return true;
    }

    private static List<BatchGroup> BuildGroups(
        IReadOnlyList<string> numbers,
        bool includeReverse,
        List<string> warnings)
    {
        var groups = new List<BatchGroup>();
        var groupIndexes = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var number in numbers)
        {
            var key = includeReverse ? CanonicalPair(number) : number;
            if (groupIndexes.TryGetValue(key, out var groupIndex))
            {
                groups[groupIndex].Occurrences++;
                warnings.Add($"Duplicate {key} merged.");
                continue;
            }

            groupIndexes[key] = groups.Count;
            groups.Add(new BatchGroup(
                key,
                number,
                BuildResolvedNumbers(number, includeReverse)));
        }

        return groups;
    }

    private static Dictionary<string, decimal> AllocateAmounts(
        IReadOnlyList<BatchGroup> groups,
        int totalOccurrences,
        BatchSaleRequest request,
        out decimal totalStake)
    {
        var groupAmounts = new Dictionary<string, decimal>(StringComparer.Ordinal);

        if (request.AmountMode == BatchAmountMode.PerBaseNumber)
        {
            foreach (var group in groups)
            {
                groupAmounts[group.Key] = request.Amount * group.Occurrences;
            }

            totalStake = request.Amount * totalOccurrences;
            return groupAmounts;
        }

        var occurrenceAmounts = MoneySplitter.Split(request.Amount, totalOccurrences);
        var occurrenceIndex = 0;
        foreach (var group in groups)
        {
            groupAmounts[group.Key] = 0m;
        }

        foreach (var group in groups)
        {
            for (var index = 0; index < group.Occurrences; index++)
            {
                groupAmounts[group.Key] += occurrenceAmounts[occurrenceIndex];
                occurrenceIndex++;
            }
        }

        totalStake = request.Amount;
        return groupAmounts;
    }

    private static string[] BuildResolvedNumbers(string baseNumber, bool includeReverse)
    {
        if (!includeReverse)
        {
            return new[] { baseNumber };
        }

        var reverse = new string([baseNumber[1], baseNumber[0]]);
        if (string.Equals(reverse, baseNumber, StringComparison.Ordinal))
        {
            return new[] { baseNumber };
        }

        return new[] { baseNumber, reverse };
    }

    private static string CanonicalPair(string number)
    {
        var first = number[0];
        var second = number[1];
        return first <= second
            ? new string([first, second])
            : new string([second, first]);
    }

    private sealed class BatchGroup
    {
        public BatchGroup(string key, string baseNumber, IReadOnlyList<string> resolvedNumbers)
        {
            Key = key;
            BaseNumber = baseNumber;
            ResolvedNumbers = resolvedNumbers;
        }

        public string Key { get; }
        public string BaseNumber { get; }
        public IReadOnlyList<string> ResolvedNumbers { get; }
        public int Occurrences { get; set; } = 1;
    }
}
