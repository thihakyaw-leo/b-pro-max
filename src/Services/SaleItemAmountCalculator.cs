using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Calculates the total stake and per-number stakes for one sale item.
/// </summary>
public class SaleItemAmountCalculator : ISaleItemAmountCalculator
{
    public bool TryCalculate(
        decimal amount,
        string amountMode,
        int numberCount,
        decimal payoutMultiplier,
        PayoutMode payoutMode,
        out SaleItemAmountResult result,
        out string? validationError)
    {
        if (amount <= 0)
        {
            result = new SaleItemAmountResult();
            validationError = "Amount must be greater than zero.";
            return false;
        }

        if (decimal.Round(amount, 2) != amount)
        {
            result = new SaleItemAmountResult();
            validationError = "Amount may have at most two decimal places.";
            return false;
        }

        if (numberCount <= 0)
        {
            result = new SaleItemAmountResult();
            validationError = "A sale item must resolve to at least one number.";
            return false;
        }

        if (payoutMultiplier <= 0)
        {
            result = new SaleItemAmountResult();
            validationError = "Payout multiplier must be greater than zero.";
            return false;
        }

        if (string.Equals(amountMode, BetAmountModes.PerNumber, StringComparison.Ordinal))
        {
            var total = RoundMoney(amount * numberCount);
            var stakes = new decimal[numberCount];
            for (var index = 0; index < numberCount; index++)
            {
                stakes[index] = amount;
            }

            result = new SaleItemAmountResult
            {
                Total = total,
                MaxPayout = payoutMode == PayoutMode.TotalIfHit
                    ? RoundMoney(total * payoutMultiplier)
                    : RoundMoney(amount * payoutMultiplier),
                Stakes = stakes
            };
            validationError = null;
            return true;
        }

        if (string.Equals(amountMode, BetAmountModes.TotalSplit, StringComparison.Ordinal))
        {
            var stakes = MoneySplitter.Split(amount, numberCount);
            result = new SaleItemAmountResult
            {
                Total = amount,
                MaxPayout = payoutMode == PayoutMode.TotalIfHit
                    ? RoundMoney(amount * payoutMultiplier)
                    : RoundMoney(stakes.Max() * payoutMultiplier),
                Stakes = stakes
            };
            validationError = null;
            return true;
        }

        if (string.Equals(amountMode, BetAmountModes.Total, StringComparison.Ordinal))
        {
            var stakes = new decimal[numberCount];
            result = new SaleItemAmountResult
            {
                Total = amount,
                MaxPayout = RoundMoney(amount * payoutMultiplier),
                Stakes = stakes
            };
            validationError = null;
            return true;
        }

        result = new SaleItemAmountResult();
        validationError = $"Unknown amount mode '{amountMode}'.";
        return false;
    }

    private static decimal RoundMoney(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
