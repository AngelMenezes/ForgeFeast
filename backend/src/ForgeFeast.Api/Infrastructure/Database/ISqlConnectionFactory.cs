using System.Data.Common;

namespace ForgeFeast.Api.Infrastructure.Database;

public interface ISqlConnectionFactory
{
    /// <summary>Cria e abre uma conexão. Quem chama é responsável por descartá-la.</summary>
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
