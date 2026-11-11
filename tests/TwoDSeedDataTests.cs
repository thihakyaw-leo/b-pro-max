using BProMax.Models;
using BProMax.Services;
using Xunit;

namespace BProMax.Tests;

public class TwoDSeedDataTests
{
    private static readonly string[] ApprovedSalePanelLabels =
    [
        "R", "ပါ", "ဘရိတ်", "ထိပ်", "နောက်", "စုံစုံ", "မမ", "စုံမ", "ပါဝါ", "နက္ခတ်"
    ];

    private static readonly string[] ForbiddenSalePanelNames =
    [
        "Apa", "Break", "Top", "Bottom", "Direct"
    ];

    private static readonly string[] ExpectedNakkhatMembers = ["07", "18", "24", "35", "69"];
    private static readonly string[] ExpectedPowerMembers = ["05", "16", "27", "38", "49"];

    private readonly NumberResolutionService _resolutionService = new();

    [Theory]
    [InlineData("DIRECT", "45", 1)]
    [InlineData("R", "45", 2)]
    [InlineData("APA", "5", 19)]
    [InlineData("BRAKE", "5", 10)]
    [InlineData("TOP", "5", 10)]
    [InlineData("BOTTOM", "5", 10)]
    [InlineData("EVEN_EVEN", null, 15)]
    [InlineData("ODD_ODD", null, 15)]
    [InlineData("MIXED", null, 25)]
    [InlineData("POWER_GROUP", null, 5)]
    [InlineData("NAKKHAT", null, 5)]
    public void SeedBetTypeResolvesToTheExpectedNumberCount(string code, string? selection, int expectedCount)
    {
        var betType = TwoDSeedData.BetTypes.Single(type => type.Code == code);
        var group = TwoDSeedData.NumberGroups.FirstOrDefault(item => item.Code == code);

        var request = new NumberResolutionRequest
        {
            RuleType = betType.RuleType,
            RuleConfig = TwoDJson.DeserializeRuleConfig(betType.RuleConfigJson),
            Selection = selection,
            FixedNumbers = group?.Members
        };

        var success = _resolutionService.TryResolve(request, out var result, out var validationError);

        Assert.True(success);
        Assert.Null(validationError);
        Assert.Equal(expectedCount, result.Numbers.Count);
    }

    [Fact]
    public void SeedContainsExactlyTheApprovedSalePanelLabels()
    {
        var labels = TwoDSeedData.BetTypes
            .Where(type => !string.Equals(type.Code, "DIRECT", StringComparison.Ordinal))
            .OrderBy(type => type.SortOrder)
            .Select(type => type.NameMy)
            .ToArray();

        Assert.Equal(ApprovedSalePanelLabels, labels);
    }

    [Fact]
    public void SeedDoesNotUseEnglishSalePanelLabels()
    {
        var labels = TwoDSeedData.BetTypes.Select(type => type.NameMy).ToArray();

        Assert.DoesNotContain(labels, label =>
            ForbiddenSalePanelNames.Contains(label, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void NakkhatSeedMatchesTheApprovedGroup()
    {
        var group = TwoDSeedData.NumberGroups.Single(item => item.Code == "NAKKHAT");

        Assert.Equal("နက္ခတ်", group.NameMy);
        Assert.True(group.AllowReverse);
        Assert.Equal(ExpectedNakkhatMembers, group.Members.ToArray());
    }

    [Fact]
    public void PowerSeedMatchesTheApprovedGroup()
    {
        var group = TwoDSeedData.NumberGroups.Single(item => item.Code == "POWER_GROUP");

        Assert.Equal("ပါဝါ", group.NameMy);
        Assert.True(group.AllowReverse);
        Assert.Equal(ExpectedPowerMembers, group.Members.ToArray());
    }

    [Fact]
    public void RuleConfigRoundTripsThroughJson()
    {
        var original = new NumberRuleConfig
        {
            Position = NumberPosition.Ones,
            TensParity = NumberParity.Odd,
            OnesParity = NumberParity.Even,
            Ordered = false,
            Numbers = ["12", "34"],
            Minimum = 10,
            Maximum = 90,
            ExcludeDoubles = true
        };

        var json = TwoDJson.SerializeRuleConfig(original);
        var restored = TwoDJson.DeserializeRuleConfig(json);

        Assert.Equal(original.Position, restored.Position);
        Assert.Equal(original.TensParity, restored.TensParity);
        Assert.Equal(original.OnesParity, restored.OnesParity);
        Assert.Equal(original.Ordered, restored.Ordered);
        Assert.Equal(original.Numbers, restored.Numbers);
        Assert.Equal(original.Minimum, restored.Minimum);
        Assert.Equal(original.Maximum, restored.Maximum);
        Assert.Equal(original.ExcludeDoubles, restored.ExcludeDoubles);
    }
}
