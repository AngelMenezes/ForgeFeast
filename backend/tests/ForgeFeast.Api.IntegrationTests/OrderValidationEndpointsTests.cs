using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;

namespace ForgeFeast.Api.IntegrationTests;

/// <summary>
/// Não precisa de Docker: cobre as respostas que saem antes de qualquer acesso ao banco
/// (validação de formato e janela de pedidos), com o relógio simulado.
/// </summary>
public sealed class OrderValidationEndpointsTests
{
    private const string UnreachableConnectionString =
        "Server=localhost,1;Database=ForgeFeast;User Id=sa;Password=nao-usada;TrustServerCertificate=True;Connect Timeout=2";

    private static DateTimeOffset TenAm => new(2026, 10, 28, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PostOrder_EmptyItems_Returns400OnItems()
    {
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false, new FakeTimeProvider(TenAm));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/orders",
            new { name = "Ana", date = "2026-10-28", items = Array.Empty<object>() });

        await AssertValidationErrorAsync(response, "items", "Adicione ao menos um prato ao pedido.");
    }

    [Fact]
    public async Task PostOrder_QuantityBelowOne_Returns400OnItems()
    {
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false, new FakeTimeProvider(TenAm));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/orders",
            new { name = "Ana", date = "2026-10-28", items = new[] { new { dishId = 3, quantity = 0 } } });

        await AssertValidationErrorAsync(response, "items", "A quantidade do prato 3 deve ser de 1 a 10.");
    }

    [Theory]
    [InlineData("2026-10-31")]
    [InlineData("2026-11-01")]
    public async Task PostAndPut_Weekend_Return400OnDate(string date)
    {
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false, new FakeTimeProvider(TenAm));
        using var client = factory.CreateClient();
        var items = new[] { new { dishId = 3, quantity = 1 } };

        using var post = await client.PostAsJsonAsync("/api/orders", new { name = "Ana", date, items });
        using var put = await client.PutAsJsonAsync($"/api/orders?name=Ana&date={date}", new { items });

        await AssertValidationErrorAsync(post, "date", "Escolha um dia útil (segunda a sexta).");
        await AssertValidationErrorAsync(put, "date", "Escolha um dia útil (segunda a sexta).");
    }

    [Fact]
    public async Task PostOrder_BlankName_Returns400OnName()
    {
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false, new FakeTimeProvider(TenAm));
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/orders",
            new { name = "  ", date = "2026-10-28", items = new[] { new { dishId = 3, quantity = 1 } } });

        await AssertValidationErrorAsync(response, "name", "Informe o seu nome.");
    }

    [Fact]
    public async Task PostOrder_MalformedJson_Returns400()
    {
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false, new FakeTimeProvider(TenAm));
        using var client = factory.CreateClient();
        using var content = new StringContent("{ nao eh json", System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/orders", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_MissingNameAndDate_Returns400OnBothFields()
    {
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false, new FakeTimeProvider(TenAm));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("name", out _));
        Assert.True(errors.TryGetProperty("date", out _));
    }

    [Theory]
    [InlineData("2026-10-28T14:59:59Z", true)]
    [InlineData("2026-10-28T15:00:00Z", false)]
    [InlineData("2026-10-28T20:00:00Z", false)]
    public async Task GetWindow_Today_ReflectsClockAroundCutoff(string utcNow, bool expectedOpen)
    {
        var clock = new FakeTimeProvider(DateTimeOffset.Parse(utcNow, System.Globalization.CultureInfo.InvariantCulture));
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false, clock);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/orders/window?date=2026-10-28");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("12:00", body.RootElement.GetProperty("cutoffTime").GetString());
        Assert.Equal("America/Sao_Paulo", body.RootElement.GetProperty("timeZone").GetString());
        Assert.Equal(expectedOpen, body.RootElement.GetProperty("isOpen").GetBoolean());
        Assert.Equal(
            DateTimeOffset.Parse("2026-10-28T12:00:00-03:00", System.Globalization.CultureInfo.InvariantCulture),
            body.RootElement.GetProperty("closesAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task GetWindow_PastAndFutureDates_ReflectToday()
    {
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false, new FakeTimeProvider(TenAm));
        using var client = factory.CreateClient();

        using var past = await client.GetAsync("/api/orders/window?date=2026-10-27");
        using var future = await client.GetAsync("/api/orders/window?date=2026-10-29");

        Assert.False(await IsOpenAsync(past));
        Assert.True(await IsOpenAsync(future));
    }

    [Fact]
    public async Task GetWindow_Weekend_Returns400OnDate()
    {
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false, new FakeTimeProvider(TenAm));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/orders/window?date=2026-10-31");

        await AssertValidationErrorAsync(response, "date", "Escolha um dia útil (segunda a sexta).");
    }

    [Fact]
    public async Task GetWindow_ConfiguredCutoff_ChangesBehaviorWithoutCodeChange()
    {
        await using var factory = new ApiFactory(
            UnreachableConnectionString,
            applyMigrations: false,
            new FakeTimeProvider(TenAm),
            cutoffTime: "10:00");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/orders/window?date=2026-10-28");

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("10:00", body.RootElement.GetProperty("cutoffTime").GetString());
        Assert.False(body.RootElement.GetProperty("isOpen").GetBoolean());
    }

    private static async Task<bool> IsOpenAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("isOpen").GetBoolean();
    }

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field, string message)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Um ou mais campos são inválidos.", body.RootElement.GetProperty("title").GetString());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty(field, out var messages), $"Sem erro no campo '{field}'.");
        Assert.Contains(message, messages.EnumerateArray().Select(element => element.GetString()));
    }
}
