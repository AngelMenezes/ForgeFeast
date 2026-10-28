using ForgeFeast.Api.Features.Orders;

namespace ForgeFeast.Api.UnitTests.Features.Orders;

public sealed class OrderValidatorTests
{
    private static OrderItemRequest[] ValidItems => [new(3, 1), new(4, 2)];

    [Fact]
    public void ValidateOrder_ValidRequest_ReturnsTrimmedInput()
    {
        var result = OrderValidator.ValidateOrder("  Ana Souza ", "2026-10-28", ValidItems);

        Assert.True(result.IsValid);
        Assert.Equal("Ana Souza", result.Value?.Name);
        Assert.Equal(new DateOnly(2026, 10, 28), result.Value?.Date);
        Assert.Equal([new OrderItemInput(3, 1), new OrderItemInput(4, 2)], result.Value?.Items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateOrder_BlankName_ErrorsOnName(string? name)
    {
        var result = OrderValidator.ValidateOrder(name, "2026-10-28", ValidItems);

        Assert.Equal([OrderValidator.NameRequiredMessage], result.Errors["name"]);
    }

    [Fact]
    public void ValidateOrder_NameOver60AfterTrim_ErrorsOnName()
    {
        var result = OrderValidator.ValidateOrder($" {new string('a', 61)} ", "2026-10-28", ValidItems);

        Assert.Equal([OrderValidator.NameTooLongMessage], result.Errors["name"]);
    }

    [Fact]
    public void ValidateOrder_NameWith60Characters_IsValid()
    {
        var result = OrderValidator.ValidateOrder(new string('a', 60), "2026-10-28", ValidItems);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("2026-10-31")]
    [InlineData("2026-11-01")]
    public void ValidateOrder_Weekend_ErrorsOnDate(string date)
    {
        var result = OrderValidator.ValidateOrder("Ana", date, ValidItems);

        Assert.Equal(["Escolha um dia útil (segunda a sexta)."], result.Errors["date"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("28/10/2026")]
    [InlineData("abc")]
    public void ValidateOrder_InvalidDate_ErrorsOnDate(string? date)
    {
        var result = OrderValidator.ValidateOrder("Ana", date, ValidItems);

        Assert.True(result.Errors.ContainsKey("date"));
    }

    [Fact]
    public void ValidateOrder_NoItems_ErrorsOnItems()
    {
        Assert.Equal([OrderValidator.ItemsRequiredMessage], OrderValidator.ValidateOrder("Ana", "2026-10-28", []).Errors["items"]);
        Assert.Equal([OrderValidator.ItemsRequiredMessage], OrderValidator.ValidateOrder("Ana", "2026-10-28", null).Errors["items"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void ValidateOrder_QuantityOutOfRange_ErrorsOnItemsWithDishId(int quantity)
    {
        var result = OrderValidator.ValidateOrder("Ana", "2026-10-28", [new OrderItemRequest(3, quantity)]);

        Assert.Equal([OrderValidator.QuantityMessage(3)], result.Errors["items"]);
    }

    [Fact]
    public void ValidateOrder_MissingQuantity_ErrorsOnItems()
    {
        var result = OrderValidator.ValidateOrder("Ana", "2026-10-28", [new OrderItemRequest(3, null)]);

        Assert.Equal([OrderValidator.QuantityMessage(3)], result.Errors["items"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void ValidateOrder_InvalidDishId_ErrorsOnItemsOnce(int? dishId)
    {
        var result = OrderValidator.ValidateOrder(
            "Ana",
            "2026-10-28",
            [new OrderItemRequest(dishId, 1), new OrderItemRequest(dishId, 2)]);

        Assert.Equal([OrderValidator.InvalidDishMessage], result.Errors["items"]);
    }

    [Fact]
    public void ValidateOrder_NullItemEntry_ErrorsOnItems()
    {
        var result = OrderValidator.ValidateOrder("Ana", "2026-10-28", [null]);

        Assert.Equal([OrderValidator.InvalidDishMessage], result.Errors["items"]);
    }

    [Fact]
    public void ValidateOrder_RepeatedDish_ErrorsOnItems()
    {
        var result = OrderValidator.ValidateOrder("Ana", "2026-10-28", [new OrderItemRequest(3, 1), new OrderItemRequest(3, 2)]);

        Assert.Equal([OrderValidator.DuplicateDishMessage(3)], result.Errors["items"]);
    }

    [Fact]
    public void ValidateOrder_MoreThan20Items_ErrorsOnItems()
    {
        var items = Enumerable.Range(1, 21).Select(id => new OrderItemRequest(id, 1)).ToList();

        var result = OrderValidator.ValidateOrder("Ana", "2026-10-28", items);

        Assert.Equal([OrderValidator.TooManyItemsMessage], result.Errors["items"]);
    }

    [Fact]
    public void ValidateOrder_NameAndItemsInvalid_ReportsBothFields()
    {
        var result = OrderValidator.ValidateOrder("", "2026-10-28", []);

        Assert.Contains("name", result.Errors.Keys);
        Assert.Contains("items", result.Errors.Keys);
    }

    [Fact]
    public void ValidateKey_WeekendAllowed_IsValid()
    {
        var result = OrderValidator.ValidateKey("Ana", "2026-10-31", allowWeekend: true);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateKey_WeekendNotAllowed_ErrorsOnDate()
    {
        var result = OrderValidator.ValidateKey("Ana", "2026-10-31", allowWeekend: false);

        Assert.True(result.Errors.ContainsKey("date"));
    }

    [Fact]
    public void ValidateWindowDate_Weekend_ErrorsOnDate()
    {
        Assert.True(OrderValidator.ValidateWindowDate("2026-11-01").Errors.ContainsKey("date"));
        Assert.True(OrderValidator.ValidateWindowDate("2026-11-02").IsValid);
    }
}
