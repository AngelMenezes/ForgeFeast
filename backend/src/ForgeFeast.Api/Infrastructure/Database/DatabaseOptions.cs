namespace ForgeFeast.Api.Infrastructure.Database;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public const string ConnectionStringName = "ForgeFeast";

    public string ConnectionString { get; set; } = string.Empty;

    public bool ApplyMigrationsOnStartup { get; set; }
}
