using System.Globalization;
using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Builds a display row for the Sale Panel without touching the database.
/// </summary>
public class SalePanelLineFactory : ISalePanelLineFactory
{
    private readonly INumberResolutionService _resolutionService;
    private readonly ISaleItemAmountCalculator _amountCalculator;

    public SalePanelLineFactory(
        INumberResolutionService resolutionService,
        ISaleItemAmountCalculator amountCalculator)
    {
        _resolutionService = resolutionService;
        _amountCalculator = amountCalculator;
    }

    public bool TryCreate(
        SaleEntryItemRequest request,
        BetTypeRecord betType,
        NumberGroupRecord? group,
        out SalePanelLine line,
        out string? validationError)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(betType);

        var resolutionRequest = new NumberResolutionRequest
        {
            RuleType = betType.RuleType,
            RuleConfig = TwoDJson.DeserializeRuleConfig(betType.RuleConfigJson),
            Selection = request.Selection,
            FixedNumbers = group?.Members,
            IncludeReverse = request.IncludeReverse
        };

        if (!_resolutionService.TryResolve(resolutionRequest, out var resolution, out validationError))
        {
            line = new SalePanelLine();
            return false;
        }

        var payoutMode = ParsePayoutMode(betType.PayoutMode);
        decimal amount;
        decimal maxPayout;
        IReadOnlyList<decimal> stakes;

        if (request.NumberStakes is { Count: > 0 })
        {
            if (!TryBuildExplicitStakes(
                    request,
                    resolution.Numbers,
                    betType,
                    payoutMode,
                    out amount,
                    out maxPayout,
                    out var explicitStakes,
                    out validationError))
            {
                line = new SalePanelLine();
                return false;
            }

            stakes = resolution.Numbers.Select(number => explicitStakes[number]).ToArray();
        }
        else
        {
            if (!_amountCalculator.TryCalculate(
                    request.Amount,
                    betType.AmountMode,
                    resolution.Numbers.Count,
                    betType.PayoutMultiplier,
                    payoutMode,
                    out var amountResult,
                    out validationError))
            {
                line = new SalePanelLine();
                return false;
            }

            amount = amountResult.Total;
            maxPayout = amountResult.MaxPayout;
            stakes = amountResult.Stakes;
        }

        line = new SalePanelLine
        {
            BetTypeCode = betType.Code,
            Label = BuildLabel(betType, group),
            BetKey = group?.Code ?? request.Selection?.Trim() ?? betType.Code,
            NumbersText = string.Join(", ", resolution.Numbers),
            Amount = amount,
            PerNumberText = FormatStakes(stakes),
            MaxPayout = maxPayout,
            Request = request
        };
        validationError = null;
        return true;
    }

    private static bool TryBuildExplicitStakes(
        SaleEntryItemRequest request,
        IReadOnlyList<string> resolvedNumbers,
        BetTypeRecord betType,
        PayoutMode payoutMode,
        out decimal amount,
        out decimal maxPayout,
        out IReadOnlyDictionary<string, decimal> stakes,
        out string? validationError)
    {
        var allowed = new HashSet<string>(resolvedNumbers, StringComparer.Ordinal);
        var merged = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var number in request.NumberStakes!)
        {
            if (!allowed.Contains(number.Number))
            {
                amount = 0m;
                maxPayout = 0m;
                stakes = new Dictionary<string, decimal>(StringComparer.Ordinal);
                validationError = $"Number '{number.Number}' is not part of the resolved selection.";
                return false;
            }

            if (merged.TryGetValue(number.Number, out var existing))
            {
                merged[number.Number] = existing + number.Stake;
                continue;
            }

            merged[number.Number] = number.Stake;
        }

        if (merged.Count != resolvedNumbers.Count)
        {
            amount = 0m;
            maxPayout = 0m;
            stakes = new Dictionary<string, decimal>(StringComparer.Ordinal);
            validationError = "Every resolved number must have a stake.";
            return false;
        }

        amount = string.Equals(betType.AmountMode, BetAmountModes.Total, StringComparison.Ordinal)
            ? request.Amount
            : decimal.Round(merged.Values.Sum(), 2, MidpointRounding.AwayFromZero);

        if (!string.Equals(betType.AmountMode, BetAmountModes.Total, StringComparison.Ordinal)
            && amount != request.Amount)
        {
            maxPayout = 0m;
            stakes = new Dictionary<string, decimal>(StringComparer.Ordinal);
            validationError = $"The item amount for '{betType.NameMy}' does not match its number stakes.";
            return false;
        }

        maxPayout = payoutMode == PayoutMode.TotalIfHit
            ? decimal.Round(amount * betType.PayoutMultiplier, 2, MidpointRounding.AwayFromZero)
            : decimal.Round(merged.Values.Max() * betType.PayoutMultiplier, 2, MidpointRounding.AwayFromZero);
        stakes = merged;
        validationError = null;
        return true;
    }

    private static string BuildLabel(BetTypeRecord betType, NumberGroupRecord? group)
    {
        if (group is not null)
        {
            return group.NameMy;
        }

        return string.Equals(betType.Code, "DIRECT", StringComparison.Ordinal)
            ? "(base)"
            : betType.NameMy;
    }

    private static string FormatStakes(IReadOnlyList<decimal> stakes)
    {
        if (stakes.Count == 0)
        {
            return "0";
        }

        var min = stakes.Min();
        var max = stakes.Max();
        return min == max
            ? $"{min.ToString("N2", CultureInfo.InvariantCulture)} each"
            : $"{min.ToString("N2", CultureInfo.InvariantCulture)}–{max.ToString("N2", CultureInfo.InvariantCulture)}";
    }

    private static PayoutMode ParsePayoutMode(string value)
    {
        if (Enum.TryParse<PayoutMode>(value, ignoreCase: true, out var mode))
        {
            return mode;
        }

        throw new InvalidOperationException($"Unknown payout mode '{value}'.");
    }
}
