using ForgeFeast.Api.Features.Menu;

namespace ForgeFeast.Api.UnitTests.Features.Menu;

public sealed class MenuServiceTests
{
    private static DateOnly Monday => new(2026, 10, 26);

    [Fact]
    public async Task GetWeekAsync_NoDishes_ReturnsFiveDaysWithEmptyLists()
    {
        var service = new MenuService(new FakeDishRepository());

        var menu = await service.GetWeekAsync(Monday, CancellationToken.None);

        Assert.Equal(Monday, menu.WeekStart);
        Assert.Equal(new DateOnly(2026, 10, 30), menu.WeekEnd);
        Assert.Equal(
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
            menu.Days.Select(day => day.DayOfWeek));
        Assert.All(menu.Days, day => Assert.Empty(day.Dishes));
    }

    [Fact]
    public async Task GetWeekAsync_DishesOnSomeDays_GroupsByDateKeepingOrder()
    {
        var repository = new FakeDishRepository(
            new DishResponse(1, Monday, "Feijoada", null),
            new DishResponse(2, Monday, "Salada", "Alface"),
            new DishResponse(3, Monday.AddDays(4), "Peixe", null));
        var service = new MenuService(repository);

        var menu = await service.GetWeekAsync(Monday, CancellationToken.None);

        Assert.Equal([1, 2], menu.Days[0].Dishes.Select(dish => dish.Id));
        Assert.Empty(menu.Days[1].Dishes);
        Assert.Equal([3], menu.Days[4].Dishes.Select(dish => dish.Id));
    }

    [Fact]
    public async Task GetWeekAsync_AnyWeek_QueriesRepositoryForMondayToFriday()
    {
        var repository = new FakeDishRepository();
        var service = new MenuService(repository);

        await service.GetWeekAsync(Monday, CancellationToken.None);

        Assert.Equal((Monday, new DateOnly(2026, 10, 30)), repository.LastRange);
    }

    private sealed class FakeDishRepository : IDishRepository
    {
        private readonly IReadOnlyList<DishResponse> _dishes;

        public FakeDishRepository(params DishResponse[] dishes)
        {
            _dishes = dishes;
        }

        public (DateOnly Start, DateOnly End)? LastRange { get; private set; }

        public Task<IReadOnlyList<DishResponse>> ListByDateRangeAsync(
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken)
        {
            LastRange = (startDate, endDate);
            return Task.FromResult(_dishes);
        }

        public Task<DishResponse> InsertAsync(DishInput dish, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<DishResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<DishResponse?> UpdateAsync(int id, DishInput dish, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
