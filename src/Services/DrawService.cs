using BProMax.Models;
using Npgsql;

namespace BProMax.Services;

/// <summary>
/// Manages the draw lifecycle: Open → Closed → Drawn → Settled or Cancelled.
/// </summary>
public class DrawService : IDrawService
{
    private readonly IDatabaseService _db;

    public DrawService(IDatabaseService db)
    {
        _db = db;
    }

    public Task<List<DrawRecord>> GetDrawsAsync()
    {
        const string sql = """
SELECT id, draw_date, draw_time, draw_number, status, created_by, created_at, updated_at
FROM draws
ORDER BY draw_date DESC, draw_time DESC;
""";

        return _db.QueryListAsync(sql, MapDraw);
    }

    public Task<List<DrawRecord>> GetOpenDrawsAsync()
    {
        const string sql = """
SELECT id, draw_date, draw_time, draw_number, status, created_by, created_at, updated_at
FROM draws
WHERE status = 'Open'
ORDER BY draw_date, draw_time;
""";

        return _db.QueryListAsync(sql, MapDraw);
    }

    public Task<DrawRecord?> GetDrawByIdAsync(long id)
    {
        const string sql = """
SELECT id, draw_date, draw_time, draw_number, status, created_by, created_at, updated_at
FROM draws
WHERE id = @id;
""";

        return _db.QuerySingleAsync(sql, MapDraw,
            new NpgsqlParameter("@id", id));
    }

    public Task<long> CreateDrawAsync(DateOnly drawDate, TimeOnly drawTime, int? createdBy)
    {
        const string sql = """
INSERT INTO draws (draw_date, draw_time, status, created_by, updated_at)
VALUES (@draw_date, @draw_time, 'Open', @created_by, NOW())
RETURNING id;
""";

        return _db.QuerySingleAsync(sql, reader => reader.GetInt64(0),
            new NpgsqlParameter("@draw_date", drawDate),
            new NpgsqlParameter("@draw_time", drawTime),
            new NpgsqlParameter("@created_by", createdBy ?? (object)DBNull.Value));
    }

    public Task<int> CloseDrawAsync(long id)
    {
        const string sql = """
UPDATE draws
SET status = 'Closed', updated_at = NOW()
WHERE id = @id AND status = 'Open';
""";

        return _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@id", id));
    }

    public Task<int> SetResultAsync(long id, string drawNumber)
    {
        if (!IsTwoDigitNumber(drawNumber))
        {
            throw new ArgumentException("The draw result must be exactly two digits from 00 to 99.", nameof(drawNumber));
        }

        const string sql = """
UPDATE draws
SET draw_number = @draw_number, status = 'Drawn', updated_at = NOW()
WHERE id = @id AND status = 'Closed';
""";

        return _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@id", id),
            new NpgsqlParameter("@draw_number", drawNumber));
    }

    public Task<int> CancelDrawAsync(long id)
    {
        const string sql = """
UPDATE draws
SET status = 'Cancelled', updated_at = NOW()
WHERE id = @id AND status IN ('Open', 'Closed');
""";

        return _db.ExecuteNonQueryAsync(sql,
            new NpgsqlParameter("@id", id));
    }

    private static DrawRecord MapDraw(NpgsqlDataReader reader)
    {
        return new DrawRecord
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            DrawDate = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("draw_date")),
            DrawTime = reader.GetFieldValue<TimeOnly>(reader.GetOrdinal("draw_time")),
            DrawNumber = reader.IsDBNull(reader.GetOrdinal("draw_number"))
                ? null
                : reader.GetString(reader.GetOrdinal("draw_number")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("created_by")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    private static bool IsTwoDigitNumber(string? value)
    {
        return value is { Length: 2 } && char.IsAsciiDigit(value[0]) && char.IsAsciiDigit(value[1]);
    }
}
