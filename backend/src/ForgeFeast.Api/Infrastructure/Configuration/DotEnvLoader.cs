namespace ForgeFeast.Api.Infrastructure.Configuration;

/// <summary>
/// Lê um arquivo .env e define as variáveis de ambiente que ainda não existem no processo (ADR-009).
/// </summary>
public static class DotEnvLoader
{
    private const string FileName = ".env";

    public static string? FindNearestFile(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, FileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    public static int Load(string filePath)
    {
        var applied = 0;

        foreach (var (key, value) in Parse(File.ReadLines(filePath)))
        {
            if (Environment.GetEnvironmentVariable(key) is not null)
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, value);
            applied++;
        }

        return applied;
    }

    public static IEnumerable<KeyValuePair<string, string>> Parse(IEnumerable<string> lines)
    {
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = Unquote(line[(separator + 1)..].Trim());
            yield return new KeyValuePair<string, string>(key, value);
        }
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && (value[0] == '"' || value[0] == '\'')
            && value[^1] == value[0])
        {
            return value[1..^1];
        }

        return value;
    }
}
