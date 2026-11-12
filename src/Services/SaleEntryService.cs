using BProMax.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace BProMax.Services;

/// <summary>
/// Creates a sale, its items, and its resolved numbers in one database transaction.
/// </summary>
public class SaleEntryService : ISaleEntryService
{
    private readonly IDatabaseService _db;
    private readonly INumberResolutionService _resolutionService;
    private readonly ISaleItemAmountCalculator _amountCalculator;
    private readonly ICommissionService _commissionService;
    private readonly ILogger<SaleEntryService> _logger;

    private static readonly Action<ILogger, int, long, int, Exception?> _saleCreatingLog =
        LoggerMessage.Define<int, long, int>(LogLevel.Information, new EventId(1, "SaleCreating"), "Creating sale for agent {AgentId}, draw {DrawId}, {ItemCount} item(s).");

    private static readonly Action<ILogger, long, decimal, decimal, Exception?> _saleCreatedLog =
        LoggerMessage.Define<long, decimal, decimal>(LogLevel.Information, new EventId(2, "SaleCreated"), "Sale {SaleId} created: stake {TotalStake}, commission {Commission}.");

    private static readonly Action<ILogger, int, Exception?> _saleCreateFailedLog =
        LoggerMessage.Define<int>(LogLevel.Error, new EventId(3, "SaleCreateFailed"), "Sale creation failed for agent {AgentId}.");

    private static readonly Action<ILogger, long, int?, decimal?, Exception?> _autoBuyTriggeredLog =
        LoggerMessage.Define<long, int?, decimal?>(LogLevel.Information, new EventId(4, "AutoBuyTriggered"), "Auto-buy triggered for sale {SaleId}: rule {RuleId}, amount {Amount}.");

    public SaleEntryService(
        IDatabaseService db,
        INumberResolutionService resolutionService,
        ISaleItemAmountCalculator amountCalculator,
        ICommissionService commissionService,
        ILogger<SaleEntryService>? logger = null)
    {
        _db = db;
        _resolutionService = resolutionService;
        _amountCalculator = amountCalculator;
        _commissionService = commissionService;
        _logger = logger ?? NullLogger<SaleEntryService>.Instance;
    }

    public async Task<SaleEntryResult> CreateSaleAsync(
        SaleEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        _saleCreatingLog(_logger, request.AgentId, request.DrawId, request.Items.Count, null);

        try
        {
            var result = await _db.ExecuteInTransactionAsync(
                (connection, transaction) => CreateSaleCoreAsync(connection, transaction, request, cancellationToken));

            _saleCreatedLog(_logger, result.SaleId, result.TotalStake, result.Commission + result.Commission1, null);

            return result;
        }
        catch (Exception ex)
        {
            _saleCreateFailedLog(_logger, request.AgentId, ex);
            throw;
        }
    }

    private async Task<SaleEntryResult> CreateSaleCoreAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SaleEntryRequest request,
        CancellationToken cancellationToken)
    {
        var agent = await LoadAgentAsync(connection, transaction, request.AgentId, cancellationToken);
        var draw = await LoadDrawAsync(connection, transaction, request.DrawId, cancellationToken);

        if (!string.Equals(draw.Status, DrawStatuses.Open, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The selected draw is not open for sales.");
        }

        var plannedItems = new List<PlannedItem>(request.Items.Count);
        foreach (var item in request.Items)
        {
            plannedItems.Add(await PlanItemAsync(connection, transaction, item, cancellationToken));
        }

        var totalStake = decimal.Round(plannedItems.Sum(item => item.Amount), 2, MidpointRounding.AwayFromZero);
        var commission = _commissionService.CalculateCommission(totalStake, agent.Rate);
        var commission1 = _commissionService.CalculateCommission1(totalStake, agent.Rate1);
        var saleDate = NormalizeUtc(request.SaleDate ?? DateTime.UtcNow);
        var currencyType = request.CurrencyType ?? agent.CurrencyType;
        var currencyRate = request.CurrencyRate ?? agent.CurrencyRate;

        var saleId = await InsertSaleAsync(
            connection,
            transaction,
            request,
            totalStake,
            commission,
            commission1,
            saleDate,
            currencyType,
            currencyRate,
            cancellationToken);

        var resultItems = new List<SaleEntryItemResult>(plannedItems.Count);
        foreach (var plannedItem in plannedItems)
        {
            resultItems.Add(await InsertItemAsync(connection, transaction, saleId, request.DrawId, plannedItem, cancellationToken));
        }

        var autoBuy = await EvaluateAutoBuyAsync(connection, transaction, agent.AgentName, totalStake, cancellationToken);

        if (autoBuy.Triggered)
        {
            _autoBuyTriggeredLog(_logger, saleId, autoBuy.RuleId, autoBuy.Amount, null);
        }

        return new SaleEntryResult
        {
            SaleId = saleId,
            TotalStake = totalStake,
            Commission = commission,
            Commission1 = commission1,
            AutoBuyTriggered = autoBuy.Triggered,
            AutoBuyRuleId = autoBuy.RuleId,
            AutoBuyAmount = autoBuy.Amount,
            Items = resultItems
        };
    }

    private async Task<PlannedItem> PlanItemAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SaleEntryItemRequest item,
        CancellationToken cancellationToken)
    {
        var betType = await LoadBetTypeAsync(connection, transaction, item.BetTypeCode, cancellationToken);
        var group = item.NumberGroupCode is null
            ? null
            : await LoadNumberGroupAsync(connection, transaction, item.NumberGroupCode, cancellationToken);

        if (group is not null && item.IncludeReverse && !group.AllowReverse)
        {
            throw new InvalidOperationException($"R is disabled for the '{group.NameMy}' group.");
        }

        if (item.IncludeReverse
            && group is null
            && !string.Equals(betType.RuleType, NumberRuleTypes.ExactAndReverse, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"R is not supported by the '{betType.NameMy}' sale type.");
        }

        var payoutMode = ParsePayoutMode(betType.PayoutMode);
        var resolved = ResolveNumbers(betType, group, item);

        decimal amount;
        decimal maxPayout;
        List<PlannedNumber> plannedNumbers;

        if (item.NumberStakes is { Count: > 0 })
        {
            plannedNumbers = ValidateExplicitStakes(
                item.NumberStakes,
                resolved.Numbers,
                betType.PayoutMultiplier,
                out maxPayout);
            amount = string.Equals(betType.AmountMode, BetAmountModes.Total, StringComparison.Ordinal)
                ? item.Amount
                : decimal.Round(plannedNumbers.Sum(number => number.Stake), 2, MidpointRounding.AwayFromZero);

            if (!string.Equals(betType.AmountMode, BetAmountModes.Total, StringComparison.Ordinal)
                && amount != item.Amount)
            {
                throw new InvalidOperationException(
                    $"The item amount for '{betType.NameMy}' does not match its number stakes.");
            }

            if (payoutMode == PayoutMode.TotalIfHit)
            {
                maxPayout = decimal.Round(amount * betType.PayoutMultiplier, 2, MidpointRounding.AwayFromZero);
            }
        }
        else
        {
            if (!_amountCalculator.TryCalculate(
                    item.Amount,
                    betType.AmountMode,
                    resolved.Numbers.Count,
                    betType.PayoutMultiplier,
                    payoutMode,
                    out var amountResult,
                    out var validationError))
            {
                throw new InvalidOperationException(validationError);
            }

            amount = amountResult.Total;
            maxPayout = amountResult.MaxPayout;
            plannedNumbers = new List<PlannedNumber>(resolved.Numbers.Count);
            for (var index = 0; index < resolved.Numbers.Count; index++)
            {
                plannedNumbers.Add(new PlannedNumber
                {
                    Number = resolved.Numbers[index],
                    Stake = amountResult.Stakes[index],
                    IsReverse = IsReverseNumber(resolved.Numbers[index], item, group)
                });
            }
        }

        return new PlannedItem
        {
            BetType = betType,
            Group = group,
            BetKey = group?.Code ?? item.Selection?.Trim() ?? betType.Code,
            Amount = amount,
            MaxPayout = maxPayout,
            MatchMode = resolved.MatchMode,
            Numbers = plannedNumbers
        };
    }

    private ResolvedSelection ResolveNumbers(
        BetTypeRecord betType,
        NumberGroupRecord? group,
        SaleEntryItemRequest item)
    {
        var request = new NumberResolutionRequest
        {
            RuleType = betType.RuleType,
            RuleConfig = TwoDJson.DeserializeRuleConfig(betType.RuleConfigJson),
            Selection = item.Selection,
            FixedNumbers = group?.Members,
            IncludeReverse = item.IncludeReverse
        };

        if (!_resolutionService.TryResolve(request, out var result, out var validationError))
        {
            throw new InvalidOperationException(validationError);
        }

        if (result.Numbers.Count == 0)
        {
            throw new InvalidOperationException($"The '{betType.NameMy}' sale type resolved to no numbers.");
        }

        return new ResolvedSelection
        {
            Numbers = result.Numbers,
            MatchMode = result.MatchMode
        };
    }

    private static bool IsReverseNumber(string number, SaleEntryItemRequest item, NumberGroupRecord? group)
    {
        if (!item.IncludeReverse)
        {
            return false;
        }

        if (group is not null)
        {
            return !group.Members.Contains(number, StringComparer.Ordinal);
        }

        if (string.IsNullOrWhiteSpace(item.Selection) || item.Selection.Length != 2)
        {
            return false;
        }

        return !string.Equals(number, item.Selection, StringComparison.Ordinal);
    }

    private static List<PlannedNumber> ValidateExplicitStakes(
        IReadOnlyList<SaleEntryNumberRequest> requested,
        IReadOnlyList<string> allowedNumbers,
        decimal payoutMultiplier,
        out decimal maxPayout)
    {
        var planned = new List<PlannedNumber>(requested.Count);
        var indexes = new Dictionary<string, int>(StringComparer.Ordinal);
        var allowed = new HashSet<string>(allowedNumbers, StringComparer.Ordinal);

        foreach (var number in requested)
        {
            if (!IsTwoDigitNumber(number.Number))
            {
                throw new InvalidOperationException("Every resolved number must be exactly two digits from 00 to 99.");
            }

            if (!allowed.Contains(number.Number))
            {
                throw new InvalidOperationException(
                    $"Number '{number.Number}' is not part of the resolved selection.");
            }

            if (number.Stake < 0)
            {
                throw new InvalidOperationException("A number stake cannot be negative.");
            }

            if (indexes.TryGetValue(number.Number, out var index))
            {
                planned[index].Stake += number.Stake;
                planned[index].IsReverse |= number.IsReverse;
                continue;
            }

            indexes[number.Number] = planned.Count;
            planned.Add(new PlannedNumber
            {
                Number = number.Number,
                Stake = number.Stake,
                IsReverse = number.IsReverse
            });
        }

        if (planned.Count == 0)
        {
            throw new InvalidOperationException("A sale item must resolve to at least one number.");
        }

        maxPayout = decimal.Round(planned.Max(number => number.Stake) * payoutMultiplier, 2, MidpointRounding.AwayFromZero);
        return planned;
    }

    private static PayoutMode ParsePayoutMode(string value)
    {
        if (Enum.TryParse<PayoutMode>(value, ignoreCase: true, out var mode))
        {
            return mode;
        }

        throw new InvalidOperationException($"Unknown payout mode '{value}'.");
    }

    private async Task<Agent> LoadAgentAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int agentId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT agent_name, rate, rate1, currency_type, currency_rate
FROM agents
WHERE id = @id;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@id", agentId));
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Agent {agentId} was not found.");
        }

        return new Agent
        {
            Id = agentId,
            AgentName = reader.GetString(reader.GetOrdinal("agent_name")),
            Rate = reader.GetDecimal(reader.GetOrdinal("rate")),
            Rate1 = reader.GetDecimal(reader.GetOrdinal("rate1")),
            CurrencyType = reader.IsDBNull(reader.GetOrdinal("currency_type"))
                ? null
                : reader.GetString(reader.GetOrdinal("currency_type")),
            CurrencyRate = reader.GetDecimal(reader.GetOrdinal("currency_rate"))
        };
    }

    private async Task<DrawRecord> LoadDrawAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long drawId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, status
FROM draws
WHERE id = @id
FOR UPDATE;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@id", drawId));
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Draw {drawId} was not found.");
        }

        return new DrawRecord
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Status = reader.GetString(reader.GetOrdinal("status"))
        };
    }

    private async Task<BetTypeRecord> LoadBetTypeAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string code,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, code, name_en, name_my, rule_type, rule_config::text,
       amount_mode, payout_mode, payout_multiplier, is_active, sort_order,
       created_at, updated_at
FROM bet_types
WHERE code = @code;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@code", code));
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Sale type '{code}' was not found.");
        }

        var betType = new BetTypeRecord
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

        if (!betType.IsActive)
        {
            throw new InvalidOperationException($"Sale type '{betType.NameMy}' is inactive.");
        }

        return betType;
    }

    private async Task<NumberGroupRecord> LoadNumberGroupAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string code,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, code, name_en, name_my, allow_r, is_active, sort_order,
       created_at, updated_at
FROM number_groups
WHERE code = @code;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@code", code));
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException($"Number group '{code}' was not found.");
        }

        var group = new NumberGroupRecord
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

        if (!group.IsActive)
        {
            throw new InvalidOperationException($"Number group '{group.NameMy}' is inactive.");
        }

        await reader.DisposeAsync();
        group.Members = await LoadGroupMembersAsync(connection, transaction, group.Id, cancellationToken);
        return group;
    }

    private static async Task<List<string>> LoadGroupMembersAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int groupId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT number
FROM number_group_members
WHERE group_id = @group_id
ORDER BY sort_order, number;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@group_id", groupId));
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var members = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            members.Add(reader.GetString(0));
        }

        return members;
    }

    private static async Task<long> InsertSaleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SaleEntryRequest request,
        decimal totalStake,
        decimal commission,
        decimal commission1,
        DateTime saleDate,
        string? currencyType,
        decimal currencyRate,
        CancellationToken cancellationToken)
    {
        const string sql = """
INSERT INTO sales
    (agent_id, sale_date, amount, commission, commission1, currency_type,
     currency_rate, status, draw_id, created_by, settlement_status, updated_at)
VALUES
    (@agent_id, @sale_date, @amount, @commission, @commission1, @currency_type,
     @currency_rate, 'Pending', @draw_id, @created_by, 'Open', NOW())
RETURNING id;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@agent_id", request.AgentId));
        command.Parameters.Add(new NpgsqlParameter("@sale_date", saleDate));
        command.Parameters.Add(new NpgsqlParameter("@amount", totalStake));
        command.Parameters.Add(new NpgsqlParameter("@commission", commission));
        command.Parameters.Add(new NpgsqlParameter("@commission1", commission1));
        command.Parameters.Add(new NpgsqlParameter("@currency_type", currencyType ?? (object)DBNull.Value));
        command.Parameters.Add(new NpgsqlParameter("@currency_rate", currencyRate));
        command.Parameters.Add(new NpgsqlParameter("@draw_id", request.DrawId));
        command.Parameters.Add(new NpgsqlParameter("@created_by", request.CreatedBy ?? (object)DBNull.Value));

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<SaleEntryItemResult> InsertItemAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long saleId,
        long drawId,
        PlannedItem plannedItem,
        CancellationToken cancellationToken)
    {
        const string itemSql = """
INSERT INTO sale_items
    (sale_id, draw_id, bet_type_id, number_group_id, bet_key, amount,
     payout_multiplier, payout_mode, amount_mode, match_mode, status)
VALUES
    (@sale_id, @draw_id, @bet_type_id, @number_group_id, @bet_key, @amount,
     @payout_multiplier, @payout_mode, @amount_mode, @match_mode, 'Pending')
RETURNING id;
""";

        long saleItemId;
        using (var command = new NpgsqlCommand(itemSql, connection, transaction))
        {
            command.Parameters.Add(new NpgsqlParameter("@sale_id", saleId));
            command.Parameters.Add(new NpgsqlParameter("@draw_id", drawId));
            command.Parameters.Add(new NpgsqlParameter("@bet_type_id", plannedItem.BetType.Id));
            command.Parameters.Add(new NpgsqlParameter("@number_group_id", plannedItem.Group?.Id ?? (object)DBNull.Value));
            command.Parameters.Add(new NpgsqlParameter("@bet_key", plannedItem.BetKey));
            command.Parameters.Add(new NpgsqlParameter("@amount", plannedItem.Amount));
            command.Parameters.Add(new NpgsqlParameter("@payout_multiplier", plannedItem.BetType.PayoutMultiplier));
            command.Parameters.Add(new NpgsqlParameter("@payout_mode", plannedItem.BetType.PayoutMode));
            command.Parameters.Add(new NpgsqlParameter("@amount_mode", plannedItem.BetType.AmountMode));
            command.Parameters.Add(new NpgsqlParameter("@match_mode", plannedItem.MatchMode.ToString()));

            var value = await command.ExecuteScalarAsync(cancellationToken);
            saleItemId = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        var numbers = new List<SaleItemNumberRecord>(plannedItem.Numbers.Count);
        foreach (var number in plannedItem.Numbers)
        {
            numbers.Add(await InsertNumberAsync(connection, transaction, saleItemId, number, cancellationToken));
        }

        return new SaleEntryItemResult
        {
            SaleItemId = saleItemId,
            BetTypeCode = plannedItem.BetType.Code,
            NumberGroupCode = plannedItem.Group?.Code,
            BetKey = plannedItem.BetKey,
            Amount = plannedItem.Amount,
            MaxPayout = plannedItem.MaxPayout,
            Numbers = numbers
        };
    }

    private static async Task<SaleItemNumberRecord> InsertNumberAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long saleItemId,
        PlannedNumber number,
        CancellationToken cancellationToken)
    {
        const string sql = """
INSERT INTO sale_item_numbers (sale_item_id, number, stake, is_reverse)
VALUES (@sale_item_id, @number, @stake, @is_reverse)
RETURNING id;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@sale_item_id", saleItemId));
        command.Parameters.Add(new NpgsqlParameter("@number", number.Number));
        command.Parameters.Add(new NpgsqlParameter("@stake", number.Stake));
        command.Parameters.Add(new NpgsqlParameter("@is_reverse", number.IsReverse));

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return new SaleItemNumberRecord
        {
            Id = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture),
            SaleItemId = saleItemId,
            Number = number.Number,
            Stake = number.Stake,
            IsReverse = number.IsReverse
        };
    }

    private static async Task<(bool Triggered, int? RuleId, decimal? Amount)> EvaluateAutoBuyAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string agentName,
        decimal totalStake,
        CancellationToken cancellationToken)
    {
        const string selectSql = """
SELECT id, trigger_amount, auto_buy_amount
FROM auto_buy_rules
WHERE LOWER(agent_name) = LOWER(@agent_name) AND is_enabled = TRUE
LIMIT 1;
""";

        int ruleId;
        decimal triggerAmount;
        decimal autoBuyAmount;
        using (var command = new NpgsqlCommand(selectSql, connection, transaction))
        {
            command.Parameters.Add(new NpgsqlParameter("@agent_name", agentName));
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, null, null);
            }

            ruleId = reader.GetInt32(reader.GetOrdinal("id"));
            triggerAmount = reader.GetDecimal(reader.GetOrdinal("trigger_amount"));
            autoBuyAmount = reader.GetDecimal(reader.GetOrdinal("auto_buy_amount"));
        }

        if (totalStake < triggerAmount)
        {
            return (false, ruleId, autoBuyAmount);
        }

        const string updateSql = """
UPDATE auto_buy_rules
SET execution_count = execution_count + 1,
    last_executed = NOW()
WHERE id = @id;
""";

        using (var command = new NpgsqlCommand(updateSql, connection, transaction))
        {
            command.Parameters.Add(new NpgsqlParameter("@id", ruleId));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return (true, ruleId, autoBuyAmount);
    }

    private static void ValidateRequest(SaleEntryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.AgentId <= 0)
        {
            throw new ArgumentException("An agent is required.", nameof(request));
        }

        if (request.DrawId <= 0)
        {
            throw new ArgumentException("A draw is required.", nameof(request));
        }

        if (request.CurrencyRate is <= 0)
        {
            throw new ArgumentException("Currency rate must be greater than zero.", nameof(request));
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ArgumentException("A sale must contain at least one item.", nameof(request));
        }

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.BetTypeCode))
            {
                throw new ArgumentException("Every sale item needs a bet type.", nameof(request));
            }

            if (item.Amount <= 0)
            {
                throw new ArgumentException("Every sale item amount must be greater than zero.", nameof(request));
            }

            if (decimal.Round(item.Amount, 2) != item.Amount)
            {
                throw new ArgumentException("Sale item amounts may have at most two decimal places.", nameof(request));
            }
        }
    }

    private static bool IsTwoDigitNumber(string? value)
    {
        return value is { Length: 2 } && char.IsAsciiDigit(value[0]) && char.IsAsciiDigit(value[1]);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
    }

    private sealed class PlannedItem
    {
        public BetTypeRecord BetType { get; init; } = new();
        public NumberGroupRecord? Group { get; init; }
        public string BetKey { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public decimal MaxPayout { get; init; }
        public NumberMatchMode MatchMode { get; init; }
        public List<PlannedNumber> Numbers { get; init; } = [];
    }

    private sealed class ResolvedSelection
    {
        public IReadOnlyList<string> Numbers { get; init; } = Array.Empty<string>();
        public NumberMatchMode MatchMode { get; init; }
    }

    private sealed class PlannedNumber
    {
        public string Number { get; init; } = string.Empty;
        public decimal Stake { get; set; }
        public bool IsReverse { get; set; }
    }
}
