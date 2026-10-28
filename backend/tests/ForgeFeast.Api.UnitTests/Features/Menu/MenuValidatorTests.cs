using ForgeFeast.Api.Features.Menu;

namespace ForgeFeast.Api.UnitTests.Features.Menu;

public sealed class MenuValidatorTests
{
    [Fact]
    public void ValidateWeekStart_Monday_IsValid()
    {
        var result = MenuValidator.ValidateWeekStart("2026-10-26");

        Assert.True(result.IsValid);
        Assert.Equal(new DateOnly(2026, 10, 26), result.Value?.Date);
    }

    [Theory]
    [InlineData("2026-10-27")]
    [InlineData("2026-10-31")]
    [InlineData("2026-11-01")]
    public void ValidateWeekStart_NotMonday_ReturnsErrorOnWeekStart(string weekStart)
    {
        var result = MenuValidator.ValidateWeekStart(weekStart);

        Assert.False(result.IsValid);
        Assert.Equal([MenuValidator.WeekStartNotMondayMessage], result.Errors["weekStart"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("26/10/2026")]
    [InlineData("2026-13-40")]
    public void ValidateWeekStart_MissingOrMalformed_ReturnsInvalidDateMessage(string? weekStart)
    {
        var result = MenuValidator.ValidateWeekStart(weekStart);

        Assert.Equal([MenuValidator.InvalidDateMessage], result.Errors["weekStart"]);
    }

    [Theory]
    [InlineData("2026-10-26")]
    [InlineData("2026-10-30")]
    public void ValidateDish_Weekday_IsValid(string date)
    {
        var result = MenuValidator.ValidateDish(new DishRequest(date, "Feijoada", null));

        Assert.True(result.IsValid);
        Assert.Equal(DateOnly.Parse(date), result.Value?.Date);
    }

    [Theory]
    [InlineData("2026-10-31")]
    [InlineData("2026-11-01")]
    public void ValidateDish_Weekend_ReturnsErrorOnDate(string date)
    {
        var result = MenuValidator.ValidateDish(new DishRequest(date, "Feijoada", null));

        Assert.False(result.IsValid);
        Assert.Equal([MenuValidator.WeekendMessage], result.Errors["date"]);
        Assert.False(result.Errors.ContainsKey("name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2026-10-26T00:00:00")]
    [InlineData("abc")]
    public void ValidateDish_MissingOrMalformedDate_ReturnsInvalidDateMessage(string? date)
    {
        var result = MenuValidator.ValidateDish(new DishRequest(date, "Feijoada", null));

        Assert.Equal([MenuValidator.InvalidDateMessage], result.Errors["date"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateDish_BlankName_ReturnsNameRequired(string? name)
    {
        var result = MenuValidator.ValidateDish(new DishRequest("2026-10-26", name, null));

        Assert.Equal([MenuValidator.NameRequiredMessage], result.Errors["name"]);
    }

    [Fact]
    public void ValidateDish_NameWith100Characters_IsValid()
    {
        var result = MenuValidator.ValidateDish(new DishRequest("2026-10-26", new string('a', 100), null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateDish_NameWith101Characters_ReturnsNameTooLong()
    {
        var result = MenuValidator.ValidateDish(new DishRequest("2026-10-26", new string('a', 101), null));

        Assert.Equal([MenuValidator.NameTooLongMessage], result.Errors["name"]);
    }

    [Fact]
    public void ValidateDish_NameWith100CharactersPlusSpaces_IsValidAfterTrim()
    {
        var result = MenuValidator.ValidateDish(new DishRequest("2026-10-26", "  " + new string('a', 100) + "  ", null));

        Assert.True(result.IsValid);
        Assert.Equal(100, result.Value?.Name.Length);
    }

    [Fact]
    public void ValidateDish_DescriptionWith300Characters_IsValid()
    {
        var result = MenuValidator.ValidateDish(new DishRequest("2026-10-26", "Feijoada", new string('d', 300)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateDish_DescriptionWith301Characters_ReturnsDescriptionTooLong()
    {
        var result = MenuValidator.ValidateDish(new DishRequest("2026-10-26", "Feijoada", new string('d', 301)));

        Assert.Equal([MenuValidator.DescriptionTooLongMessage], result.Errors["description"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateDish_BlankDescription_BecomesNull(string? description)
    {
        var result = MenuValidator.ValidateDish(new DishRequest("2026-10-26", "Feijoada", description));

        Assert.True(result.IsValid);
        Assert.Null(result.Value?.Description);
    }

    [Fact]
    public void ValidateDish_NameAndDescriptionWithSpaces_AreTrimmed()
    {
        var result = MenuValidator.ValidateDish(new DishRequest("2026-10-26", "  Feijoada ", " Com arroz  "));

        Assert.Equal(new DishInput(new DateOnly(2026, 10, 26), "Feijoada", "Com arroz"), result.Value);
    }

    [Fact]
    public void ValidateDish_SeveralInvalidFields_ReportsAllOfThem()
    {
        var result = MenuValidator.ValidateDish(new DishRequest("2026-10-31", "", new string('d', 301)));

        Assert.Equal(["date", "description", "name"], result.Errors.Keys.Order());
    }
}
