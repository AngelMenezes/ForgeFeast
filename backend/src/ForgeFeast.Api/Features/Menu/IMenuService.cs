namespace ForgeFeast.Api.Features.Menu;

public interface IMenuService
{
    /// <summary>Cardápio de segunda a sexta da semana que começa em <paramref name="weekStart"/> (já validada como segunda).</summary>
    Task<WeekMenuResponse> GetWeekAsync(DateOnly weekStart, CancellationToken cancellationToken);

    Task<DishResponse> CreateDishAsync(DishInput dish, CancellationToken cancellationToken);

    Task<DishResponse?> GetDishAsync(int id, CancellationToken cancellationToken);

    Task<DishResponse?> UpdateDishAsync(int id, DishInput dish, CancellationToken cancellationToken);

    /// <summary>Pratos encontrados entre os ids informados (ids inexistentes são omitidos).</summary>
    Task<IReadOnlyList<DishResponse>> GetDishesAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);

    /// <summary>Remove o prato, exceto se ele estiver em algum pedido (ADR-017).</summary>
    Task<DeleteDishResult> DeleteDishAsync(int id, CancellationToken cancellationToken);
}
