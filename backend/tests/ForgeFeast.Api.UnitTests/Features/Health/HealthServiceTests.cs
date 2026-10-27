using ForgeFeast.Api.Features.Health;
using Microsoft.Extensions.Logging.Abstractions;

namespace ForgeFeast.Api.UnitTests.Features.Health;

public sealed class HealthServiceTests
{
    [Fact]
    public async Task CheckAsync_DatabaseResponds_ReturnsHealthy()
    {
        var service = new HealthService(new FakeHealthRepository(), NullLogger<HealthService>.Instance);

        var report = await service.CheckAsync(CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, report.Status);
        var check = Assert.Single(report.Checks);
        Assert.Equal(HealthService.DatabaseCheckName, check.Name);
        Assert.Equal(HealthStatus.Healthy, check.Status);
    }

    [Fact]
    public async Task CheckAsync_DatabaseThrows_ReturnsUnhealthy()
    {
        var repository = new FakeHealthRepository { Failure = new InvalidOperationException("sem conexão") };
        var service = new HealthService(repository, NullLogger<HealthService>.Instance);

        var report = await service.CheckAsync(CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal(HealthStatus.Unhealthy, Assert.Single(report.Checks).Status);
    }

    [Fact]
    public async Task CheckAsync_RequestCancelled_PropagatesCancellation()
    {
        var repository = new FakeHealthRepository { Failure = new OperationCanceledException() };
        var service = new HealthService(repository, NullLogger<HealthService>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.CheckAsync(CancellationToken.None));
    }

    private sealed class FakeHealthRepository : IHealthRepository
    {
        public Exception? Failure { get; init; }

        public Task PingDatabaseAsync(CancellationToken cancellationToken)
        {
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
    }
}
