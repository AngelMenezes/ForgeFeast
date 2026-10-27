using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ForgeFeast.Api.IntegrationTests;

/// <summary>Sobe a API em memória com a connection string e as migrações definidas pelo teste (ADR-009).</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly bool _applyMigrations;

    public ApiFactory(string connectionString, bool applyMigrations)
    {
        _connectionString = connectionString;
        _applyMigrations = applyMigrations;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ForgeFeast"] = _connectionString,
                ["Database:ApplyMigrationsOnStartup"] = _applyMigrations.ToString(),
            }));
    }
}
