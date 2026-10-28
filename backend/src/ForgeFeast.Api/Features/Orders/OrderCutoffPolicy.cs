using Microsoft.Extensions.Options;

namespace ForgeFeast.Api.Features.Orders;

public enum OrderDayStatus
{
    Open,
    ClosedToday,
    PastDate,
}

/// <summary>Regra do horário de corte, calculada com um relógio injetado (ADR-015, RNF-04).</summary>
public sealed class OrderCutoffPolicy
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public OrderCutoffPolicy(TimeProvider timeProvider, IOptions<OrderingOptions> options)
    {
        _timeProvider = timeProvider;

        var value = options.Value;
        if (!OrderingOptions.TryParseCutoffTime(value.CutoffTime, out var cutoffTime))
        {
            throw new InvalidOperationException(
                $"Ordering:CutoffTime inválido: '{value.CutoffTime}'. Use HH:mm (variável Ordering__CutoffTime).");
        }

        if (!OrderingOptions.TryFindTimeZone(value.TimeZone, out var timeZone))
        {
            throw new InvalidOperationException(
                $"Ordering:TimeZone não encontrado: '{value.TimeZone}' (variável Ordering__TimeZone).");
        }

        CutoffTime = cutoffTime;
        _timeZone = timeZone;
        TimeZoneId = value.TimeZone;
    }

    public TimeOnly CutoffTime { get; }

    public string TimeZoneId { get; }

    /// <summary>Instante em que o dia fecha, com o offset do fuso naquela data.</summary>
    public DateTimeOffset ClosesAt(DateOnly date)
    {
        var local = date.ToDateTime(CutoffTime);
        return new DateTimeOffset(local, _timeZone.GetUtcOffset(local));
    }

    /// <summary>Aberto até 11:59:59 (com corte 12:00); a partir do corte já está encerrado.</summary>
    public OrderDayStatus GetStatus(DateOnly date)
    {
        var nowLocal = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone);
        var today = DateOnly.FromDateTime(nowLocal.DateTime);

        if (date < today)
        {
            return OrderDayStatus.PastDate;
        }

        return date == today && nowLocal >= ClosesAt(date) ? OrderDayStatus.ClosedToday : OrderDayStatus.Open;
    }
}
