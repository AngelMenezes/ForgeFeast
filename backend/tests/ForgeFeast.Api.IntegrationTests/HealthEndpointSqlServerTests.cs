using System.Net;
using System.Text.Json;
using Testcontainers.MsSql;

namespace ForgeFeast.Api.IntegrationTests;

/// <summary>Usa um SQL Server real via Testcontainers. Requer Docker ativo.</summary>
[Trait("Category", "RequiresDocker")]
public sealed class HealthEndpointSqlServerTests : IAsyncLifetime
{
    private const string DefaultImage = "mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04";

    private readonly MsSqlContainer _sqlServer =
        new MsSqlBuilder(Environment.GetEnvironmentVariable("MSSQL_IMAGE") ?? DefaultImage).Build();

    public async Task InitializeAsync()
    {
        await _sqlServer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlServer.DisposeAsync();
    }

    [Fact]
    public async Task GetHealth_DatabaseAvailableWithMigrations_Returns200Healthy()
    {
        await using var factory = new ApiFactory(_sqlServer.GetConnectionString(), applyMigrations: true);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("Healthy", body.RootElement.GetProperty("checks")[0].GetProperty("status").GetString());
    }
}
