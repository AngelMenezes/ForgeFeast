namespace ForgeFeast.Api.Features.Health;

public interface IHealthRepository
{
    /// <summary>Executa uma consulta mínima no banco; lança exceção se ele estiver indisponível.</summary>
    Task PingDatabaseAsync(CancellationToken cancellationToken);
}
