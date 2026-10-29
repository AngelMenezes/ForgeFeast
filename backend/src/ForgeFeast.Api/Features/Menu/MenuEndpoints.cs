using Microsoft.AspNetCore.Http.HttpResults;

namespace ForgeFeast.Api.Features.Menu;

public static class MenuEndpoints
{
    public static IServiceCollection AddMenuFeature(this IServiceCollection services)
    {
        services.AddScoped<IDishRepository, DishRepository>();
        services.AddScoped<IMenuService, MenuService>();
        return services;
    }

    public static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/menu", GetWeekMenuAsync)
            .WithName("getWeekMenu")
            .WithTags("Menu")
            .WithSummary("Cardápio da semana, de segunda a sexta.");

        app.MapPost("/dishes", CreateDishAsync)
            .WithName("createDish")
            .WithTags("Menu")
            .WithSummary("Cadastra um prato em um dia útil.");

        app.MapGet("/dishes/{id:int}", GetDishAsync)
            .WithName("getDish")
            .WithTags("Menu")
            .WithSummary("Consulta um prato pelo id.");

        app.MapPut("/dishes/{id:int}", UpdateDishAsync)
            .WithName("updateDish")
            .WithTags("Menu")
            .WithSummary("Substitui um prato (inclusive a data).");

        app.MapDelete("/dishes/{id:int}", DeleteDishAsync)
            .WithName("deleteDish")
            .WithTags("Menu")
            .WithSummary("Remove um prato.");

        return app;
    }

    private static async Task<IResult> GetWeekMenuAsync(
        string? weekStart,
        IMenuService menuService,
        CancellationToken cancellationToken)
    {
        var validation = MenuValidator.ValidateWeekStart(weekStart);
        if (!validation.IsValid || validation.Value is null)
        {
            return ValidationProblem(validation.Errors);
        }

        var menu = await menuService.GetWeekAsync(validation.Value.Date, cancellationToken);
        return TypedResults.Ok(menu);
    }

    private static async Task<IResult> CreateDishAsync(
        DishRequest? request,
        IMenuService menuService,
        CancellationToken cancellationToken)
    {
        var validation = MenuValidator.ValidateDish(request ?? new DishRequest(null, null, null));
        if (!validation.IsValid || validation.Value is null)
        {
            return ValidationProblem(validation.Errors);
        }

        var dish = await menuService.CreateDishAsync(validation.Value, cancellationToken);
        return TypedResults.Created($"/api/dishes/{dish.Id}", dish);
    }

    private static async Task<IResult> GetDishAsync(
        int id,
        IMenuService menuService,
        CancellationToken cancellationToken)
    {
        var dish = await menuService.GetDishAsync(id, cancellationToken);
        return dish is null ? TypedResults.NotFound() : TypedResults.Ok(dish);
    }

    private static async Task<IResult> UpdateDishAsync(
        int id,
        DishRequest? request,
        IMenuService menuService,
        CancellationToken cancellationToken)
    {
        // ADR-013: a validação (400) vem antes da existência (404).
        var validation = MenuValidator.ValidateDish(request ?? new DishRequest(null, null, null));
        if (!validation.IsValid || validation.Value is null)
        {
            return ValidationProblem(validation.Errors);
        }

        var result = await menuService.UpdateDishAsync(id, validation.Value, cancellationToken);
        return result.Status switch
        {
            UpdateDishStatus.Updated when result.Dish is not null => TypedResults.Ok(result.Dish),
            UpdateDishStatus.HasOrders => TypedResults.Problem(
                title: "Prato com pedidos.",
                detail: "Este prato tem pedidos e não pode mudar de data.",
                statusCode: StatusCodes.Status409Conflict,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            _ => TypedResults.NotFound(),
        };
    }

    private static async Task<IResult> DeleteDishAsync(
        int id,
        IMenuService menuService,
        CancellationToken cancellationToken)
    {
        var result = await menuService.DeleteDishAsync(id, cancellationToken);
        return result switch
        {
            DeleteDishResult.Deleted => TypedResults.NoContent(),
            DeleteDishResult.HasOrders => TypedResults.Problem(
                title: "Prato com pedidos.",
                detail: "Este prato tem pedidos e não pode ser removido.",
                statusCode: StatusCodes.Status409Conflict,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            _ => TypedResults.NotFound(),
        };
    }

    private static ValidationProblem ValidationProblem(IReadOnlyDictionary<string, string[]> errors)
    {
        return TypedResults.ValidationProblem(
            errors.ToDictionary(pair => pair.Key, pair => pair.Value),
            title: "Um ou mais campos são inválidos.",
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");
    }
}
