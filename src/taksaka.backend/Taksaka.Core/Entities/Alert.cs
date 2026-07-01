using Taksaka.Core.Enums;

namespace Taksaka.Core.Entities;

public sealed class Alert
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public AlertSeverity Severity { get; init; } = AlertSeverity.Information;

    public string Message { get; init; } = string.Empty;

    public DateTimeOffset RaisedAt { get; init; } = DateTimeOffset.UtcNow;
}
