using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.MsSql;
using Xunit.Abstractions;

namespace ForgeFeast.Api.IntegrationTests;

/// <summary>SQL Server real via Testcontainers e relógio falso compartilhados pela classe de testes. Requer Docker ativo.</summary>
#pragma warning disable CA1001 // O xUnit descarta a fixture por IAsyncLifetime.DisposeAsync, que não é IAsyncDisposable.
public sealed class OrdersApiFixture : IAsyncLifetime
#pragma warning restore CA1001
{
    private const string DefaultImage = "mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04";

    private readonly MsSqlContainer _sqlServer =
        new MsSqlBuilder(Environment.GetEnvironmentVariable("MSSQL_IMAGE") ?? DefaultImage).Build();

    private ApiFactory? _factory;
    private HttpClient? _client;

    /// <summary>Hoje é 28/10/2026 (quarta), 10:00 em São Paulo. Os testes ajustam o relógio quando precisam.</summary>
    public FakeTimeProvider Clock { get; } = new(OrderEndpointsTests.TenAm);

    public HttpClient Client => _client ?? throw new InvalidOperationException("Fixture não inicializada.");

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();
        _factory = new ApiFactory(_sqlServer.GetConnectionString(), applyMigrations: true, Clock);
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

/// <remarks>
/// Cada teste usa nome de cliente próprio (a chave é nome + data) e datas de prato escolhidas para não colidir.
/// O relógio é compartilhado: cada teste que depende da hora a define no início (o xUnit não paraleliza uma classe).
/// </remarks>
[Trait("Category", "RequiresDocker")]
public sealed class OrderEndpointsTests : IClassFixture<OrdersApiFixture>
{
    internal static DateTimeOffset TenAm => new(2026, 10, 28, 13, 0, 0, TimeSpan.Zero);

    private const string Today = "2026-10-28";
    private const string Tomorrow = "2026-10-29";

    private readonly OrdersApiFixture _fixture;
    private readonly ITestOutputHelper _output;

    public OrderEndpointsTests(OrdersApiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _fixture.Clock.SetUtcNow(TenAm);
    }

    private HttpClient Client => _fixture.Client;

    [Fact]
    public async Task PostOrder_ValidOrder_Returns201WithLocationAndGetReturnsSameItems()
    {
        var chicken = await PostDishAsync(Today, "Frango grelhado");
        var salad = await PostDishAsync(Today, "Salada verde");

        using var response = await PostOrderAsync("Ana Souza", Today, (salad, 2), (chicken, 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/orders?name=Ana%20Souza&date={Today}", response.Headers.Location?.ToString());
        var created = await ReadJsonAsync(response);
        Assert.Equal("Ana Souza", created.GetProperty("name").GetString());

        using var get = await Client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var order = await ReadJsonAsync(get);
        Assert.Equal([(chicken, "Frango grelhado", 1), (salad, "Salada verde", 2)], ReadItems(order));
        Assert.Equal(created.GetProperty("createdAt").GetString(), order.GetProperty("createdAt").GetString());
    }

    [Fact]
    public async Task PostOrder_NameWithSpaces_StoresTrimmedName()
    {
        var dish = await PostDishAsync(Today, "Prato do trim");

        using var response = await PostOrderAsync("  Beatriz  ", Today, (dish, 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Beatriz", (await ReadJsonAsync(response)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task PostOrder_EmptyItems_Returns400OnItemsAndStoresNothing()
    {
        using var response = await Client.PostAsJsonAsync("/api/orders", new { name = "Vazio", date = Today, items = Array.Empty<object>() });

        await AssertValidationErrorAsync(response, "items");
        await AssertOrderNotFoundAsync("Vazio", Today);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task PostOrder_QuantityOutOfRange_Returns400OnItems(int quantity)
    {
        var dish = await PostDishAsync(Today, $"Prato quantidade {quantity}");
        var name = $"Quantidade {quantity}";

        using var response = await PostOrderAsync(name, Today, (dish, quantity));

        await AssertValidationErrorAsync(response, "items");
        await AssertOrderNotFoundAsync(name, Today);
    }

    [Fact]
    public async Task PostOrder_BlankName_Returns400OnName()
    {
        var dish = await PostDishAsync(Today, "Prato sem nome");

        using var response = await PostOrderAsync("   ", Today, (dish, 1));

        await AssertValidationErrorAsync(response, "name");
    }

    [Theory]
    [InlineData("2026-10-31")]
    [InlineData("2026-11-01")]
    public async Task PostOrder_Weekend_Returns400OnDate(string date)
    {
        using var response = await PostOrderAsync("Fim de semana", date, (1, 1));

        await AssertValidationErrorAsync(response, "date");
    }

    [Fact]
    public async Task PostOrder_MalformedJson_Returns400()
    {
        using var content = new StringContent("{ nao eh json", System.Text.Encoding.UTF8, "application/json");

        using var response = await Client.PostAsync("/api/orders", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostOrder_UnknownDish_Returns400OnItemsAndStoresNothing()
    {
        var valid = await PostDishAsync(Today, "Prato válido");

        using var response = await PostOrderAsync("Prato inexistente", Today, (valid, 1), (999_999, 1));

        await AssertValidationErrorAsync(response, "items");
        await AssertOrderNotFoundAsync("Prato inexistente", Today);
    }

    [Fact]
    public async Task PostOrder_DishFromAnotherDate_Returns400OnItems()
    {
        var tomorrowDish = await PostDishAsync(Tomorrow, "Prato de amanhã");

        using var response = await PostOrderAsync("Outro dia", Today, (tomorrowDish, 1));

        await AssertValidationErrorAsync(response, "items");
        await AssertOrderNotFoundAsync("Outro dia", Today);
    }

    [Fact]
    public async Task PostOrder_SameNameAndDate_Returns409AndKeepsOriginalOrder()
    {
        var first = await PostDishAsync(Today, "Primeiro prato");
        var second = await PostDishAsync(Today, "Segundo prato");
        using var created = await PostOrderAsync("Carla", Today, (first, 1));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var duplicate = await PostOrderAsync("  CARLA ", Today, (second, 3));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var problem = await ReadJsonAsync(duplicate);
        Assert.Equal("Pedido já existe.", problem.GetProperty("title").GetString());
        var order = await GetOrderAsync("Carla", Today);
        Assert.Equal([(first, "Primeiro prato", 1)], ReadItems(order));
    }

    [Fact]
    public async Task PostOrder_SameNameOnDifferentDate_Returns201()
    {
        var todayDish = await PostDishAsync(Today, "Hoje mesmo nome");
        var tomorrowDish = await PostDishAsync(Tomorrow, "Amanhã mesmo nome");
        using var first = await PostOrderAsync("Diana", Today, (todayDish, 1));

        using var second = await PostOrderAsync("Diana", Tomorrow, (tomorrowDish, 1));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task PostOrder_ConcurrentSameNameAndDate_ReturnsOneCreatedAndOneConflict()
    {
        var dish = await PostDishAsync(Today, "Prato concorrente");

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ => PostOrderAsync("Concorrente", Today, (dish, 1))));
        try
        {
            var statuses = responses.Select(response => response.StatusCode).Order().ToList();
            Assert.Equal([HttpStatusCode.Created, HttpStatusCode.Conflict], statuses);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task PostOrder_AtFirstSecondBeforeCutoff_Returns201()
    {
        var dish = await PostDishAsync(Today, "Prato 11:59");
        _fixture.Clock.SetUtcNow(new DateTimeOffset(2026, 10, 28, 14, 59, 59, TimeSpan.Zero));

        using var response = await PostOrderAsync("Onze e cinquenta e nove", Today, (dish, 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostOrder_AtCutoff_Returns422AndStoresNothing()
    {
        var dish = await PostDishAsync(Today, "Prato 12:00");
        _fixture.Clock.SetUtcNow(new DateTimeOffset(2026, 10, 28, 15, 0, 0, TimeSpan.Zero));

        using var response = await PostOrderAsync("Meio-dia", Today, (dish, 1));

        await AssertClosedAsync(response, "Os pedidos de 28/10/2026 foram encerrados às 12:00.");
        await AssertOrderNotFoundAsync("Meio-dia", Today);
    }

    [Fact]
    public async Task PostOrder_PastDate_Returns422AndStoresNothing()
    {
        var dish = await PostDishAsync("2026-10-27", "Prato de ontem");

        using var response = await PostOrderAsync("Retroativo", "2026-10-27", (dish, 1));

        await AssertClosedAsync(response, "Não é possível enviar ou alterar pedidos de datas passadas.");
        await AssertOrderNotFoundAsync("Retroativo", "2026-10-27");
    }

    [Fact]
    public async Task PostOrder_FutureDateAfterTodaysCutoff_Returns201()
    {
        var dish = await PostDishAsync(Tomorrow, "Prato futuro");
        _fixture.Clock.SetUtcNow(new DateTimeOffset(2026, 10, 28, 20, 0, 0, TimeSpan.Zero));

        using var response = await PostOrderAsync("Planejado", Tomorrow, (dish, 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_UnknownOrder_Returns404()
    {
        using var response = await Client.GetAsync($"/api/orders?name=Ninguem&date={Today}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Pedido não encontrado.", (await ReadJsonAsync(response)).GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetOrder_MissingName_Returns400OnName()
    {
        using var response = await Client.GetAsync($"/api/orders?date={Today}");

        await AssertValidationErrorAsync(response, "name");
    }

    [Fact]
    public async Task GetOrder_AfterCutoffAndOnPastDate_StillReturns200()
    {
        var dish = await PostDishAsync(Today, "Prato consulta");
        using var created = await PostOrderAsync("Consultora", Today, (dish, 2));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        _fixture.Clock.SetUtcNow(new DateTimeOffset(2026, 10, 30, 20, 0, 0, TimeSpan.Zero));

        var order = await GetOrderAsync("consultora", Today);

        Assert.Equal([(dish, "Prato consulta", 2)], ReadItems(order));
    }

    [Fact]
    public async Task PutOrder_NewItemsBeforeCutoff_Returns200AndGetReturnsExactlyThoseItems()
    {
        var first = await PostDishAsync(Today, "Item antigo");
        var second = await PostDishAsync(Today, "Item novo");
        using var created = await PostOrderAsync("Eduarda", Today, (first, 1));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var response = await PutOrderAsync("eduarda", Today, (second, 4));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await GetOrderAsync("Eduarda", Today);
        Assert.Equal([(second, "Item novo", 4)], ReadItems(order));
        Assert.Equal("Eduarda", order.GetProperty("name").GetString());
    }

    [Fact]
    public async Task PutOrder_AtCutoff_Returns422AndKeepsItems()
    {
        var first = await PostDishAsync(Today, "Item mantido");
        var second = await PostDishAsync(Today, "Item recusado");
        using var created = await PostOrderAsync("Fernanda", Today, (first, 1));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        _fixture.Clock.SetUtcNow(new DateTimeOffset(2026, 10, 28, 15, 0, 0, TimeSpan.Zero));

        using var response = await PutOrderAsync("Fernanda", Today, (second, 1));

        await AssertClosedAsync(response, "Os pedidos de 28/10/2026 foram encerrados às 12:00.");
        Assert.Equal([(first, "Item mantido", 1)], ReadItems(await GetOrderAsync("Fernanda", Today)));
    }

    [Fact]
    public async Task PutOrder_UnknownOrder_Returns404()
    {
        var dish = await PostDishAsync(Today, "Prato sem pedido");

        using var response = await PutOrderAsync("Fantasma", Today, (dish, 1));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertOrderNotFoundAsync("Fantasma", Today);
    }

    [Fact]
    public async Task PutOrder_Weekend_Returns400OnDate()
    {
        using var response = await PutOrderAsync("Sabado", "2026-10-31", (1, 1));

        await AssertValidationErrorAsync(response, "date");
    }

    [Fact]
    public async Task PutOrder_UnknownDish_Returns400AndKeepsItems()
    {
        var dish = await PostDishAsync(Today, "Prato que fica");
        using var created = await PostOrderAsync("Gabriela", Today, (dish, 1));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var response = await PutOrderAsync("Gabriela", Today, (999_999, 1));

        await AssertValidationErrorAsync(response, "items");
        Assert.Equal([(dish, "Prato que fica", 1)], ReadItems(await GetOrderAsync("Gabriela", Today)));
    }

    [Fact]
    public async Task PutOrder_PastDate_Returns422()
    {
        var dish = await PostDishAsync("2026-10-27", "Prato passado put");

        using var response = await PutOrderAsync("Passado", "2026-10-27", (dish, 1));

        await AssertClosedAsync(response, "Não é possível enviar ou alterar pedidos de datas passadas.");
    }

    [Fact]
    public async Task DeleteDish_DishInOrder_Returns409AndKeepsDish()
    {
        var dish = await PostDishAsync(Today, "Prato com pedido");
        using var created = await PostOrderAsync("Helena", Today, (dish, 1));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var response = await Client.DeleteAsync($"/api/dishes/{dish}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await ReadJsonAsync(response);
        Assert.Equal("Prato com pedidos.", problem.GetProperty("title").GetString());
        Assert.Equal("Este prato tem pedidos e não pode ser removido.", problem.GetProperty("detail").GetString());
        using var stillThere = await Client.GetAsync($"/api/dishes/{dish}");
        Assert.Equal(HttpStatusCode.OK, stillThere.StatusCode);
    }

    [Fact]
    public async Task DeleteDish_DishOnlyInReplacedItems_Returns204()
    {
        var oldDish = await PostDishAsync(Today, "Prato substituído");
        var newDish = await PostDishAsync(Today, "Prato substituto");
        using var created = await PostOrderAsync("Isabela", Today, (oldDish, 1));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var replaced = await PutOrderAsync("Isabela", Today, (newDish, 1));
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);

        using var response = await Client.DeleteAsync($"/api/dishes/{oldDish}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetWindow_Today_ReturnsCutoffAndOpenState()
    {
        using var open = await Client.GetAsync($"/api/orders/window?date={Today}");
        var openBody = await ReadJsonAsync(open);
        _fixture.Clock.SetUtcNow(new DateTimeOffset(2026, 10, 28, 15, 0, 0, TimeSpan.Zero));
        using var closed = await Client.GetAsync($"/api/orders/window?date={Today}");
        var closedBody = await ReadJsonAsync(closed);

        Assert.Equal("12:00", openBody.GetProperty("cutoffTime").GetString());
        Assert.Equal("America/Sao_Paulo", openBody.GetProperty("timeZone").GetString());
        Assert.Equal(DateTimeOffset.Parse("2026-10-28T12:00:00-03:00", System.Globalization.CultureInfo.InvariantCulture), openBody.GetProperty("closesAt").GetDateTimeOffset());
        Assert.True(openBody.GetProperty("isOpen").GetBoolean());
        Assert.False(closedBody.GetProperty("isOpen").GetBoolean());
    }

    [Fact]
    public async Task GetWindow_Weekend_Returns400OnDate()
    {
        using var response = await Client.GetAsync("/api/orders/window?date=2026-10-31");

        await AssertValidationErrorAsync(response, "date");
    }

    [Fact]
    public async Task OrderWrites_WithSampleData_RespondWithin500Milliseconds()
    {
        var dishes = new List<int>();
        for (var index = 1; index <= 5; index++)
        {
            dishes.Add(await PostDishAsync(Today, $"Prato desempenho {index}"));
        }

        // Aquecimento (JIT, pool de conexões) fora da medição.
        (await PostOrderAsync("Aquecimento", Today, (dishes[0], 1))).Dispose();

        var stopwatch = Stopwatch.StartNew();
        using var post = await PostOrderAsync("Desempenho", Today, [.. dishes.Select(id => (id, 2))]);
        var postMilliseconds = stopwatch.ElapsedMilliseconds;
        stopwatch.Restart();
        using var put = await PutOrderAsync("Desempenho", Today, [.. dishes.Take(3).Select(id => (id, 1))]);
        var putMilliseconds = stopwatch.ElapsedMilliseconds;

        _output.WriteLine($"RNF-01: POST {postMilliseconds} ms, PUT {putMilliseconds} ms.");
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.InRange(postMilliseconds, 0, 500);
        Assert.InRange(putMilliseconds, 0, 500);
    }

    private async Task<int> PostDishAsync(string date, string name)
    {
        using var response = await Client.PostAsJsonAsync("/api/dishes", new { date, name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetInt32();
    }

    private Task<HttpResponseMessage> PostOrderAsync(string name, string date, params (int DishId, int Quantity)[] items)
    {
        return Client.PostAsJsonAsync(
            "/api/orders",
            new { name, date, items = items.Select(item => new { dishId = item.DishId, quantity = item.Quantity }) });
    }

    private Task<HttpResponseMessage> PutOrderAsync(string name, string date, params (int DishId, int Quantity)[] items)
    {
        return Client.PutAsJsonAsync(
            $"/api/orders?name={Uri.EscapeDataString(name)}&date={date}",
            new { items = items.Select(item => new { dishId = item.DishId, quantity = item.Quantity }) });
    }

    private async Task<JsonElement> GetOrderAsync(string name, string date)
    {
        using var response = await Client.GetAsync($"/api/orders?name={Uri.EscapeDataString(name)}&date={date}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    private async Task AssertOrderNotFoundAsync(string name, string date)
    {
        using var response = await Client.GetAsync($"/api/orders?name={Uri.EscapeDataString(name)}&date={date}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static List<(int DishId, string DishName, int Quantity)> ReadItems(JsonElement order)
    {
        return order.GetProperty("items").EnumerateArray()
            .Select(item => (
                item.GetProperty("dishId").GetInt32(),
                item.GetProperty("dishName").GetString() ?? string.Empty,
                item.GetProperty("quantity").GetInt32()))
            .ToList();
    }

    private static async Task AssertClosedAsync(HttpResponseMessage response, string detail)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("Pedido encerrado.", body.GetProperty("title").GetString());
        Assert.Equal(detail, body.GetProperty("detail").GetString());
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
