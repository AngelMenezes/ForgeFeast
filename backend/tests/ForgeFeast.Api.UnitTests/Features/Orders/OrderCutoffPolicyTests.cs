using ForgeFeast.Api.Features.Orders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace ForgeFeast.Api.UnitTests.Features.Orders;

public sealed class OrderCutoffPolicyTests
{
    // São Paulo está em UTC-3 (sem horário de verão), então 12:00 locais = 15:00 UTC.
    private static DateOnly Today => new(2026, 10, 28);

    private static OrderCutoffPolicy CreatePolicy(DateTimeOffset utcNow, string cutoff = "12:00", string timeZone = "America/Sao_Paulo")
    {
        return new OrderCutoffPolicy(
            new FakeTimeProvider(utcNow),
            Options.Create(new OrderingOptions { CutoffTime = cutoff, TimeZone = timeZone }));
    }

    [Theory]
    [InlineData("2026-10-28T14:59:59Z")]
    [InlineData("2026-10-28T03:00:00Z")]
    public void GetStatus_TodayBeforeCutoffInServerUtc_IsOpen(string utcNow)
    {
        var policy = CreatePolicy(DateTimeOffset.Parse(utcNow, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(OrderDayStatus.Open, policy.GetStatus(Today));
    }

    [Theory]
    [InlineData("2026-10-28T15:00:00Z")]
    [InlineData("2026-10-28T20:30:00Z")]
    public void GetStatus_TodayAtOrAfterCutoffInServerUtc_IsClosedToday(string utcNow)
    {
        var policy = CreatePolicy(DateTimeOffset.Parse(utcNow, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(OrderDayStatus.ClosedToday, policy.GetStatus(Today));
    }

    [Fact]
    public void GetStatus_PastDate_IsPastDate()
    {
        var policy = CreatePolicy(new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero));

        Assert.Equal(OrderDayStatus.PastDate, policy.GetStatus(Today.AddDays(-1)));
    }

    [Fact]
    public void GetStatus_FutureDateAfterTodaysCutoff_IsOpen()
    {
        var policy = CreatePolicy(new DateTimeOffset(2026, 10, 28, 20, 0, 0, TimeSpan.Zero));

        Assert.Equal(OrderDayStatus.Open, policy.GetStatus(Today.AddDays(1)));
    }

    [Fact]
    public void GetStatus_UtcAlreadyNextDayButLocalStillToday_UsesLocalDate()
    {
        // 02:00 UTC de 29/10 = 23:00 de 28/10 em São Paulo: 28/10 já encerrou, 29/10 ainda não.
        var policy = CreatePolicy(new DateTimeOffset(2026, 10, 29, 2, 0, 0, TimeSpan.Zero));

        Assert.Equal(OrderDayStatus.ClosedToday, policy.GetStatus(Today));
        Assert.Equal(OrderDayStatus.Open, policy.GetStatus(Today.AddDays(1)));
    }

    [Fact]
    public void GetStatus_ConfiguredCutoff_ChangesBehaviorWithoutCodeChange()
    {
        var now = new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero); // 10:00 locais

        Assert.Equal(OrderDayStatus.Open, CreatePolicy(now, cutoff: "10:01").GetStatus(Today));
        Assert.Equal(OrderDayStatus.ClosedToday, CreatePolicy(now, cutoff: "10:00").GetStatus(Today));
    }

    [Fact]
    public void ClosesAt_AnyDay_UsesTimeZoneOffset()
    {
        var policy = CreatePolicy(new DateTimeOffset(2026, 10, 28, 13, 0, 0, TimeSpan.Zero));

        var closesAt = policy.ClosesAt(Today);

        Assert.Equal(TimeSpan.FromHours(-3), closesAt.Offset);
        Assert.Equal(new DateTimeOffset(2026, 10, 28, 15, 0, 0, TimeSpan.Zero), closesAt);
    }

    [Theory]
    [InlineData("12h00")]
    [InlineData("25:00")]
    [InlineData("")]
    public void Constructor_InvalidCutoff_Throws(string cutoff)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreatePolicy(DateTimeOffset.UnixEpoch, cutoff: cutoff));

        Assert.Contains("Ordering__CutoffTime", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_UnknownTimeZone_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreatePolicy(DateTimeOffset.UnixEpoch, timeZone: "Marte/Olympus"));

        Assert.Contains("Ordering__TimeZone", exception.Message, StringComparison.Ordinal);
    }
}
