namespace ForgeFeast.Api.Features.Health;

public sealed class HealthService : IHealthService
{
    public const string DatabaseCheckName = "database";

    private readonly IHealthRepository _repository;
    private readonly ILogger<HealthService> _logger;

    public HealthService(IHealthRepository repository, ILogger<HealthService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<HealthReport> CheckAsync(CancellationToken cancellationToken)
    {
        var databaseStatus = await CheckDatabaseAsync(cancellationToken);
        var checks = new[] { new HealthCheckEntry(DatabaseCheckName, databaseStatus) };

        return new HealthReport(databaseStatus, checks);
    }

    private async Task<HealthStatus> CheckDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _repository.PingDatabaseAsync(cancellationToken);
            return HealthStatus.Healthy;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao verificar a conexão com o banco de dados");
            return HealthStatus.Unhealthy;
        }
    }
}
