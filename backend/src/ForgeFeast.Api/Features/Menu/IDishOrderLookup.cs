namespace ForgeFeast.Api.Features.Menu;

/// <summary>Porta definida pelo Menu e implementada por Orders, para evitar dependência circular (ADR-017).</summary>
public interface IDishOrderLookup
{
    /// <summary>Indica se o prato está em algum pedido, de qualquer data.</summary>
    Task<bool> HasOrdersAsync(int dishId, CancellationToken cancellationToken);
}
