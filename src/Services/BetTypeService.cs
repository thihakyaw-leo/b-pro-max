using BProMax.Models;
using Npgsql;

namespace BProMax.Services;

/// <summary>
/// Reads the dealer-configurable bet types and fixed number groups.
/// </summary>
public class BetTypeService : IBetTypeService
{
    private readonly IDatabaseService _db;

    public BetTypeService(IDatabaseService db)
    {
        _db = db;
    }

    public async Task<List<BetTypeRecord>> GetBetTypesAsync(bool activeOnly = true)
    {
        const string sql = """
SELECT id, code, name_en, name_my, rule_type, rule_config::text,
       amount_mode, payout_mode, payout_multiplier, is_active, sort_order,
       created_at, updated_at
FROM bet_types
WHERE (@active_only = FALSE OR is_active = TRUE)
ORDER BY sort_order, code;
""";

        return await _db.QueryListAsync(sql, MapBetType,
            new NpgsqlParameter("@active_only", activeOnly));
    }

    public Task<BetTypeRecord?> GetBetTypeByCodeAsync(string code)
    {
        const string sql = """
SELECT id, code, name_en, name_my, rule_type, rule_config::text,
       amount_mode, payout_mode, payout_multiplier, is_active, sort_order,
       created_at, updated_at
FROM bet_types
WHERE code = @code;
""";

        return _db.QuerySingleAsync(sql, MapBetType,
            new NpgsqlParameter("@code", code));
    }

    public async Task<List<NumberGroupRecord>> GetNumberGroupsAsync(bool activeOnly = true)
    {
        const string sql = """
SELECT id, code, name_en, name_my, allow_r, is_active, sort_order,
       created_at, updated_at
FROM number_groups
WHERE (@active_only = FALSE OR is_active = TRUE)
ORDER BY sort_order, code;
""";

        var groups = await _db.QueryListAsync(sql, MapNumberGroup,
            new NpgsqlParameter("@active_only", activeOnly));

        foreach (var group in groups)
        {
            group.Members = await GetMembersAsync(group.Id);
        }

        return groups;
    }

    public async Task<NumberGroupRecord?> GetNumberGroupByCodeAsync(string code)
    {
        const string sql = """
SELECT id, code, name_en, name_my, allow_r, is_active, sort_order,
       created_at, updated_at
FROM number_groups
WHERE code = @code;
""";

        var group = await _db.QuerySingleAsync(sql, MapNumberGroup,
            new NpgsqlParameter("@code", code));

        if (group is not null)
        {
            group.Members = await GetMembersAsync(group.Id);
        }

        return group;
    }

    public Task<int> UpdateBetTypeAsync(
        int id,
        decimal payoutMultiplier,
        bool isActive,
        int sortOrder)
    {
        if (payoutMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payoutMultiplier),
                payoutMultiplier,
                "The payout multiplier must be greater than zero.");
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                sortOrder,
                "The sort order cannot be negative.");
        }

        const string sql = """
UPDATE bet_types
SET payout_multiplier = @payout_multiplier,
    is_active = @is_active,
    sort_order = @sort_order,
    updated_at = NOW()
WHERE id = @id;
""";

        return _db.ExecuteNonQueryAsync(
            sql,
            new NpgsqlParameter("@payout_multiplier", payoutMultiplier),
            new NpgsqlParameter("@is_active", isActive),
            new NpgsqlParameter("@sort_order", sortOrder),
            new NpgsqlParameter("@id", id));
    }

    public Task<int> UpdateNumberGroupAsync(
        int id,
        bool allowReverse,
        bool isActive,
        int sortOrder,
        IReadOnlyList<string> members)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                sortOrder,
                "The sort order cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(members);

        var normalized = members
            .Select(member => member.Trim())
            .Where(member => member.Length > 0)
            .ToList();

        foreach (var member in normalized)
        {
            if (member.Length != 2 || !char.IsAsciiDigit(member[0]) || !char.IsAsciiDigit(member[1]))
            {
                throw new ArgumentException(
                    $"'{member}' is not a two-digit number from 00 to 99.",
                    nameof(members));
            }
        }

        if (normalized.Count == 0)
        {
            throw new ArgumentException("A group must contain at least one number.", nameof(members));
        }

        if (normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Count)
        {
            throw new ArgumentException("A group cannot contain duplicate numbers.", nameof(members));
        }

        return _db.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            await using (var update = new NpgsqlCommand(
                """
UPDATE number_groups
SET allow_r = @allow_r,
    is_active = @is_active,
    sort_order = @sort_order,
    updated_at = NOW()
WHERE id = @id;
""",
                connection,
                transaction))
            {
                update.Parameters.AddWithValue("@allow_r", allowReverse);
                update.Parameters.AddWithValue("@is_active", isActive);
                update.Parameters.AddWithValue("@sort_order", sortOrder);
                update.Parameters.AddWithValue("@id", id);
                await update.ExecuteNonQueryAsync();
            }

            await using (var delete = new NpgsqlCommand(
                "DELETE FROM number_group_members WHERE group_id = @group_id;",
                connection,
                transaction))
            {
                delete.Parameters.AddWithValue("@group_id", id);
                await delete.ExecuteNonQueryAsync();
            }

            for (var index = 0; index < normalized.Count; index++)
            {
                await using var insert = new NpgsqlCommand(
                    """
INSERT INTO number_group_members (group_id, number, sort_order)
VALUES (@group_id, @number, @sort_order);
""",
                    connection,
                    transaction);
                insert.Parameters.AddWithValue("@group_id", id);
                insert.Parameters.AddWithValue("@number", normalized[index]);
                insert.Parameters.AddWithValue("@sort_order", index);
                await insert.ExecuteNonQueryAsync();
            }

            return normalized.Count;
        });
    }

    private async Task<List<string>> GetMembersAsync(int groupId)
    {
        const string sql = """
SELECT number
FROM number_group_members
WHERE group_id = @group_id
ORDER BY sort_order, number;
""";

        return await _db.QueryListAsync(sql, reader => reader.GetString(0),
            new NpgsqlParameter("@group_id", groupId));
    }

    private static BetTypeRecord MapBetType(NpgsqlDataReader reader)
    {
        return new BetTypeRecord
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Code = reader.GetString(reader.GetOrdinal("code")),
            NameEn = reader.GetString(reader.GetOrdinal("name_en")),
            NameMy = reader.GetString(reader.GetOrdinal("name_my")),
            RuleType = reader.GetString(reader.GetOrdinal("rule_type")),
            RuleConfigJson = reader.GetString(reader.GetOrdinal("rule_config")),
            AmountMode = reader.GetString(reader.GetOrdinal("amount_mode")),
            PayoutMode = reader.GetString(reader.GetOrdinal("payout_mode")),
            PayoutMultiplier = reader.GetDecimal(reader.GetOrdinal("payout_multiplier")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    private static NumberGroupRecord MapNumberGroup(NpgsqlDataReader reader)
    {
        return new NumberGroupRecord
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Code = reader.GetString(reader.GetOrdinal("code")),
            NameEn = reader.GetString(reader.GetOrdinal("name_en")),
            NameMy = reader.GetString(reader.GetOrdinal("name_my")),
            AllowReverse = reader.GetBoolean(reader.GetOrdinal("allow_r")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            SortOrder = reader.GetInt32(reader.GetOrdinal("sort_order")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }
}
