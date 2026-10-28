using System.Globalization;

namespace ForgeFeast.Api.Features.Orders;

/// <summary>Seção <c>Ordering</c>: corte e fuso da regra de pedidos (ADR-015).</summary>
public sealed class OrderingOptions
{
    public const string SectionName = "Ordering";
    public const string CutoffTimeFormat = "HH:mm";

    public string CutoffTime { get; set; } = "12:00";

    public string TimeZone { get; set; } = "America/Sao_Paulo";

    public static bool TryParseCutoffTime(string? value, out TimeOnly cutoffTime)
    {
        return TimeOnly.TryParseExact(value, CutoffTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out cutoffTime);
    }

    public static bool TryFindTimeZone(string? id, out TimeZoneInfo timeZone)
    {
        timeZone = TimeZoneInfo.Utc;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return TryFind(id, out timeZone)
            || (_windowsEquivalents.TryGetValue(id, out var windowsId) && TryFind(windowsId, out timeZone));
    }

    // Com InvariantGlobalization (Directory.Build.props) não há ICU, e no Windows o id IANA deixa de ser
    // resolvido. Estes ids cobrem o padrão do projeto; outros fusos no Windows usam o id do próprio Windows.
    private static readonly Dictionary<string, string> _windowsEquivalents = new(StringComparer.Ordinal)
    {
        ["America/Sao_Paulo"] = "E. South America Standard Time",
        ["UTC"] = "UTC",
    };

    private static bool TryFind(string id, out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = TimeZoneInfo.Utc;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            timeZone = TimeZoneInfo.Utc;
            return false;
        }
    }
}
