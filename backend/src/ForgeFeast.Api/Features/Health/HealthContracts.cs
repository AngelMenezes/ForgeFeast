using System.Text.Json.Serialization;

namespace ForgeFeast.Api.Features.Health;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
}

public sealed record HealthCheckEntry(string Name, HealthStatus Status);

public sealed record HealthReport(HealthStatus Status, IReadOnlyList<HealthCheckEntry> Checks);
