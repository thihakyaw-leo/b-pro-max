namespace BProMax.Services;

/// <summary>
/// Splits money into two-decimal parts without losing a cent.
/// </summary>
public static class MoneySplitter
{
    /// <summary>
    /// Splits <paramref name="total"/> into <paramref name="count"/> parts. The remainder is
    /// assigned one cent at a time to the first parts so the sum equals the total exactly.
    /// </summary>
    public static decimal[] Split(decimal total, int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "The part count must be greater than zero.");
        }

        var roundedTotal = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
        var baseAmount = decimal.Round(roundedTotal / count, 2, MidpointRounding.ToZero);
        var amounts = new decimal[count];
        for (var index = 0; index < count; index++)
        {
            amounts[index] = baseAmount;
        }

        var remainder = roundedTotal - (baseAmount * count);
        var cents = (int)decimal.Round(remainder * 100m, 0, MidpointRounding.AwayFromZero);
        for (var index = 0; index < cents && index < count; index++)
        {
            amounts[index] += 0.01m;
        }

        return amounts;
    }
}
