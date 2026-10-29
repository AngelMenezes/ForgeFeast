using ForgeFeast.Api.Features.Menu;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ForgeFeast.Api.Features.Orders;

public static class OrderEndpoints
{
    public static IServiceCollection AddOrdersFeature(this IServiceCollection services)
    {
        services
            .AddOptions<OrderingOptions>()
            .BindConfiguration(OrderingOptions.SectionName)
            .Validate(
                options => OrderingOptions.TryParseCutoffTime(options.CutoffTime, out _),
                "Ordering:CutoffTime deve estar no formato HH:mm (defina Ordering__CutoffTime).")
            .Validate(
                options => OrderingOptions.TryFindTimeZone(options.TimeZone, out _),
                "Ordering:TimeZone não foi encontrado no sistema (defina Ordering__TimeZone).")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<OrderCutoffPolicy>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IDishOrderLookup, DishOrderLookup>();
        return services;
    }

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", CreateOrderAsync)
            .WithName("createOrder")
            .WithTags("Orders")
            .WithSummary("Cria o pedido de uma pessoa num dia útil.");

        app.MapGet("/orders", GetOrderAsync)
            .WithName("getOrder")
            .WithTags("Orders")
            .WithSummary("Consulta o pedido de uma pessoa numa data.");

        app.MapPut("/orders", UpdateOrderAsync)
            .WithName("updateOrder")
            .WithTags("Orders")
            .WithSummary("Substitui os itens do pedido existente.");

        app.MapGet("/orders/window", GetWindow)
            .WithName("getOrderWindow")
            .WithTags("Orders")
            .WithSummary("Horário de corte de um dia útil e se ele ainda aceita pedidos.");

        return app;
    }

    private static async Task<IResult> CreateOrderAsync(
        OrderRequest? request,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
        request ??= new OrderRequest(null, null, null);
        var validation = OrderValidator.ValidateOrder(request.Name, request.Date, request.Items);
        if (!validation.IsValid || validation.Value is null)
        {
            return ValidationProblem(validation.Errors);
        }

        var result = await orderService.CreateAsync(validation.Value, cancellationToken);
        if (result.Outcome == OrderOutcome.Success && result.Order is not null)
        {
            var location = $"/api/orders?name={Uri.EscapeDataString(result.Order.Name)}&date={result.Order.Date:yyyy-MM-dd}";
            return TypedResults.Created(location, result.Order);
        }

        return ToProblem(result);
    }

    private static async Task<IResult> GetOrderAsync(
        string? name,
        string? date,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
        var validation = OrderValidator.ValidateKey(name, date, allowWeekend: true);
        if (!validation.IsValid || validation.Value is null)
        {
            return ValidationProblem(validation.Errors);
        }

        var result = await orderService.GetAsync(validation.Value, cancellationToken);
        return result.Outcome == OrderOutcome.Success ? TypedResults.Ok(result.Order) : ToProblem(result);
    }

    private static async Task<IResult> UpdateOrderAsync(
        string? name,
        string? date,
        OrderItemsRequest? request,
        IOrderService orderService,
        CancellationToken cancellationToken)
    {
        var validation = OrderValidator.ValidateOrder(name, date, request?.Items);
        if (!validation.IsValid || validation.Value is null)
        {
            return ValidationProblem(validation.Errors);
        }

        var result = await orderService.ReplaceItemsAsync(validation.Value, cancellationToken);
        return result.Outcome == OrderOutcome.Success ? TypedResults.Ok(result.Order) : ToProblem(result);
    }

    private static IResult GetWindow(string? date, IOrderService orderService)
    {
        var validation = OrderValidator.ValidateWindowDate(date);
        if (!validation.IsValid || validation.Value is null)
        {
            return ValidationProblem(validation.Errors);
        }

        return TypedResults.Ok(orderService.GetWindow(validation.Value.Date));
    }

    private static IResult ToProblem(OrderResult result)
    {
        return result.Outcome switch
        {
            OrderOutcome.InvalidItems => ValidationProblem(
                new Dictionary<string, string[]> { ["items"] = [.. result.ItemErrors ?? []] }),
            OrderOutcome.Closed => TypedResults.Problem(
                title: "Pedido encerrado.",
                detail: result.Detail,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.21"),
            OrderOutcome.NotFound => TypedResults.Problem(
                title: "Pedido não encontrado.",
                detail: result.Detail,
                statusCode: StatusCodes.Status404NotFound,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            OrderOutcome.AlreadyExists => TypedResults.Problem(
                title: "Pedido já existe.",
                detail: result.Detail,
                statusCode: StatusCodes.Status409Conflict,
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            _ => throw new InvalidOperationException($"Resultado inesperado: {result.Outcome}."),
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
