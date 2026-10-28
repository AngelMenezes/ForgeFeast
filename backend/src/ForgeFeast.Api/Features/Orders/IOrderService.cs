namespace ForgeFeast.Api.Features.Orders;

public enum OrderOutcome
{
    Success,
    InvalidItems,
    Closed,
    NotFound,
    AlreadyExists,
}

/// <summary>Resultado de uma operação de pedido: o pedido em caso de sucesso, ou o motivo da recusa.</summary>
/// <param name="ItemErrors">Mensagens para a chave <c>items</c> quando o resultado é <see cref="OrderOutcome.InvalidItems"/>.</param>
/// <param name="Detail">Texto do ProblemDetails para <see cref="OrderOutcome.Closed"/>, <see cref="OrderOutcome.NotFound"/> e <see cref="OrderOutcome.AlreadyExists"/>.</param>
public sealed record OrderResult(
    OrderOutcome Outcome,
    OrderResponse? Order = null,
    IReadOnlyList<string>? ItemErrors = null,
    string? Detail = null);

public interface IOrderService
{
    /// <summary>Ordem (ADR-014): pratos da data, corte, duplicidade.</summary>
    Task<OrderResult> CreateAsync(OrderInput order, CancellationToken cancellationToken);

    /// <summary>Ordem (ADR-014): pratos da data, corte, existência.</summary>
    Task<OrderResult> ReplaceItemsAsync(OrderInput order, CancellationToken cancellationToken);

    /// <summary>Funciona para qualquer data, inclusive passada e encerrada.</summary>
    Task<OrderResult> GetAsync(OrderKey key, CancellationToken cancellationToken);

    OrderWindowResponse GetWindow(DateOnly day);
}
