using Dapper;
using ForgeFeast.Api.Features.Menu;
using ForgeFeast.Api.Infrastructure.Database;

namespace ForgeFeast.Api.Features.Orders;

/// <summary>Implementa a porta do Menu consultando OrderItem (ADR-017).</summary>
public sealed class DishOrderLookup : IDishOrderLookup
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public DishOrderLookup(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> HasOrdersAsync(int dishId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.OrderItem WHERE DishId = @DishId) THEN 1 ELSE 0 END;",
            new { DishId = dishId },
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }
}
