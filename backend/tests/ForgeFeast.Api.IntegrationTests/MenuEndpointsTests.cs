using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.MsSql;

namespace ForgeFeast.Api.IntegrationTests;

/// <summary>Sobe um SQL Server real via Testcontainers, compartilhado pela classe de testes. Requer Docker ativo.</summary>
#pragma warning disable CA1001 // O xUnit descarta a fixture por IAsyncLifetime.DisposeAsync, que não é IAsyncDisposable.
public sealed class MenuApiFixture : IAsyncLifetime
#pragma warning restore CA1001
{
    private const string DefaultImage = "mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04";

    private readonly MsSqlContainer _sqlServer =
        new MsSqlBuilder(Environment.GetEnvironmentVariable("MSSQL_IMAGE") ?? DefaultImage).Build();

    private ApiFactory? _factory;
    private HttpClient? _client;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Fixture não inicializada.");

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();
        _factory = new ApiFactory(_sqlServer.GetConnectionString(), applyMigrations: true);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _sqlServer.DisposeAsync();
    }
}

/// <remarks>Cada teste usa datas próprias (semanas distintas) para não interferir nos demais.</remarks>
[Trait("Category", "RequiresDocker")]
public sealed class MenuEndpointsTests : IClassFixture<MenuApiFixture>
{
    private readonly MenuApiFixture _fixture;

    public MenuEndpointsTests(MenuApiFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClient Client => _fixture.Client;

    [Fact]
    public async Task PostDish_ValidWeekday_Returns201AndAppearsInWeekMenu()
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/dishes",
            new { date = "2026-10-27", name = "  Feijoada ", description = "Com arroz" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        var id = created.GetProperty("id").GetInt32();
        Assert.Equal($"/api/dishes/{id}", response.Headers.Location?.ToString());
        Assert.Equal("Feijoada", created.GetProperty("name").GetString());

        var menu = await GetMenuAsync("2026-10-26");
        var tuesday = menu.GetProperty("days")[1];
        Assert.Equal("2026-10-27", tuesday.GetProperty("date").GetString());
        Assert.Equal("Tuesday", tuesday.GetProperty("dayOfWeek").GetString());
        var dish = Assert.Single(tuesday.GetProperty("dishes").EnumerateArray());
        Assert.Equal(id, dish.GetProperty("id").GetInt32());
        Assert.Equal("Com arroz", dish.GetProperty("description").GetString());
    }

    [Fact]
    public async Task PostDish_WithoutDescription_ReturnsNullDescription()
    {
        using var response = await Client.PostAsJsonAsync("/api/dishes", new { date = "2026-11-02", name = "Salada" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Null, created.GetProperty("description").ValueKind);
    }

    [Theory]
    [InlineData("2026-11-07")]
    [InlineData("2026-11-08")]
    public async Task PostDish_Weekend_Returns400OnDateAndStoresNothing(string date)
    {
        using var response = await Client.PostAsJsonAsync("/api/dishes", new { date, name = "Feijoada" });

        await AssertValidationErrorAsync(response, "date");
        var menu = await GetMenuAsync("2026-11-02");
        Assert.All(menu.GetProperty("days").EnumerateArray(), day => Assert.Empty(day.GetProperty("dishes").EnumerateArray()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostDish_BlankName_Returns400OnName(string name)
    {
        using var response = await Client.PostAsJsonAsync("/api/dishes", new { date = "2026-11-09", name });

        await AssertValidationErrorAsync(response, "name");
    }

    [Fact]
    public async Task PostDish_NameOver100Characters_Returns400OnName()
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/dishes",
            new { date = "2026-11-09", name = new string('a', 101) });

        await AssertValidationErrorAsync(response, "name");
    }

    [Fact]
    public async Task PostDish_MalformedJson_Returns400()
    {
        using var content = new StringContent("{ nao eh json", System.Text.Encoding.UTF8, "application/json");

        using var response = await Client.PostAsync("/api/dishes", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("2026-10-27")]
    [InlineData("2026-10-31")]
    [InlineData("2026-11-01")]
    [InlineData("abc")]
    public async Task GetMenu_WeekStartNotMondayOrInvalid_Returns400OnWeekStart(string weekStart)
    {
        using var response = await Client.GetAsync($"/api/menu?weekStart={weekStart}");

        await AssertValidationErrorAsync(response, "weekStart");
    }

    [Fact]
    public async Task GetMenu_MissingWeekStart_Returns400OnWeekStart()
    {
        using var response = await Client.GetAsync("/api/menu");

        await AssertValidationErrorAsync(response, "weekStart");
    }

    [Fact]
    public async Task GetMenu_WeekWithoutDishes_ReturnsFiveDaysWithEmptyLists()
    {
        var menu = await GetMenuAsync("2027-03-01");

        Assert.Equal("2027-03-01", menu.GetProperty("weekStart").GetString());
        Assert.Equal("2027-03-05", menu.GetProperty("weekEnd").GetString());
        var days = menu.GetProperty("days").EnumerateArray().ToList();
        Assert.Equal(5, days.Count);
        Assert.All(days, day => Assert.Empty(day.GetProperty("dishes").EnumerateArray()));
    }

    [Fact]
    public async Task GetMenu_DishesInDifferentWeeks_ReturnsOnlyRequestedWeek()
    {
        await PostDishAsync("2027-04-05", "Semana A");
        await PostDishAsync("2027-04-12", "Semana B");

        var menu = await GetMenuAsync("2027-04-12");

        var names = menu.GetProperty("days").EnumerateArray()
            .SelectMany(day => day.GetProperty("dishes").EnumerateArray())
            .Select(dish => dish.GetProperty("name").GetString());
        Assert.Equal(["Semana B"], names);
    }

    [Fact]
    public async Task GetDish_ExistingId_Returns200()
    {
        var id = await PostDishAsync("2027-05-03", "Moqueca");

        using var response = await Client.GetAsync($"/api/dishes/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Moqueca", (await ReadJsonAsync(response)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetDish_UnknownId_Returns404()
    {
        using var response = await Client.GetAsync("/api/dishes/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutDish_ExistingId_UpdatesNameDescriptionAndDate()
    {
        var id = await PostDishAsync("2027-06-07", "Antigo");

        using var response = await Client.PutAsJsonAsync(
            $"/api/dishes/{id}",
            new { date = "2027-06-09", name = "Novo", description = "Descrição nova" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var menu = await GetMenuAsync("2027-06-07");
        var days = menu.GetProperty("days").EnumerateArray().ToList();
        Assert.Empty(days[0].GetProperty("dishes").EnumerateArray());
        var dish = Assert.Single(days[2].GetProperty("dishes").EnumerateArray());
        Assert.Equal("Novo", dish.GetProperty("name").GetString());
        Assert.Equal("Descrição nova", dish.GetProperty("description").GetString());
    }

    [Fact]
    public async Task PutDish_UnknownId_Returns404()
    {
        using var response = await Client.PutAsJsonAsync(
            "/api/dishes/999999",
            new { date = "2027-06-07", name = "Qualquer" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutDish_WeekendDate_Returns400AndKeepsDish()
    {
        var id = await PostDishAsync("2027-07-05", "Intacto");

        using var response = await Client.PutAsJsonAsync($"/api/dishes/{id}", new { date = "2027-07-10", name = "Intacto" });

        await AssertValidationErrorAsync(response, "date");
        using var current = await Client.GetAsync($"/api/dishes/{id}");
        Assert.Equal("2027-07-05", (await ReadJsonAsync(current)).GetProperty("date").GetString());
    }

    [Fact]
    public async Task PutDish_InvalidBodyOnUnknownId_Returns400BeforeNotFound()
    {
        using var response = await Client.PutAsJsonAsync("/api/dishes/999999", new { date = "2027-07-05", name = "" });

        await AssertValidationErrorAsync(response, "name");
    }

    [Fact]
    public async Task DeleteDish_ExistingId_Returns204AndRemovesFromMenu()
    {
        var id = await PostDishAsync("2027-08-02", "Para remover");

        using var response = await Client.DeleteAsync($"/api/dishes/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var menu = await GetMenuAsync("2027-08-02");
        Assert.All(menu.GetProperty("days").EnumerateArray(), day => Assert.Empty(day.GetProperty("dishes").EnumerateArray()));
    }

    [Fact]
    public async Task DeleteDish_UnknownId_Returns404()
    {
        using var response = await Client.DeleteAsync("/api/dishes/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<int> PostDishAsync(string date, string name)
    {
        using var response = await Client.PostAsJsonAsync("/api/dishes", new { date, name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }

    private async Task<JsonElement> GetMenuAsync(string weekStart)
    {
        using var response = await Client.GetAsync($"/api/menu?weekStart={weekStart}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("Um ou mais campos são inválidos.", body.GetProperty("title").GetString());
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out var messages), $"Sem erro no campo '{field}'.");
        Assert.NotEmpty(messages.EnumerateArray());
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
