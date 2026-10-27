using Dapper;
using ForgeFeast.Api.Infrastructure.Database;

namespace ForgeFeast.Api.Features.Health;

public sealed class HealthRepository : IHealthRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public HealthRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task PingDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition("SELECT 1;", commandTimeout: 5, cancellationToken: cancellationToken);
        await connection.ExecuteScalarAsync<int>(command);
    }
}
