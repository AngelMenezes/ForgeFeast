using System.Data;
using System.Data.Common;
using Dapper;
using ForgeFeast.Api.Infrastructure.Database;
using Microsoft.Data.SqlClient;

namespace ForgeFeast.Api.Features.Orders;

public sealed class OrderRepository : IOrderRepository
{
    private const int SqlUniqueIndexViolation = 2601;
    private const int SqlUniqueConstraintViolation = 2627;
    private const int SqlForeignKeyViolation = 547;
    private const string DishForeignKeyName = "FK_OrderItem_Dish";

    private readonly ISqlConnectionFactory _connectionFactory;

    public OrderRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<OrderRecord?> GetAsync(OrderKey key, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            """
            SELECT o.CustomerName, o.[Date], o.CreatedAtUtc, o.UpdatedAtUtc, i.DishId, i.Quantity
            FROM dbo.[Order] AS o
            INNER JOIN dbo.OrderItem AS i ON i.OrderId = o.Id
            WHERE o.CustomerName = @Name AND o.[Date] = @Date
            ORDER BY i.DishId;
            """,
            KeyParameters(key),
            cancellationToken: cancellationToken);

        var rows = (await connection.QueryAsync<OrderRow>(command)).ToList();
        if (rows.Count == 0)
        {
            return null;
        }

        var first = rows[0];
        return new OrderRecord(
            first.CustomerName,
            DateOnly.FromDateTime(first.Date),
            rows.Select(row => new OrderItemInput(row.DishId, row.Quantity)).ToList(),
            DateTime.SpecifyKind(first.CreatedAtUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(first.UpdatedAtUtc, DateTimeKind.Utc));
    }

    public async Task<OrderWriteResult> CreateAsync(OrderInput order, DateTime nowUtc, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var parameters = KeyParameters(new OrderKey(order.Name, order.Date));
            parameters.Add("Now", nowUtc, DbType.DateTime2);

            var insertOrder = new CommandDefinition(
                """
                INSERT INTO dbo.[Order] (CustomerName, [Date], CreatedAtUtc, UpdatedAtUtc)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Date, @Now, @Now);
                """,
                parameters,
                transaction,
                cancellationToken: cancellationToken);
            var orderId = await connection.QuerySingleAsync<int>(insertOrder);

            await InsertItemsAsync(connection, transaction, orderId, order.Items, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (SqlException exception) when (exception.Number is SqlUniqueIndexViolation or SqlUniqueConstraintViolation)
        {
            return new OrderWriteResult(OrderWriteStatus.AlreadyExists);
        }
        catch (SqlException exception) when (IsDishForeignKeyViolation(exception))
        {
            return new OrderWriteResult(OrderWriteStatus.DishNotFound);
        }

        var saved = new OrderRecord(order.Name, order.Date, order.Items, nowUtc, nowUtc);
        return new OrderWriteResult(OrderWriteStatus.Saved, saved);
    }

    public async Task<OrderWriteResult> ReplaceItemsAsync(OrderInput order, DateTime nowUtc, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var findOrder = new CommandDefinition(
                """
                SELECT Id AS OrderId, CustomerName, CreatedAtUtc
                FROM dbo.[Order] WITH (UPDLOCK, HOLDLOCK)
                WHERE CustomerName = @Name AND [Date] = @Date;
                """,
                KeyParameters(new OrderKey(order.Name, order.Date)),
                transaction,
                cancellationToken: cancellationToken);
            var existing = await connection.QuerySingleOrDefaultAsync<ExistingOrderRow>(findOrder);
            if (existing is null)
            {
                return new OrderWriteResult(OrderWriteStatus.NotFound);
            }

            var touchParameters = new DynamicParameters();
            touchParameters.Add("OrderId", existing.OrderId);
            touchParameters.Add("Now", nowUtc, DbType.DateTime2);

            var touch = new CommandDefinition(
                "UPDATE dbo.[Order] SET UpdatedAtUtc = @Now WHERE Id = @OrderId; DELETE FROM dbo.OrderItem WHERE OrderId = @OrderId;",
                touchParameters,
                transaction,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(touch);

            await InsertItemsAsync(connection, transaction, existing.OrderId, order.Items, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var saved = new OrderRecord(
                existing.CustomerName,
                order.Date,
                order.Items,
                DateTime.SpecifyKind(existing.CreatedAtUtc, DateTimeKind.Utc),
                nowUtc);
            return new OrderWriteResult(OrderWriteStatus.Saved, saved);
        }
        catch (SqlException exception) when (IsDishForeignKeyViolation(exception))
        {
            return new OrderWriteResult(OrderWriteStatus.DishNotFound);
        }
    }

    private static Task<int> InsertItemsAsync(
        DbConnection connection,
        DbTransaction transaction,
        int orderId,
        IReadOnlyList<OrderItemInput> items,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            "INSERT INTO dbo.OrderItem (OrderId, DishId, Quantity) VALUES (@OrderId, @DishId, @Quantity);",
            items.Select(item => new { OrderId = orderId, item.DishId, item.Quantity }).ToList(),
            transaction,
            cancellationToken: cancellationToken);
        return connection.ExecuteAsync(command);
    }

    // O prato removido entre a validação e o INSERT viola FK_OrderItem_Dish (ADR-017).
    private static bool IsDishForeignKeyViolation(SqlException exception)
    {
        return exception.Number == SqlForeignKeyViolation
            && exception.Message.Contains(DishForeignKeyName, StringComparison.Ordinal);
    }

    // O collation CI da coluna CustomerName vale na comparação (ADR-016).
    private static DynamicParameters KeyParameters(OrderKey key)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Name", key.Name, DbType.String, size: OrderValidator.NameMaxLength);
        parameters.Add("Date", key.Date.ToDateTime(TimeOnly.MinValue), DbType.Date);
        return parameters;
    }

    private sealed class OrderRow
    {
        public string CustomerName { get; init; } = string.Empty;

        public DateTime Date { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime UpdatedAtUtc { get; init; }

        public int DishId { get; init; }

        public byte Quantity { get; init; }
    }

    private sealed class ExistingOrderRow
    {
        public int OrderId { get; init; }

        public string CustomerName { get; init; } = string.Empty;

        public DateTime CreatedAtUtc { get; init; }
    }
}
