namespace ForgeFeast.Api.Features.Orders;

public enum OrderWriteStatus
{
    Saved,
    AlreadyExists,
    NotFound,
    DishNotFound,
}

public sealed record OrderWriteResult(OrderWriteStatus Status, OrderRecord? Order = null);

public interface IOrderRepository
{
    /// <summary>Busca por nome (sem diferenciar maiúsculas) e data. Retorna null quando não existe.</summary>
    Task<OrderRecord?> GetAsync(OrderKey key, CancellationToken cancellationToken);

    /// <summary>Grava cabeçalho e itens numa transação. <see cref="OrderWriteStatus.AlreadyExists"/> em violação da unicidade.</summary>
    Task<OrderWriteResult> CreateAsync(OrderInput order, DateTime nowUtc, CancellationToken cancellationToken);

    /// <summary>Substitui os itens do pedido existente numa transação. <see cref="OrderWriteStatus.NotFound"/> se ele não existir.</summary>
    Task<OrderWriteResult> ReplaceItemsAsync(OrderInput order, DateTime nowUtc, CancellationToken cancellationToken);
}
