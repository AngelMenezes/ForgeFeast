using ForgeFeast.Api.Features.Menu;
using ForgeFeast.Api.Features.Orders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace ForgeFeast.Api.UnitTests.Features.Orders;

public sealed class OrderServiceTests
{
    private static DateOnly Today => new(2026, 10, 28);

    // 11:59:59 em São Paulo (UTC-3).
    private static DateTimeOffset BeforeCutoff => new(2026, 10, 28, 14, 59, 59, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_BeforeCutoff_SavesAndReturnsOrderWithDishNames()
    {
        var context = new Context(BeforeCutoff);

        var result = await context.Service.CreateAsync(Order(), CancellationToken.None);

        Assert.Equal(OrderOutcome.Success, result.Outcome);
        Assert.Equal(["Frango", "Salada"], result.Order?.Items.Select(item => item.DishName));
        Assert.Equal(1, context.Repository.WriteCount);
    }

    [Fact]
    public async Task CreateAsync_AtCutoff_ReturnsClosedAndWritesNothing()
    {
        var context = new Context(BeforeCutoff.AddSeconds(1));

        var result = await context.Service.CreateAsync(Order(), CancellationToken.None);

        Assert.Equal(OrderOutcome.Closed, result.Outcome);
        Assert.Equal("Os pedidos de 28/10/2026 foram encerrados às 12:00.", result.Detail);
        Assert.Equal(0, context.Repository.WriteCount);
    }

    [Fact]
    public async Task CreateAsync_PastDate_ReturnsClosedWithPastDateDetail()
    {
        var context = new Context(BeforeCutoff);
        var pastDish = new DishResponse(3, Today.AddDays(-1), "Frango", null);
        context.Menu.Dishes = [pastDish];

        var result = await context.Service.CreateAsync(
            new OrderInput("Ana", Today.AddDays(-1), [new OrderItemInput(3, 1)]),
            CancellationToken.None);

        Assert.Equal(OrderOutcome.Closed, result.Outcome);
        Assert.Equal("Não é possível enviar ou alterar pedidos de datas passadas.", result.Detail);
        Assert.Equal(0, context.Repository.WriteCount);
    }

    [Fact]
    public async Task CreateAsync_FutureDateAfterTodaysCutoff_Saves()
    {
        var context = new Context(BeforeCutoff.AddHours(5));
        var tomorrow = Today.AddDays(1);
        context.Menu.Dishes = [new DishResponse(3, tomorrow, "Frango", null)];

        var result = await context.Service.CreateAsync(
            new OrderInput("Ana", tomorrow, [new OrderItemInput(3, 1)]),
            CancellationToken.None);

        Assert.Equal(OrderOutcome.Success, result.Outcome);
    }

    [Fact]
    public async Task CreateAsync_DishFromAnotherDate_ReturnsInvalidItemsBeforeCutoffCheck()
    {
        var context = new Context(BeforeCutoff.AddHours(5));
        context.Menu.Dishes = [new DishResponse(3, Today.AddDays(1), "Frango", null), new DishResponse(4, Today, "Salada", null)];

        var result = await context.Service.CreateAsync(Order(), CancellationToken.None);

        Assert.Equal(OrderOutcome.InvalidItems, result.Outcome);
        Assert.Equal(["O prato 3 não está no cardápio de 28/10/2026."], result.ItemErrors);
        Assert.Equal(0, context.Repository.WriteCount);
    }

    [Fact]
    public async Task CreateAsync_UnknownDish_ReturnsInvalidItems()
    {
        var context = new Context(BeforeCutoff);
        context.Menu.Dishes = [new DishResponse(4, Today, "Salada", null)];

        var result = await context.Service.CreateAsync(Order(), CancellationToken.None);

        Assert.Equal(OrderOutcome.InvalidItems, result.Outcome);
        Assert.Equal(["O prato 3 não está no cardápio de 28/10/2026."], result.ItemErrors);
    }

    [Fact]
    public async Task CreateAsync_RepositoryReportsDuplicate_ReturnsAlreadyExists()
    {
        var context = new Context(BeforeCutoff);
        context.Repository.NextStatus = OrderWriteStatus.AlreadyExists;

        var result = await context.Service.CreateAsync(Order(), CancellationToken.None);

        Assert.Equal(OrderOutcome.AlreadyExists, result.Outcome);
        Assert.Equal("Já existe um pedido de Ana para 28/10/2026. Altere o pedido existente.", result.Detail);
    }

    [Fact]
    public async Task CreateAsync_DishRemovedDuringWrite_ReturnsInvalidItems()
    {
        var context = new Context(BeforeCutoff);
        context.Repository.NextStatus = OrderWriteStatus.DishNotFound;

        var result = await context.Service.CreateAsync(Order(), CancellationToken.None);

        Assert.Equal(OrderOutcome.InvalidItems, result.Outcome);
        Assert.NotEmpty(result.ItemErrors ?? []);
    }

    [Fact]
    public async Task ReplaceItemsAsync_AtCutoff_ReturnsClosedAndWritesNothing()
    {
        var context = new Context(BeforeCutoff.AddSeconds(1));

        var result = await context.Service.ReplaceItemsAsync(Order(), CancellationToken.None);

        Assert.Equal(OrderOutcome.Closed, result.Outcome);
        Assert.Equal(0, context.Repository.WriteCount);
    }

    [Fact]
    public async Task ReplaceItemsAsync_BeforeCutoff_Saves()
    {
        var context = new Context(BeforeCutoff);

        var result = await context.Service.ReplaceItemsAsync(Order(), CancellationToken.None);

        Assert.Equal(OrderOutcome.Success, result.Outcome);
    }

    [Fact]
    public async Task ReplaceItemsAsync_MissingOrder_ReturnsNotFound()
    {
        var context = new Context(BeforeCutoff);
        context.Repository.NextStatus = OrderWriteStatus.NotFound;

        var result = await context.Service.ReplaceItemsAsync(Order(), CancellationToken.None);

        Assert.Equal(OrderOutcome.NotFound, result.Outcome);
        Assert.Equal("Não existe pedido de Ana para 28/10/2026.", result.Detail);
    }

    [Fact]
    public async Task GetAsync_AfterCutoff_StillReturnsOrder()
    {
        var context = new Context(BeforeCutoff.AddHours(3));
        context.Repository.Stored = new OrderRecord("Ana", Today, [new OrderItemInput(3, 2)], BeforeCutoff.UtcDateTime, BeforeCutoff.UtcDateTime);

        var result = await context.Service.GetAsync(new OrderKey("ana", Today), CancellationToken.None);

        Assert.Equal(OrderOutcome.Success, result.Outcome);
        Assert.Equal("Frango", Assert.Single(result.Order?.Items ?? []).DishName);
    }

    [Fact]
    public async Task GetAsync_MissingOrder_ReturnsNotFound()
    {
        var context = new Context(BeforeCutoff);

        var result = await context.Service.GetAsync(new OrderKey("Ana", Today), CancellationToken.None);

        Assert.Equal(OrderOutcome.NotFound, result.Outcome);
    }

    [Fact]
    public void GetWindow_BeforeCutoff_IsOpenWithConfiguredCutoff()
    {
        var context = new Context(BeforeCutoff);

        var window = context.Service.GetWindow(Today);

        Assert.True(window.IsOpen);
        Assert.Equal("12:00", window.CutoffTime);
        Assert.Equal("America/Sao_Paulo", window.TimeZone);
        Assert.Equal(new DateTimeOffset(2026, 10, 28, 15, 0, 0, TimeSpan.Zero), window.ClosesAt);
    }

    [Fact]
    public void GetWindow_AfterCutoff_IsNotOpen()
    {
        var context = new Context(BeforeCutoff.AddSeconds(1));

        Assert.False(context.Service.GetWindow(Today).IsOpen);
    }

    private static OrderInput Order()
    {
        return new OrderInput("Ana", Today, [new OrderItemInput(3, 1), new OrderItemInput(4, 2)]);
    }

    private sealed class Context
    {
        public Context(DateTimeOffset utcNow)
        {
            var clock = new FakeTimeProvider(utcNow);
            var policy = new OrderCutoffPolicy(clock, Options.Create(new OrderingOptions()));
            Menu = new FakeMenuService
            {
                Dishes = [new DishResponse(3, Today, "Frango", null), new DishResponse(4, Today, "Salada", null)],
            };
            Repository = new FakeOrderRepository();
            Service = new OrderService(Repository, Menu, policy, clock);
        }

        public FakeMenuService Menu { get; }

        public FakeOrderRepository Repository { get; }

        public OrderService Service { get; }
    }

    private sealed class FakeMenuService : IMenuService
    {
        public IReadOnlyList<DishResponse> Dishes { get; set; } = [];

        public Task<IReadOnlyList<DishResponse>> GetDishesAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<DishResponse>>(Dishes.Where(dish => ids.Contains(dish.Id)).ToList());
        }

        public Task<WeekMenuResponse> GetWeekAsync(DateOnly weekStart, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<DishResponse> CreateDishAsync(DishInput dish, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<DishResponse?> GetDishAsync(int id, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<DishResponse?> UpdateDishAsync(int id, DishInput dish, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<DeleteDishResult> DeleteDishAsync(int id, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public int WriteCount { get; private set; }

        public OrderWriteStatus NextStatus { get; set; } = OrderWriteStatus.Saved;

        public OrderRecord? Stored { get; set; }

        public Task<OrderRecord?> GetAsync(OrderKey key, CancellationToken cancellationToken)
        {
            return Task.FromResult(Stored);
        }

        public Task<OrderWriteResult> CreateAsync(OrderInput order, DateTime nowUtc, CancellationToken cancellationToken)
        {
            return Task.FromResult(Write(order, nowUtc));
        }

        public Task<OrderWriteResult> ReplaceItemsAsync(OrderInput order, DateTime nowUtc, CancellationToken cancellationToken)
        {
            return Task.FromResult(Write(order, nowUtc));
        }

        private OrderWriteResult Write(OrderInput order, DateTime nowUtc)
        {
            WriteCount++;
            return NextStatus == OrderWriteStatus.Saved
                ? new OrderWriteResult(NextStatus, new OrderRecord(order.Name, order.Date, order.Items, nowUtc, nowUtc))
                : new OrderWriteResult(NextStatus);
        }
    }
}
