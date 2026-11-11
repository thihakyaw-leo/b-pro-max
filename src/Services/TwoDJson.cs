using System.Text.Json;
using System.Text.Json.Serialization;
using BProMax.Models;

namespace BProMax.Services;

/// <summary>
/// Serializes the dealer-configurable rule options stored in <c>bet_types.rule_config</c>.
/// </summary>
public static class TwoDJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SerializeRuleConfig(NumberRuleConfig config)
    {
        return JsonSerializer.Serialize(config, Options);
    }

    public static NumberRuleConfig DeserializeRuleConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new NumberRuleConfig();
        }

        return JsonSerializer.Deserialize<NumberRuleConfig>(json, Options) ?? new NumberRuleConfig();
    }
}
