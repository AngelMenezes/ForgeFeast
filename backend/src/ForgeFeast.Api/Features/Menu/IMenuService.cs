namespace ForgeFeast.Api.Features.Menu;

public interface IMenuService
{
    /// <summary>Cardápio de segunda a sexta da semana que começa em <paramref name="weekStart"/> (já validada como segunda).</summary>
    Task<WeekMenuResponse> GetWeekAsync(DateOnly weekStart, CancellationToken cancellationToken);

    Task<DishResponse> CreateDishAsync(DishInput dish, CancellationToken cancellationToken);

    Task<DishResponse?> GetDishAsync(int id, CancellationToken cancellationToken);

    Task<DishResponse?> UpdateDishAsync(int id, DishInput dish, CancellationToken cancellationToken);

    Task<bool> DeleteDishAsync(int id, CancellationToken cancellationToken);
}
