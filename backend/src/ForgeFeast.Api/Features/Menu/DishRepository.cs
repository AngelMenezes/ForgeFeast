using System.Data;
using Dapper;
using ForgeFeast.Api.Infrastructure.Database;

namespace ForgeFeast.Api.Features.Menu;

public sealed class DishRepository : IDishRepository
{
    private const string Columns = "Id, [Date], Name, Description";
    private const string OutputColumns = "INSERTED.Id, INSERTED.[Date], INSERTED.Name, INSERTED.Description";

    private readonly ISqlConnectionFactory _connectionFactory;

    public DishRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<DishResponse>> ListByDateRangeAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        AddDate(parameters, "From", startDate);
        AddDate(parameters, "To", endDate);

        var command = new CommandDefinition(
            $"SELECT {Columns} FROM dbo.Dish WHERE [Date] BETWEEN @From AND @To ORDER BY [Date], Id;",
            parameters,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<DishRow>(command);
        return rows.Select(row => row.ToResponse()).ToList();
    }

    public async Task<DishResponse> InsertAsync(DishInput dish, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        AddDish(parameters, dish);

        var command = new CommandDefinition(
            $"INSERT INTO dbo.Dish ([Date], Name, Description) OUTPUT {OutputColumns} VALUES (@Date, @Name, @Description);",
            parameters,
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleAsync<DishRow>(command);
        return row.ToResponse();
    }

    public async Task<DishResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            $"SELECT {Columns} FROM dbo.Dish WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<DishRow>(command);
        return row?.ToResponse();
    }

    public async Task<DishResponse?> UpdateAsync(int id, DishInput dish, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("Id", id);
        AddDish(parameters, dish);

        var command = new CommandDefinition(
            $"UPDATE dbo.Dish SET [Date] = @Date, Name = @Name, Description = @Description OUTPUT {OutputColumns} WHERE Id = @Id;",
            parameters,
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<DishRow>(command);
        return row?.ToResponse();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            "DELETE FROM dbo.Dish WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken);

        var affected = await connection.ExecuteAsync(command);
        return affected > 0;
    }

    // DateOnly não é mapeado pelo SqlClient: enviamos DateTime com DbType.Date para manter o índice utilizável.
    private static void AddDate(DynamicParameters parameters, string name, DateOnly value)
    {
        parameters.Add(name, value.ToDateTime(TimeOnly.MinValue), DbType.Date);
    }

    private static void AddDish(DynamicParameters parameters, DishInput dish)
    {
        AddDate(parameters, "Date", dish.Date);
        parameters.Add("Name", dish.Name, DbType.String, size: MenuValidator.NameMaxLength);
        parameters.Add("Description", dish.Description, DbType.String, size: MenuValidator.DescriptionMaxLength);
    }

    private sealed class DishRow
    {
        public int Id { get; init; }

        public DateTime Date { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Description { get; init; }

        public DishResponse ToResponse()
        {
            return new DishResponse(Id, DateOnly.FromDateTime(Date), Name, Description);
        }
    }
}
