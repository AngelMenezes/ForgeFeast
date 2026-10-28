using ForgeFeast.Api.Features.Menu;

namespace ForgeFeast.Api.UnitTests.Features.Menu;

public sealed class MenuServiceTests
{
    private static DateOnly Monday => new(2026, 10, 26);

    [Fact]
    public async Task GetWeekAsync_NoDishes_ReturnsFiveDaysWithEmptyLists()
    {
        var service = new MenuService(new FakeDishRepository(), new FakeOrderLookup(hasOrders: false));

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
        var service = new MenuService(repository, new FakeOrderLookup(hasOrders: false));

        var menu = await service.GetWeekAsync(Monday, CancellationToken.None);

        Assert.Equal([1, 2], menu.Days[0].Dishes.Select(dish => dish.Id));
        Assert.Empty(menu.Days[1].Dishes);
        Assert.Equal([3], menu.Days[4].Dishes.Select(dish => dish.Id));
    }

    [Fact]
    public async Task GetWeekAsync_AnyWeek_QueriesRepositoryForMondayToFriday()
    {
        var repository = new FakeDishRepository();
        var service = new MenuService(repository, new FakeOrderLookup(hasOrders: false));

        await service.GetWeekAsync(Monday, CancellationToken.None);

        Assert.Equal((Monday, new DateOnly(2026, 10, 30)), repository.LastRange);
    }

    [Fact]
    public async Task DeleteDishAsync_UnknownDish_ReturnsNotFoundWithoutDeleting()
    {
        var repository = new FakeDishRepository();
        var service = new MenuService(repository, new FakeOrderLookup(hasOrders: false));

        var result = await service.DeleteDishAsync(99, CancellationToken.None);

        Assert.Equal(DeleteDishResult.NotFound, result);
        Assert.Empty(repository.DeletedIds);
    }

    [Fact]
    public async Task DeleteDishAsync_DishWithOrders_ReturnsHasOrdersWithoutDeleting()
    {
        var repository = new FakeDishRepository(new DishResponse(1, Monday, "Feijoada", null));
        var service = new MenuService(repository, new FakeOrderLookup(hasOrders: true));

        var result = await service.DeleteDishAsync(1, CancellationToken.None);

        Assert.Equal(DeleteDishResult.HasOrders, result);
        Assert.Empty(repository.DeletedIds);
    }

    [Fact]
    public async Task DeleteDishAsync_DishWithoutOrders_Deletes()
    {
        var repository = new FakeDishRepository(new DishResponse(1, Monday, "Feijoada", null));
        var service = new MenuService(repository, new FakeOrderLookup(hasOrders: false));

        var result = await service.DeleteDishAsync(1, CancellationToken.None);

        Assert.Equal(DeleteDishResult.Deleted, result);
        Assert.Equal([1], repository.DeletedIds);
    }

    [Fact]
    public async Task GetDishesAsync_NoIds_ReturnsEmptyWithoutQuerying()
    {
        var repository = new FakeDishRepository(new DishResponse(1, Monday, "Feijoada", null));
        var service = new MenuService(repository, new FakeOrderLookup(hasOrders: false));

        var dishes = await service.GetDishesAsync([], CancellationToken.None);

        Assert.Empty(dishes);
    }

    [Fact]
    public async Task GetDishesAsync_SomeUnknownIds_ReturnsOnlyExistingDishes()
    {
        var repository = new FakeDishRepository(new DishResponse(1, Monday, "Feijoada", null));
        var service = new MenuService(repository, new FakeOrderLookup(hasOrders: false));

        var dishes = await service.GetDishesAsync([1, 2], CancellationToken.None);

        Assert.Equal([1], dishes.Select(dish => dish.Id));
    }

    private sealed class FakeOrderLookup : IDishOrderLookup
    {
        private readonly bool _hasOrders;

        public FakeOrderLookup(bool hasOrders)
        {
            _hasOrders = hasOrders;
        }

        public Task<bool> HasOrdersAsync(int dishId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_hasOrders);
        }
    }

    private sealed class FakeDishRepository : IDishRepository
    {
        private readonly IReadOnlyList<DishResponse> _dishes;

        public FakeDishRepository(params DishResponse[] dishes)
        {
            _dishes = dishes;
        }

        public List<int> DeletedIds { get; } = [];

        public Task<IReadOnlyList<DishResponse>> ListByIdsAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DishResponse>>(_dishes.Where(dish => ids.Contains(dish.Id)).ToList());
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
            return Task.FromResult(_dishes.FirstOrDefault(dish => dish.Id == id));
        }

        public Task<DishResponse?> UpdateAsync(int id, DishInput dish, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<DeleteDishResult> DeleteAsync(int id, CancellationToken cancellationToken)
        {
            DeletedIds.Add(id);
            return Task.FromResult(DeleteDishResult.Deleted);
        }
    }
}
