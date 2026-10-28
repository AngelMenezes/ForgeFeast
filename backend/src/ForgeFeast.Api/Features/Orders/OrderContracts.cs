namespace ForgeFeast.Api.Features.Orders;

/// <summary>Item do corpo. Campos nuláveis para que ausência ou valor fora do intervalo caia em <c>items</c> (ADR-014).</summary>
public sealed record OrderItemRequest(int? DishId, int? Quantity);

/// <summary>Corpo do POST. A data chega como texto para que um formato inválido caia no campo (ADR-013).</summary>
public sealed record OrderRequest(string? Name, string? Date, IReadOnlyList<OrderItemRequest?>? Items);

/// <summary>Corpo do PUT; nome e data vêm da query.</summary>
public sealed record OrderItemsRequest(IReadOnlyList<OrderItemRequest?>? Items);

public sealed record OrderItemResponse(int DishId, string DishName, int Quantity);

public sealed record OrderResponse(
    string Name,
    DateOnly Date,
    IReadOnlyList<OrderItemResponse> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record OrderWindowResponse(
    DateOnly Date,
    string CutoffTime,
    string TimeZone,
    DateTimeOffset ClosesAt,
    bool IsOpen);

/// <summary>Nome (já com trim) e data de um pedido.</summary>
public sealed record OrderKey(string Name, DateOnly Date);

public sealed record OrderDay(DateOnly Date);

public sealed record OrderItemInput(int DishId, int Quantity);

/// <summary>Pedido já validado e normalizado.</summary>
public sealed record OrderInput(string Name, DateOnly Date, IReadOnlyList<OrderItemInput> Items);

/// <summary>Pedido como gravado. Os horários são UTC.</summary>
public sealed record OrderRecord(
    string Name,
    DateOnly Date,
    IReadOnlyList<OrderItemInput> Items,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
