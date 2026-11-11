using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// The default bet types and fixed groups installed on first run.
/// These values are data: dealers can change them through the Number Wisdom page.
/// </summary>
public static class TwoDSeedData
{
    public static IReadOnlyList<BetTypeRecord> BetTypes { get; } =
    [
        new BetTypeRecord
        {
            Code = "DIRECT",
            NameEn = "Direct",
            NameMy = "တိုက်ရိုက်",
            RuleType = NumberRuleTypes.Exact,
            RuleConfigJson = "{}",
            AmountMode = BetAmountModes.PerNumber,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 0
        },
        new BetTypeRecord
        {
            Code = "R",
            NameEn = "R",
            NameMy = "R",
            RuleType = NumberRuleTypes.ExactAndReverse,
            RuleConfigJson = "{}",
            AmountMode = BetAmountModes.TotalSplit,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 1
        },
        new BetTypeRecord
        {
            Code = "APA",
            NameEn = "Apa",
            NameMy = "ပါ",
            RuleType = NumberRuleTypes.DigitInEitherPosition,
            RuleConfigJson = "{}",
            AmountMode = BetAmountModes.TotalSplit,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 2
        },
        new BetTypeRecord
        {
            Code = "BRAKE",
            NameEn = "Brake",
            NameMy = "ဘရိတ်",
            RuleType = NumberRuleTypes.DigitSum,
            RuleConfigJson = "{}",
            AmountMode = BetAmountModes.TotalSplit,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 3
        },
        new BetTypeRecord
        {
            Code = "TOP",
            NameEn = "Top",
            NameMy = "ထိပ်",
            RuleType = NumberRuleTypes.DigitInPosition,
            RuleConfigJson = "{\"position\":\"Tens\"}",
            AmountMode = BetAmountModes.TotalSplit,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 4
        },
        new BetTypeRecord
        {
            Code = "BOTTOM",
            NameEn = "Bottom",
            NameMy = "နောက်",
            RuleType = NumberRuleTypes.DigitInPosition,
            RuleConfigJson = "{\"position\":\"Ones\"}",
            AmountMode = BetAmountModes.TotalSplit,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 5
        },
        new BetTypeRecord
        {
            Code = "EVEN_EVEN",
            NameEn = "Even-Even",
            NameMy = "စုံစုံ",
            RuleType = NumberRuleTypes.UnorderedPair,
            RuleConfigJson = "{\"tensParity\":\"Even\",\"onesParity\":\"Even\",\"ordered\":false}",
            AmountMode = BetAmountModes.TotalSplit,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 6
        },
        new BetTypeRecord
        {
            Code = "ODD_ODD",
            NameEn = "Odd-Odd",
            NameMy = "မမ",
            RuleType = NumberRuleTypes.UnorderedPair,
            RuleConfigJson = "{\"tensParity\":\"Odd\",\"onesParity\":\"Odd\",\"ordered\":false}",
            AmountMode = BetAmountModes.TotalSplit,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 7
        },
        new BetTypeRecord
        {
            Code = "MIXED",
            NameEn = "Mixed",
            NameMy = "စုံမ",
            RuleType = NumberRuleTypes.Parity,
            RuleConfigJson = "{\"tensParity\":\"Even\",\"onesParity\":\"Odd\",\"ordered\":true}",
            AmountMode = BetAmountModes.TotalSplit,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 8
        },
        new BetTypeRecord
        {
            Code = "POWER_GROUP",
            NameEn = "Power Group",
            NameMy = "ပါဝါ",
            RuleType = NumberRuleTypes.FixedGroup,
            RuleConfigJson = "{}",
            AmountMode = BetAmountModes.TotalSplit,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 9
        },
        new BetTypeRecord
        {
            Code = "NAKKHAT",
            NameEn = "Nakkhat",
            NameMy = "နက္ခတ်",
            RuleType = NumberRuleTypes.FixedGroup,
            RuleConfigJson = "{}",
            AmountMode = BetAmountModes.TotalSplit,
            PayoutMode = nameof(PayoutMode.PerWinningNumber),
            PayoutMultiplier = 80m,
            SortOrder = 10
        }
    ];

    public static IReadOnlyList<NumberGroupRecord> NumberGroups { get; } =
    [
        new NumberGroupRecord
        {
            Code = "NAKKHAT",
            NameEn = "Nakkhat",
            NameMy = "နက္ခတ်",
            AllowReverse = true,
            SortOrder = 1,
            Members = ["07", "18", "24", "35", "69"]
        },
        new NumberGroupRecord
        {
            Code = "POWER_GROUP",
            NameEn = "Power Group",
            NameMy = "ပါဝါ",
            AllowReverse = true,
            SortOrder = 2,
            Members = ["05", "16", "27", "38", "49"]
        }
    ];
}
