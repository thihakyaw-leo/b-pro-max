namespace BProMax.E2ETests;

using BProMax.Models;
using BProMax.Services;
using Npgsql;
using Xunit;

/// <summary>
/// E2E tests for the draw lifecycle service (SC-04):
///   Open → Closed → Drawn → Settled or Cancelled
/// Mirrors DD.exe draw board behavior.
/// </summary>
[Collection("E2E Database")]
public class DrawLifecycleE2ETests : E2ETestBase
{
    [Fact]
    public async Task DrawLifecycleOpenToClosedToDrawnMatchesServiceBehavior()
    {
        if (SkipIfUnavailable()) return;
        await CleanDrawsTableAsync();

        var drawService = new DrawService(CreateDatabaseService());

        // CREATE — draw starts as Open
        var drawId = await drawService.CreateDrawAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            createdBy: 1);
        Assert.True(drawId > 0);

        // READ — draw is Open
        var draw = await drawService.GetDrawByIdAsync(drawId);
        Assert.NotNull(draw);
        Assert.Equal(DrawStatuses.Open, draw.Status);
        Assert.Null(draw.DrawNumber);

        // GET OPEN DRAWS — includes this draw
        var openDraws = await drawService.GetOpenDrawsAsync();
        Assert.Contains(openDraws, d => d.Id == drawId);

        // CLOSE — transition Open → Closed
        var closed = await drawService.CloseDrawAsync(drawId);
        Assert.Equal(1, closed);

        draw = await drawService.GetDrawByIdAsync(drawId);
        Assert.NotNull(draw);
        Assert.Equal(DrawStatuses.Closed, draw!.Status);

        // GET OPEN DRAWS — no longer includes this draw
        openDraws = await drawService.GetOpenDrawsAsync();
        Assert.DoesNotContain(openDraws, d => d.Id == drawId);

        // SET RESULT — transition Closed → Drawn (two-digit result)
        var set = await drawService.SetResultAsync(drawId, "42");
        Assert.Equal(1, set);

        draw = await drawService.GetDrawByIdAsync(drawId);
        Assert.NotNull(draw);
        Assert.Equal(DrawStatuses.Drawn, draw!.Status);
        Assert.Equal("42", draw.DrawNumber);

        // CLOSE again should be a no-op (already Drawn, not Open)
        var closeAgain = await drawService.CloseDrawAsync(drawId);
        Assert.Equal(0, closeAgain);
    }

    [Fact]
    public async Task SetResultOnNonClosedDrawThrows()
    {
        if (SkipIfUnavailable()) return;
        await CleanDrawsTableAsync();

        var drawService = new DrawService(CreateDatabaseService());

        var drawId = await drawService.CreateDrawAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            createdBy: 1);

        // Draw is Open, not Closed — setting result should fail
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            drawService.SetResultAsync(drawId, "42"));
    }

    [Fact]
    public async Task SetResultRejectsNonTwoDigitNumber()
    {
        if (SkipIfUnavailable()) return;
        await CleanDrawsTableAsync();

        var drawService = new DrawService(CreateDatabaseService());

        var drawId = await drawService.CreateDrawAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            createdBy: 1);
        await drawService.CloseDrawAsync(drawId);

        // Not two digits
        await Assert.ThrowsAsync<ArgumentException>(() =>
            drawService.SetResultAsync(drawId, "7"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            drawService.SetResultAsync(drawId, "123"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            drawService.SetResultAsync(drawId, "ab"));
    }

    [Fact]
    public async Task CancelDrawTransitionsOpenOrClosedToCancelled()
    {
        if (SkipIfUnavailable()) return;
        await CleanDrawsTableAsync();

        var drawService = new DrawService(CreateDatabaseService());

        // Cancel an Open draw
        var openId = await drawService.CreateDrawAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            createdBy: 1);
        var cancelled = await drawService.CancelDrawAsync(openId);
        Assert.Equal(1, cancelled);
        var cancelledDraw = await drawService.GetDrawByIdAsync(openId);
        Assert.NotNull(cancelledDraw);
        Assert.Equal(DrawStatuses.Cancelled, cancelledDraw.Status);

        // Cancel a Closed draw
        var closedId = await drawService.CreateDrawAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            createdBy: 1);
        await drawService.CloseDrawAsync(closedId);
        cancelled = await drawService.CancelDrawAsync(closedId);
        Assert.Equal(1, cancelled);
        cancelledDraw = await drawService.GetDrawByIdAsync(closedId);
        Assert.NotNull(cancelledDraw);
        Assert.Equal(DrawStatuses.Cancelled, cancelledDraw.Status);

        // Cancel a Drawn draw should be a no-op (status not Open or Closed)
        var drawnId = await drawService.CreateDrawAsync(
            DateOnly.FromDateTime(DateTime.UtcNow),
            TimeOnly.FromDateTime(DateTime.UtcNow),
            createdBy: 1);
        await drawService.CloseDrawAsync(drawnId);
        await drawService.SetResultAsync(drawnId, "42");
        cancelled = await drawService.CancelDrawAsync(drawnId);
        Assert.Equal(0, cancelled);
        cancelledDraw = await drawService.GetDrawByIdAsync(drawnId);
        Assert.NotNull(cancelledDraw);
        Assert.Equal(DrawStatuses.Drawn, cancelledDraw.Status);
    }

    private async Task CleanDrawsTableAsync()
    {
        if (!IsAvailable) return;
        using var cmd = new NpgsqlCommand("DELETE FROM draws", Connection);
        await cmd.ExecuteNonQueryAsync();
    }
}
