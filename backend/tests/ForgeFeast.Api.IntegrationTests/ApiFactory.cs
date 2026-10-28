using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeFeast.Api.IntegrationTests;

/// <summary>Sobe a API em memória com a connection string e as migrações definidas pelo teste (ADR-009).</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly bool _applyMigrations;
    private readonly TimeProvider? _timeProvider;
    private readonly string? _cutoffTime;

    /// <param name="timeProvider">Quando informado, substitui o relógio da API (ADR-015, RNF-04).</param>
    /// <param name="cutoffTime">Quando informado, define Ordering:CutoffTime (HH:mm).</param>
    public ApiFactory(string connectionString, bool applyMigrations, TimeProvider? timeProvider = null, string? cutoffTime = null)
    {
        _connectionString = connectionString;
        _applyMigrations = applyMigrations;
        _timeProvider = timeProvider;
        _cutoffTime = cutoffTime;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:ForgeFeast"] = _connectionString,
                ["Database:ApplyMigrationsOnStartup"] = _applyMigrations.ToString(),
            };
            if (_cutoffTime is not null)
            {
                settings["Ordering:CutoffTime"] = _cutoffTime;
            }

            configuration.AddInMemoryCollection(settings);
        });

        if (_timeProvider is not null)
        {
            builder.ConfigureTestServices(services => services.AddSingleton(_timeProvider));
        }
    }
}
