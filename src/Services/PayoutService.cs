using BProMax.Models;
using Npgsql;

namespace BProMax.Services;

/// <summary>
/// Settles a drawn result into payouts and records when those payouts are paid.
/// </summary>
public class PayoutService : IPayoutService
{
    private readonly IDatabaseService _db;
    private readonly IPayoutCalculator _calculator;

    public PayoutService(IDatabaseService db, IPayoutCalculator calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    public Task<PayoutSettlementResult> SettleDrawAsync(
        long drawId,
        int? settledBy = null,
        CancellationToken cancellationToken = default)
    {
        if (drawId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(drawId), drawId, "A draw is required.");
        }

        return _db.ExecuteInTransactionAsync(
            (connection, transaction) => SettleDrawCoreAsync(connection, transaction, drawId, settledBy, cancellationToken));
    }

    public async Task<int> MarkPayoutPaidAsync(
        long payoutId,
        int paidBy,
        CancellationToken cancellationToken = default)
    {
        if (payoutId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payoutId), payoutId, "A payout is required.");
        }

        if (paidBy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(paidBy), paidBy, "A paying user is required.");
        }

        const string sql = """
UPDATE payouts
SET status = 'Paid', paid_at = NOW(), paid_by = @paid_by
WHERE id = @id AND status = 'Pending';
""";

        return await _db.ExecuteNonQueryAsync(
            sql,
            new NpgsqlParameter("@id", payoutId),
            new NpgsqlParameter("@paid_by", paidBy));
    }

    public Task<List<PayoutRecord>> GetPayoutsByDrawAsync(
        long drawId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
SELECT id, draw_id, sale_id, sale_item_id, sale_item_number_id,
       amount, status, created_at, paid_at, paid_by
FROM payouts
WHERE draw_id = @draw_id
ORDER BY id;
""";

        return _db.QueryListAsync(sql, MapPayout,
            new NpgsqlParameter("@draw_id", drawId));
    }

    public Task<List<PayoutDetailRecord>> GetPayoutDetailsByDrawAsync(
        long drawId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
SELECT p.id, p.sale_id, p.sale_item_id, a.agent_name,
       COALESCE(g.name_my, bt.name_en) AS bet_label,
       sin.number, p.amount, p.status, p.created_at, p.paid_at, p.paid_by
FROM payouts p
JOIN sales s ON s.id = p.sale_id
JOIN agents a ON a.id = s.agent_id
JOIN sale_items si ON si.id = p.sale_item_id
JOIN bet_types bt ON bt.id = si.bet_type_id
LEFT JOIN number_groups g ON g.id = si.number_group_id
LEFT JOIN sale_item_numbers sin ON sin.id = p.sale_item_number_id
WHERE p.draw_id = @draw_id
ORDER BY p.id;
""";

        return _db.QueryListAsync(sql, reader => new PayoutDetailRecord
        {
            PayoutId = reader.GetInt64(reader.GetOrdinal("id")),
            SaleId = reader.GetInt64(reader.GetOrdinal("sale_id")),
            SaleItemId = reader.GetInt64(reader.GetOrdinal("sale_item_id")),
            AgentName = reader.GetString(reader.GetOrdinal("agent_name")),
            BetLabel = reader.GetString(reader.GetOrdinal("bet_label")),
            Number = reader.IsDBNull(reader.GetOrdinal("number"))
                ? null
                : reader.GetString(reader.GetOrdinal("number")),
            Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            PaidAt = reader.IsDBNull(reader.GetOrdinal("paid_at"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("paid_at")),
            PaidBy = reader.IsDBNull(reader.GetOrdinal("paid_by"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("paid_by"))
        }, new NpgsqlParameter("@draw_id", drawId));
    }

    private async Task<PayoutSettlementResult> SettleDrawCoreAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long drawId,
        int? settledBy,
        CancellationToken cancellationToken)
    {
        var draw = await LoadDrawAsync(connection, transaction, drawId, cancellationToken);
        if (!string.Equals(draw.Status, DrawStatuses.Drawn, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only a drawn result can be settled.");
        }

        var items = await LoadPendingItemsAsync(connection, transaction, drawId, cancellationToken);
        var payouts = new List<PayoutRecord>();
        var winningItems = 0;
        var totalPayout = 0m;

        foreach (var item in items)
        {
            var numbers = await LoadNumbersAsync(connection, transaction, item.Id, cancellationToken);
            if (numbers.Count == 0)
            {
                await UpdateItemStatusAsync(connection, transaction, item.Id, SaleItemStatuses.Lost, cancellationToken);
                continue;
            }

            var request = new PayoutCalculationRequest
            {
                ResultNumber = draw.DrawNumber!,
                MatchMode = ParseMatchMode(item.MatchMode),
                PayoutMode = ParsePayoutMode(item.PayoutMode),
                PayoutMultiplier = item.PayoutMultiplier,
                ItemAmount = item.Amount,
                Numbers = numbers.Select(number => new PayoutNumberStake
                {
                    Number = number.Number,
                    Stake = number.Stake,
                    IsReverse = number.IsReverse
                }).ToArray()
            };

            if (!_calculator.TryCalculate(request, out var calculation, out var validationError))
            {
                throw new InvalidOperationException(validationError);
            }

            var winningIds = new HashSet<long>();
            foreach (var number in numbers)
            {
                var winningLine = calculation.WinningNumbers.FirstOrDefault(
                    line => string.Equals(line.Number, number.Number, StringComparison.Ordinal));

                var isWinner = winningLine is not null;
                var numberStatus = isWinner ? SaleItemStatuses.Won : SaleItemStatuses.Lost;
                var numberPayout = isWinner && calculation.IsWinner
                    ? winningLine!.Payout
                    : 0m;

                await UpdateNumberStatusAsync(
                    connection,
                    transaction,
                    number.Id,
                    numberStatus,
                    numberPayout,
                    cancellationToken);
            }

            var itemStatus = calculation.IsWinner ? SaleItemStatuses.Won : SaleItemStatuses.Lost;
            await UpdateItemStatusAsync(connection, transaction, item.Id, itemStatus, cancellationToken);

            if (!calculation.IsWinner)
            {
                continue;
            }

            winningItems++;
            totalPayout += calculation.TotalPayout;

            if (request.PayoutMode == PayoutMode.TotalIfHit)
            {
                payouts.Add(await InsertPayoutAsync(
                    connection,
                    transaction,
                    item,
                    null,
                    calculation.TotalPayout,
                    cancellationToken));
                continue;
            }

            foreach (var winningLine in calculation.WinningNumbers)
            {
                if (winningLine.Payout <= 0)
                {
                    continue;
                }

                var number = numbers.First(number =>
                    string.Equals(number.Number, winningLine.Number, StringComparison.Ordinal));
                payouts.Add(await InsertPayoutAsync(
                    connection,
                    transaction,
                    item,
                    number.Id,
                    winningLine.Payout,
                    cancellationToken));
            }
        }

        await UpdateSaleHeadersAsync(connection, transaction, drawId, cancellationToken);
        await UpdateDrawSettledAsync(connection, transaction, drawId, settledBy, cancellationToken);

        return new PayoutSettlementResult
        {
            DrawId = draw.Id,
            DrawNumber = draw.DrawNumber ?? string.Empty,
            ItemsSettled = items.Count,
            WinningItems = winningItems,
            TotalPayout = decimal.Round(totalPayout, 2, MidpointRounding.AwayFromZero),
            Payouts = payouts
        };
    }

    private static async Task<DrawRecord> LoadDrawAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long drawId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, draw_number, status
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
            DrawNumber = reader.IsDBNull(reader.GetOrdinal("draw_number"))
                ? null
                : reader.GetString(reader.GetOrdinal("draw_number")),
            Status = reader.GetString(reader.GetOrdinal("status"))
        };
    }

    private static async Task<List<SaleItemRecord>> LoadPendingItemsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long drawId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT si.id, si.sale_id, si.amount, si.payout_multiplier, si.payout_mode, si.match_mode
FROM sale_items si
JOIN sales s ON s.id = si.sale_id
WHERE si.draw_id = @draw_id
  AND si.status = 'Pending'
  AND s.status <> 'Cancelled'
ORDER BY si.id;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@draw_id", drawId));
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<SaleItemRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new SaleItemRecord
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                SaleId = reader.GetInt64(reader.GetOrdinal("sale_id")),
                DrawId = drawId,
                Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
                PayoutMultiplier = reader.GetDecimal(reader.GetOrdinal("payout_multiplier")),
                PayoutMode = reader.GetString(reader.GetOrdinal("payout_mode")),
                MatchMode = reader.GetString(reader.GetOrdinal("match_mode"))
            });
        }

        return items;
    }

    private static async Task<List<SaleItemNumberRecord>> LoadNumbersAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long saleItemId,
        CancellationToken cancellationToken)
    {
        const string sql = """
SELECT id, number, stake, is_reverse
FROM sale_item_numbers
WHERE sale_item_id = @sale_item_id AND status = 'Pending'
ORDER BY id;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@sale_item_id", saleItemId));
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var numbers = new List<SaleItemNumberRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            numbers.Add(new SaleItemNumberRecord
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                SaleItemId = saleItemId,
                Number = reader.GetString(reader.GetOrdinal("number")),
                Stake = reader.GetDecimal(reader.GetOrdinal("stake")),
                IsReverse = reader.GetBoolean(reader.GetOrdinal("is_reverse"))
            });
        }

        return numbers;
    }

    private static async Task UpdateNumberStatusAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long numberId,
        string status,
        decimal payout,
        CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE sale_item_numbers
SET status = @status, payout_amount = @payout
WHERE id = @id;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@id", numberId));
        command.Parameters.Add(new NpgsqlParameter("@status", status));
        command.Parameters.Add(new NpgsqlParameter("@payout", payout));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdateItemStatusAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long saleItemId,
        string status,
        CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE sale_items
SET status = @status
WHERE id = @id;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@id", saleItemId));
        command.Parameters.Add(new NpgsqlParameter("@status", status));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<PayoutRecord> InsertPayoutAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        SaleItemRecord item,
        long? saleItemNumberId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        const string sql = """
INSERT INTO payouts
    (draw_id, sale_id, sale_item_id, sale_item_number_id, amount, status)
VALUES
    (@draw_id, @sale_id, @sale_item_id, @sale_item_number_id, @amount, 'Pending')
RETURNING id, created_at;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@draw_id", item.DrawId));
        command.Parameters.Add(new NpgsqlParameter("@sale_id", item.SaleId));
        command.Parameters.Add(new NpgsqlParameter("@sale_item_id", item.Id));
        command.Parameters.Add(new NpgsqlParameter("@sale_item_number_id", saleItemNumberId ?? (object)DBNull.Value));
        command.Parameters.Add(new NpgsqlParameter("@amount", amount));

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("The payout could not be created.");
        }

        return new PayoutRecord
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            DrawId = item.DrawId,
            SaleId = item.SaleId,
            SaleItemId = item.Id,
            SaleItemNumberId = saleItemNumberId,
            Amount = amount,
            Status = PayoutStatuses.Pending,
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }

    private static async Task UpdateSaleHeadersAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long drawId,
        CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE sales
SET settlement_status = 'Settled', settled_at = NOW(), updated_at = NOW()
WHERE draw_id = @draw_id AND status <> 'Cancelled';
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@draw_id", drawId));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdateDrawSettledAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long drawId,
        int? settledBy,
        CancellationToken cancellationToken)
    {
        const string sql = """
UPDATE draws
SET status = 'Settled', settled_by = @settled_by, updated_at = NOW()
WHERE id = @id;
""";

        using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.Add(new NpgsqlParameter("@id", drawId));
        command.Parameters.Add(new NpgsqlParameter("@settled_by", settledBy ?? (object)DBNull.Value));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static PayoutMode ParsePayoutMode(string value)
    {
        if (Enum.TryParse<PayoutMode>(value, ignoreCase: true, out var mode))
        {
            return mode;
        }

        throw new InvalidOperationException($"Unknown payout mode '{value}'.");
    }

    private static NumberMatchMode ParseMatchMode(string value)
    {
        if (Enum.TryParse<NumberMatchMode>(value, ignoreCase: true, out var mode))
        {
            return mode;
        }

        throw new InvalidOperationException($"Unknown match mode '{value}'.");
    }

    private static PayoutRecord MapPayout(NpgsqlDataReader reader)
    {
        return new PayoutRecord
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            DrawId = reader.GetInt64(reader.GetOrdinal("draw_id")),
            SaleId = reader.GetInt64(reader.GetOrdinal("sale_id")),
            SaleItemId = reader.GetInt64(reader.GetOrdinal("sale_item_id")),
            SaleItemNumberId = reader.IsDBNull(reader.GetOrdinal("sale_item_number_id"))
                ? null
                : reader.GetInt64(reader.GetOrdinal("sale_item_number_id")),
            Amount = reader.GetDecimal(reader.GetOrdinal("amount")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            PaidAt = reader.IsDBNull(reader.GetOrdinal("paid_at"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("paid_at")),
            PaidBy = reader.IsDBNull(reader.GetOrdinal("paid_by"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("paid_by"))
        };
    }
}
