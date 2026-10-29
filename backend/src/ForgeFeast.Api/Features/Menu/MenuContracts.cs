using System.Text.Json.Serialization;

namespace ForgeFeast.Api.Features.Menu;

/// <summary>Corpo de POST e PUT. A data chega como texto para que um formato inválido caia no campo (ADR-013).</summary>
public sealed record DishRequest(string? Date, string? Name, string? Description);

public sealed record DishResponse(int Id, DateOnly Date, string Name, string? Description);

public sealed record MenuDayResponse(
    DateOnly Date,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] DayOfWeek DayOfWeek,
    IReadOnlyList<DishResponse> Dishes);

public sealed record WeekMenuResponse(DateOnly WeekStart, DateOnly WeekEnd, IReadOnlyList<MenuDayResponse> Days);

public enum DeleteDishResult
{
    Deleted,
    NotFound,
    HasOrders,
}

public enum UpdateDishStatus
{
    Updated,
    NotFound,
    HasOrders,
}

/// <summary>Resultado da edição de prato (ADR-018); <see cref="Dish"/> só vem preenchido em <see cref="UpdateDishStatus.Updated"/>.</summary>
public sealed record UpdateDishResult(UpdateDishStatus Status, DishResponse? Dish = null);

/// <summary>Prato já validado e normalizado (trim aplicado, descrição vazia como null).</summary>
public sealed record DishInput(DateOnly Date, string Name, string? Description);
