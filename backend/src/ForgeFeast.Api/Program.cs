using ForgeFeast.Api.Features.Health;
using ForgeFeast.Api.Features.Menu;
using ForgeFeast.Api.Features.Orders;
using ForgeFeast.Api.Infrastructure.Configuration;
using ForgeFeast.Api.Infrastructure.Database;
using ForgeFeast.Api.Infrastructure.Errors;
using Microsoft.Extensions.Options;

// ADR-009: o .env só é lido em Development e nunca sobrescreve variáveis já definidas no processo.
if (string.Equals(
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
        Environments.Development,
        StringComparison.OrdinalIgnoreCase))
{
    var envFile = DotEnvLoader.FindNearestFile(AppContext.BaseDirectory)
        ?? DotEnvLoader.FindNearestFile(Directory.GetCurrentDirectory());
    if (envFile is not null)
    {
        DotEnvLoader.Load(envFile);
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .Configure<IConfiguration>((options, configuration) =>
        options.ConnectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName) ?? string.Empty)
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        $"A connection string '{DatabaseOptions.ConnectionStringName}' não foi configurada (defina ConnectionStrings__{DatabaseOptions.ConnectionStringName}).")
    .ValidateOnStart();

builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<DatabaseMigrator>();
builder.Services.AddHealthFeature();
builder.Services.AddMenuFeature();
builder.Services.AddOrdersFeature();

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.ApplyMigrationsOnStartup)
{
    app.Services.GetRequiredService<DatabaseMigrator>().Migrate();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "ForgeFeast API v1"));
}

var api = app.MapGroup("/api");
api.MapHealthEndpoints();
api.MapMenuEndpoints();
api.MapOrderEndpoints();

app.Run();

// Exposto para o WebApplicationFactory dos testes de integração.
public partial class Program
{
}
