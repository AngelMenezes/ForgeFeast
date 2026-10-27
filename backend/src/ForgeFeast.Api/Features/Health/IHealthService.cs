namespace ForgeFeast.Api.Features.Health;

public interface IHealthService
{
    Task<HealthReport> CheckAsync(CancellationToken cancellationToken);
}
