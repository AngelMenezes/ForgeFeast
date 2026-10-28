using System.Globalization;

namespace ForgeFeast.Api.Features.Menu;

/// <summary>Resultado de uma validação: o valor quando válido, ou os erros por campo (ADR-013).</summary>
public sealed record ValidationResult<T>(T? Value, IReadOnlyDictionary<string, string[]> Errors)
    where T : class
{
    public bool IsValid => Errors.Count == 0;
}

public static class MenuValidator
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 300;

    public const string InvalidDateMessage = "Informe uma data válida no formato AAAA-MM-DD.";
    public const string WeekStartNotMondayMessage = "A semana deve começar numa segunda-feira.";
    public const string WeekendMessage = "Escolha um dia útil (segunda a sexta).";
    public const string NameRequiredMessage = "Informe o nome do prato.";
    public const string NameTooLongMessage = "O nome do prato deve ter no máximo 100 caracteres.";
    public const string DescriptionTooLongMessage = "A descrição deve ter no máximo 300 caracteres.";

    private const string DateFormat = "yyyy-MM-dd";

    public static ValidationResult<WeekStartValue> ValidateWeekStart(string? weekStart)
    {
        var errors = new Dictionary<string, string[]>();

        if (!TryParseDate(weekStart, out var date))
        {
            errors["weekStart"] = [InvalidDateMessage];
        }
        else if (date.DayOfWeek != DayOfWeek.Monday)
        {
            errors["weekStart"] = [WeekStartNotMondayMessage];
        }

        return new ValidationResult<WeekStartValue>(errors.Count == 0 ? new WeekStartValue(date) : null, errors);
    }

    public static ValidationResult<DishInput> ValidateDish(DishRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (!TryParseDate(request.Date, out var date))
        {
            errors["date"] = [InvalidDateMessage];
        }
        else if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            errors["date"] = [WeekendMessage];
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            errors["name"] = [NameRequiredMessage];
        }
        else if (name.Length > NameMaxLength)
        {
            errors["name"] = [NameTooLongMessage];
        }

        var description = request.Description?.Trim();
        if (string.IsNullOrEmpty(description))
        {
            description = null;
        }
        else if (description.Length > DescriptionMaxLength)
        {
            errors["description"] = [DescriptionTooLongMessage];
        }

        return new ValidationResult<DishInput>(
            errors.Count == 0 ? new DishInput(date, name, description) : null,
            errors);
    }

    private static bool TryParseDate(string? value, out DateOnly date)
    {
        return DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}

public sealed record WeekStartValue(DateOnly Date);
