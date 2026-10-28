namespace ForgeFeast.Api.Features.Menu;

public sealed class MenuService : IMenuService
{
    private const int WorkDaysPerWeek = 5;

    private readonly IDishRepository _repository;

    public MenuService(IDishRepository repository)
    {
        _repository = repository;
    }

    public async Task<WeekMenuResponse> GetWeekAsync(DateOnly weekStart, CancellationToken cancellationToken)
    {
        var weekEnd = weekStart.AddDays(WorkDaysPerWeek - 1);
        var dishes = await _repository.ListByDateRangeAsync(weekStart, weekEnd, cancellationToken);
        var dishesByDate = dishes.ToLookup(dish => dish.Date);

        var days = Enumerable.Range(0, WorkDaysPerWeek)
            .Select(offset => weekStart.AddDays(offset))
            .Select(date => new MenuDayResponse(date, date.DayOfWeek, dishesByDate[date].ToList()))
            .ToList();

        return new WeekMenuResponse(weekStart, weekEnd, days);
    }

    public Task<DishResponse> CreateDishAsync(DishInput dish, CancellationToken cancellationToken)
    {
        return _repository.InsertAsync(dish, cancellationToken);
    }

    public Task<DishResponse?> GetDishAsync(int id, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task<DishResponse?> UpdateDishAsync(int id, DishInput dish, CancellationToken cancellationToken)
    {
        return _repository.UpdateAsync(id, dish, cancellationToken);
    }

    public Task<bool> DeleteDishAsync(int id, CancellationToken cancellationToken)
    {
        return _repository.DeleteAsync(id, cancellationToken);
    }
}
