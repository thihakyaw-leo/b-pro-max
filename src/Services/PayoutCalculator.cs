using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Calculates the payout of one sale item for a given draw result.
/// </summary>
public class PayoutCalculator : IPayoutCalculator
{
    public bool TryCalculate(
        PayoutCalculationRequest request,
        out PayoutCalculationResult result,
        out string? validationError)
    {
        if (request is null)
        {
            result = new PayoutCalculationResult();
            validationError = "A payout request is required.";
            return false;
        }

        if (!IsTwoDigitNumber(request.ResultNumber))
        {
            result = new PayoutCalculationResult();
            validationError = "The draw result must be exactly two digits from 00 to 99.";
            return false;
        }

        if (request.PayoutMultiplier <= 0)
        {
            result = new PayoutCalculationResult();
            validationError = "The payout multiplier must be greater than zero.";
            return false;
        }

        if (request.Numbers is null)
        {
            result = new PayoutCalculationResult();
            validationError = "Resolved numbers are required.";
            return false;
        }

        foreach (var number in request.Numbers)
        {
            if (!IsTwoDigitNumber(number.Number))
            {
                result = new PayoutCalculationResult();
                validationError = "Every resolved number must be exactly two digits from 00 to 99.";
                return false;
            }

            if (number.Stake < 0)
            {
                result = new PayoutCalculationResult();
                validationError = "A stake cannot be negative.";
                return false;
            }
        }

        if (request.PayoutMode == PayoutMode.TotalIfHit)
        {
            return CalculateTotalIfHit(request, out result, out validationError);
        }

        return CalculatePerWinningNumber(request, out result, out validationError);
    }

    private static bool CalculatePerWinningNumber(
        PayoutCalculationRequest request,
        out PayoutCalculationResult result,
        out string? validationError)
    {
        var winningNumbers = new List<PayoutCalculationLine>();
        var totalPayout = 0m;

        foreach (var number in request.Numbers)
        {
            if (!Matches(number.Number, request.ResultNumber, request.MatchMode))
            {
                continue;
            }

            var payout = RoundMoney(number.Stake * request.PayoutMultiplier);
            totalPayout += payout;
            winningNumbers.Add(new PayoutCalculationLine
            {
                Number = number.Number,
                Stake = number.Stake,
                IsReverse = number.IsReverse,
                Payout = payout
            });
        }

        result = new PayoutCalculationResult
        {
            IsWinner = winningNumbers.Count > 0,
            TotalPayout = RoundMoney(totalPayout),
            WinningNumbers = winningNumbers
        };
        validationError = null;
        return true;
    }

    private static bool CalculateTotalIfHit(
        PayoutCalculationRequest request,
        out PayoutCalculationResult result,
        out string? validationError)
    {
        if (request.ItemAmount <= 0)
        {
            result = new PayoutCalculationResult();
            validationError = "The item amount must be greater than zero.";
            return false;
        }

        var winningNumbers = new List<PayoutCalculationLine>();
        foreach (var number in request.Numbers)
        {
            if (!Matches(number.Number, request.ResultNumber, request.MatchMode))
            {
                continue;
            }

            winningNumbers.Add(new PayoutCalculationLine
            {
                Number = number.Number,
                Stake = number.Stake,
                IsReverse = number.IsReverse,
                Payout = 0m
            });
        }

        var isWinner = winningNumbers.Count > 0;
        result = new PayoutCalculationResult
        {
            IsWinner = isWinner,
            TotalPayout = isWinner ? RoundMoney(request.ItemAmount * request.PayoutMultiplier) : 0m,
            WinningNumbers = winningNumbers
        };
        validationError = null;
        return true;
    }

    private static bool Matches(string number, string resultNumber, NumberMatchMode matchMode)
    {
        if (matchMode == NumberMatchMode.UnorderedPair)
        {
            return string.Equals(Canonical(number), Canonical(resultNumber), StringComparison.Ordinal);
        }

        return string.Equals(number, resultNumber, StringComparison.Ordinal);
    }

    private static string Canonical(string number)
    {
        return number[0] <= number[1]
            ? number
            : new string([number[1], number[0]]);
    }

    private static bool IsTwoDigitNumber(string? value)
    {
        return value is { Length: 2 } && char.IsAsciiDigit(value[0]) && char.IsAsciiDigit(value[1]);
    }

    private static decimal RoundMoney(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
