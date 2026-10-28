using ForgeFeast.Api.Features.Menu;

namespace ForgeFeast.Api.Features.Orders;

/// <summary>Validação de formato dos pedidos; erros por campo, sem acessar banco nem relógio (ADR-013, ADR-014).</summary>
public static class OrderValidator
{
    public const int NameMaxLength = 60;
    public const int MaxItems = 20;
    public const int MinQuantity = 1;
    public const int MaxQuantity = 10;

    public const string NameRequiredMessage = "Informe o seu nome.";
    public const string NameTooLongMessage = "O nome deve ter no máximo 60 caracteres.";
    public const string ItemsRequiredMessage = "Adicione ao menos um prato ao pedido.";
    public const string TooManyItemsMessage = "O pedido pode ter no máximo 20 pratos diferentes.";
    public const string InvalidDishMessage = "Informe um prato válido em cada item.";

    public static string QuantityMessage(int dishId) => $"A quantidade do prato {dishId} deve ser de 1 a 10.";

    public static string DuplicateDishMessage(int dishId) => $"O prato {dishId} aparece mais de uma vez no pedido.";

    /// <summary>Valida nome e data. O fim de semana só é recusado quando <paramref name="allowWeekend"/> é falso (o GET aceita).</summary>
    public static ValidationResult<OrderKey> ValidateKey(string? name, string? date, bool allowWeekend)
    {
        var errors = new Dictionary<string, string[]>();
        var key = ValidateKey(name, date, allowWeekend, errors);
        return new ValidationResult<OrderKey>(errors.Count == 0 ? key : null, errors);
    }

    public static ValidationResult<OrderInput> ValidateOrder(
        string? name,
        string? date,
        IReadOnlyList<OrderItemRequest?>? items)
    {
        var errors = new Dictionary<string, string[]>();
        var key = ValidateKey(name, date, allowWeekend: false, errors);
        var validItems = ValidateItems(items, errors);

        return new ValidationResult<OrderInput>(
            errors.Count == 0 && key is not null ? new OrderInput(key.Name, key.Date, validItems) : null,
            errors);
    }

    /// <summary>Valida só a data da janela de pedidos: formato e dia útil.</summary>
    public static ValidationResult<OrderDay> ValidateWindowDate(string? date)
    {
        var errors = new Dictionary<string, string[]>();
        var parsed = ValidateDate(date, allowWeekend: false, errors);
        return new ValidationResult<OrderDay>(errors.Count == 0 ? new OrderDay(parsed) : null, errors);
    }

    private static OrderKey? ValidateKey(string? name, string? date, bool allowWeekend, Dictionary<string, string[]> errors)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            errors["name"] = [NameRequiredMessage];
        }
        else if (trimmed.Length > NameMaxLength)
        {
            errors["name"] = [NameTooLongMessage];
        }

        var parsed = ValidateDate(date, allowWeekend, errors);
        return errors.Count == 0 ? new OrderKey(trimmed, parsed) : null;
    }

    private static DateOnly ValidateDate(string? date, bool allowWeekend, Dictionary<string, string[]> errors)
    {
        if (!MenuValidator.TryParseDate(date, out var parsed))
        {
            errors["date"] = [MenuValidator.InvalidDateMessage];
        }
        else if (!allowWeekend && parsed.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            errors["date"] = [MenuValidator.WeekendMessage];
        }

        return parsed;
    }

    private static List<OrderItemInput> ValidateItems(
        IReadOnlyList<OrderItemRequest?>? items,
        Dictionary<string, string[]> errors)
    {
        var valid = new List<OrderItemInput>();
        var messages = new List<string>();

        if (items is null || items.Count == 0)
        {
            errors["items"] = [ItemsRequiredMessage];
            return valid;
        }

        if (items.Count > MaxItems)
        {
            messages.Add(TooManyItemsMessage);
        }

        var seen = new HashSet<int>();
        var invalidDishReported = false;
        foreach (var item in items)
        {
            if (item?.DishId is not { } dishId || dishId < 1)
            {
                if (!invalidDishReported)
                {
                    messages.Add(InvalidDishMessage);
                    invalidDishReported = true;
                }

                continue;
            }

            if (item.Quantity is not { } quantity || quantity < MinQuantity || quantity > MaxQuantity)
            {
                messages.Add(QuantityMessage(dishId));
                continue;
            }

            if (!seen.Add(dishId))
            {
                messages.Add(DuplicateDishMessage(dishId));
                continue;
            }

            valid.Add(new OrderItemInput(dishId, quantity));
        }

        if (messages.Count > 0)
        {
            errors["items"] = [.. messages];
        }

        return valid;
    }
}
