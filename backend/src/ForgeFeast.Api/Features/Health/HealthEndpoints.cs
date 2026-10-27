namespace ForgeFeast.Api.Features.Health;

public static class HealthEndpoints
{
    public static IServiceCollection AddHealthFeature(this IServiceCollection services)
    {
        services.AddScoped<IHealthRepository, HealthRepository>();
        services.AddScoped<IHealthService, HealthService>();
        return services;
    }

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", GetHealthAsync)
            .WithName("getHealth")
            .WithTags("Health")
            .WithSummary("Verifica a saúde da API e da conexão com o banco.")
            .Produces<HealthReport>(StatusCodes.Status200OK)
            .Produces<HealthReport>(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    private static async Task<IResult> GetHealthAsync(IHealthService healthService, CancellationToken cancellationToken)
    {
        var report = await healthService.CheckAsync(cancellationToken);

        var statusCode = report.Status == HealthStatus.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;

        return Results.Json(report, statusCode: statusCode);
    }
}
