using DbUp;
using Microsoft.Extensions.Options;

namespace ForgeFeast.Api.Infrastructure.Database;

/// <summary>Aplica os scripts SQL embutidos em Migrations/ com DbUp (ADR-005).</summary>
public sealed class DatabaseMigrator
{
    private const string ScriptsNamespace = "ForgeFeast.Api.Infrastructure.Database.Migrations.";

    private readonly string _connectionString;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(IOptions<DatabaseOptions> options, ILogger<DatabaseMigrator> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    /// <exception cref="InvalidOperationException">Quando algum script falha (a API não deve subir).</exception>
    public void Migrate()
    {
        EnsureDatabase.For.SqlDatabase(_connectionString);

        var upgrader = DeployChanges.To
            .SqlDatabase(_connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(DatabaseMigrator).Assembly,
                scriptName => scriptName.StartsWith(ScriptsNamespace, StringComparison.Ordinal))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            _logger.LogError(
                result.Error,
                "Falha ao aplicar o script de migração {Script}",
                result.ErrorScript?.Name);
            throw new InvalidOperationException(
                $"Falha na migração do banco no script '{result.ErrorScript?.Name}'.",
                result.Error);
        }

        _logger.LogInformation(
            "Migrações do banco aplicadas ({Count} script(s) novo(s))",
            result.Scripts.Count());
    }
}
