using System.Net;
using System.Text.Json;

namespace ForgeFeast.Api.IntegrationTests;

/// <summary>Não precisa de Docker: aponta a API para uma porta sem SQL Server.</summary>
public sealed class HealthEndpointDatabaseDownTests
{
    private const string UnreachableConnectionString =
        "Server=localhost,1;Database=ForgeFeast;User Id=sa;Password=nao-usada;TrustServerCertificate=True;Connect Timeout=2";

    [Fact]
    public async Task GetHealth_DatabaseUnreachable_Returns503Unhealthy()
    {
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Unhealthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("database", body.RootElement.GetProperty("checks")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetUnknownRoute_Returns404AsProblemDetails()
    {
        await using var factory = new ApiFactory(UnreachableConnectionString, applyMigrations: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
