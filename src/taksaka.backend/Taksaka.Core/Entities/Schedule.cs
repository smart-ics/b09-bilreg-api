namespace Taksaka.Core.Entities;

public sealed class Schedule
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; init; } = string.Empty;

    public string CronExpression { get; init; } = string.Empty;

    public string WorkerName { get; init; } = string.Empty;

    public string PayloadTemplate { get; init; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset? LastRunAt { get; set; }

    public DateTimeOffset NextRunAt { get; set; }
}
