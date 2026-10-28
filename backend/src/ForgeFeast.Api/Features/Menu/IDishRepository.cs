namespace ForgeFeast.Api.Features.Menu;

public interface IDishRepository
{
    /// <summary>Pratos do intervalo fechado [startDate, endDate], ordenados por data e id.</summary>
    Task<IReadOnlyList<DishResponse>> ListByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);

    Task<DishResponse> InsertAsync(DishInput dish, CancellationToken cancellationToken);

    Task<DishResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>Substitui o prato inteiro. Retorna null quando o id não existe.</summary>
    Task<DishResponse?> UpdateAsync(int id, DishInput dish, CancellationToken cancellationToken);

    /// <summary>Retorna false quando o id não existe.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
