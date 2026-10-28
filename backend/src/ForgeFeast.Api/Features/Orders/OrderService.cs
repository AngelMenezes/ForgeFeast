using System.Globalization;
using ForgeFeast.Api.Features.Menu;

namespace ForgeFeast.Api.Features.Orders;

public sealed class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;
    private readonly IMenuService _menuService;
    private readonly OrderCutoffPolicy _cutoffPolicy;
    private readonly TimeProvider _timeProvider;

    public OrderService(
        IOrderRepository repository,
        IMenuService menuService,
        OrderCutoffPolicy cutoffPolicy,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _menuService = menuService;
        _cutoffPolicy = cutoffPolicy;
        _timeProvider = timeProvider;
    }

    public async Task<OrderResult> CreateAsync(OrderInput order, CancellationToken cancellationToken)
    {
        var rejection = await CheckDishesAndCutoffAsync(order, cancellationToken);
        if (rejection.Result is not null)
        {
            return rejection.Result;
        }

        var written = await _repository.CreateAsync(order, NowUtc(), cancellationToken);
        return written.Status switch
        {
            OrderWriteStatus.Saved => Saved(written, rejection.Dishes),
            OrderWriteStatus.AlreadyExists => new OrderResult(
                OrderOutcome.AlreadyExists,
                Detail: $"Já existe um pedido de {order.Name} para {FormatDate(order.Date)}. Altere o pedido existente."),
            _ => await InvalidDishesAsync(order, cancellationToken),
        };
    }

    public async Task<OrderResult> ReplaceItemsAsync(OrderInput order, CancellationToken cancellationToken)
    {
        var rejection = await CheckDishesAndCutoffAsync(order, cancellationToken);
        if (rejection.Result is not null)
        {
            return rejection.Result;
        }

        var written = await _repository.ReplaceItemsAsync(order, NowUtc(), cancellationToken);
        return written.Status switch
        {
            OrderWriteStatus.Saved => Saved(written, rejection.Dishes),
            OrderWriteStatus.NotFound => NotFound(order.Name, order.Date),
            _ => await InvalidDishesAsync(order, cancellationToken),
        };
    }

    public async Task<OrderResult> GetAsync(OrderKey key, CancellationToken cancellationToken)
    {
        var record = await _repository.GetAsync(key, cancellationToken);
        if (record is null)
        {
            return NotFound(key.Name, key.Date);
        }

        var dishes = await _menuService.GetDishesAsync(record.Items.Select(item => item.DishId).ToList(), cancellationToken);
        return new OrderResult(OrderOutcome.Success, ToResponse(record, dishes));
    }

    public OrderWindowResponse GetWindow(DateOnly day)
    {
        return new OrderWindowResponse(
            day,
            _cutoffPolicy.CutoffTime.ToString(OrderingOptions.CutoffTimeFormat, CultureInfo.InvariantCulture),
            _cutoffPolicy.TimeZoneId,
            _cutoffPolicy.ClosesAt(day),
            _cutoffPolicy.GetStatus(day) == OrderDayStatus.Open);
    }

    private async Task<(OrderResult? Result, IReadOnlyList<DishResponse> Dishes)> CheckDishesAndCutoffAsync(
        OrderInput order,
        CancellationToken cancellationToken)
    {
        var dishes = await _menuService.GetDishesAsync(order.Items.Select(item => item.DishId).ToList(), cancellationToken);
        var itemErrors = DishErrors(order, dishes);
        if (itemErrors.Count > 0)
        {
            return (new OrderResult(OrderOutcome.InvalidItems, ItemErrors: itemErrors), dishes);
        }

        var status = _cutoffPolicy.GetStatus(order.Date);
        if (status != OrderDayStatus.Open)
        {
            return (new OrderResult(OrderOutcome.Closed, Detail: ClosedDetail(order.Date, status)), dishes);
        }

        return (null, dishes);
    }

    // Corrida com a remoção do prato entre a validação e a gravação (ADR-017): revalida e recusa com 400.
    private async Task<OrderResult> InvalidDishesAsync(OrderInput order, CancellationToken cancellationToken)
    {
        var dishes = await _menuService.GetDishesAsync(order.Items.Select(item => item.DishId).ToList(), cancellationToken);
        var itemErrors = DishErrors(order, dishes);
        if (itemErrors.Count == 0)
        {
            itemErrors = order.Items.Select(item => DishNotInMenuMessage(item.DishId, order.Date)).ToList();
        }

        return new OrderResult(OrderOutcome.InvalidItems, ItemErrors: itemErrors);
    }

    private static List<string> DishErrors(OrderInput order, IReadOnlyList<DishResponse> dishes)
    {
        var dishDates = dishes.ToDictionary(dish => dish.Id, dish => dish.Date);
        return order.Items
            .Where(item => !dishDates.TryGetValue(item.DishId, out var date) || date != order.Date)
            .Select(item => DishNotInMenuMessage(item.DishId, order.Date))
            .ToList();
    }

    private static OrderResult Saved(OrderWriteResult written, IReadOnlyList<DishResponse> dishes)
    {
        var record = written.Order ?? throw new InvalidOperationException("Gravação concluída sem o pedido.");
        return new OrderResult(OrderOutcome.Success, ToResponse(record, dishes));
    }

    private static OrderResponse ToResponse(OrderRecord record, IReadOnlyList<DishResponse> dishes)
    {
        var names = dishes.ToDictionary(dish => dish.Id, dish => dish.Name);
        var items = record.Items
            .OrderBy(item => item.DishId)
            .Select(item => new OrderItemResponse(item.DishId, names.GetValueOrDefault(item.DishId, string.Empty), item.Quantity))
            .ToList();
        return new OrderResponse(record.Name, record.Date, items, record.CreatedAtUtc, record.UpdatedAtUtc);
    }

    private static OrderResult NotFound(string name, DateOnly date)
    {
        return new OrderResult(OrderOutcome.NotFound, Detail: $"Não existe pedido de {name} para {FormatDate(date)}.");
    }

    private string ClosedDetail(DateOnly date, OrderDayStatus status)
    {
        return status == OrderDayStatus.PastDate
            ? "Não é possível enviar ou alterar pedidos de datas passadas."
            : $"Os pedidos de {FormatDate(date)} foram encerrados às {_cutoffPolicy.CutoffTime.ToString(OrderingOptions.CutoffTimeFormat, CultureInfo.InvariantCulture)}.";
    }

    // datetime2(0) arredonda para o segundo: truncar aqui mantém a resposta do POST igual à do GET.
    private DateTime NowUtc()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
    }

    private static string DishNotInMenuMessage(int dishId, DateOnly date)
    {
        return $"O prato {dishId} não está no cardápio de {FormatDate(date)}.";
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    }
}
