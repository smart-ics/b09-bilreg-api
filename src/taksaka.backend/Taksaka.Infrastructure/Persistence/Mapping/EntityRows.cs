using Taksaka.Core.Entities;
using Taksaka.Core.Enums;

namespace Taksaka.Infrastructure.Persistence.Mapping;

internal static class RowParsing
{
    public static DateTimeOffset ParseDateTimeOffset(string value) =>
        DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

    public static DateTimeOffset? ParseNullableDateTimeOffset(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseDateTimeOffset(value);
}

internal sealed class JobRow
{
    public string Id { get; init; } = string.Empty;

    public string Payload { get; init; } = string.Empty;

    public JobPriority Priority { get; init; }

    public JobStatus Status { get; init; }

    public string? WorkerName { get; init; }

    public int RetryCount { get; init; }

    public string CreatedAt { get; init; } = string.Empty;

    public string? EnqueuedAt { get; init; }

    public string? NextRetryAt { get; init; }

    public string? DeadLetterReason { get; init; }

    public string? SourceJobId { get; init; }

    public string? LockOwner { get; init; }

    public string? LockedUntil { get; init; }

    public Job ToEntity() => new()
    {
        Id = Guid.Parse(Id),
        Payload = Payload,
        Priority = Priority,
        Status = Status,
        WorkerName = WorkerName,
        RetryCount = RetryCount,
        CreatedAt = RowParsing.ParseDateTimeOffset(CreatedAt),
        EnqueuedAt = RowParsing.ParseNullableDateTimeOffset(EnqueuedAt),
        NextRetryAt = RowParsing.ParseNullableDateTimeOffset(NextRetryAt),
        DeadLetterReason = DeadLetterReason,
        SourceJobId = string.IsNullOrWhiteSpace(SourceJobId) ? null : Guid.Parse(SourceJobId),
        LockOwner = LockOwner,
        LockedUntil = RowParsing.ParseNullableDateTimeOffset(LockedUntil)
    };
}

internal sealed class ScheduleRow
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string CronExpression { get; init; } = string.Empty;

    public string WorkerName { get; init; } = string.Empty;

    public string PayloadTemplate { get; init; } = string.Empty;

    public long IsEnabled { get; init; }

    public string? LastRunAt { get; init; }

    public string NextRunAt { get; init; } = string.Empty;

    public Schedule ToEntity() => new()
    {
        Id = Guid.Parse(Id),
        Name = Name,
        CronExpression = CronExpression,
        WorkerName = WorkerName,
        PayloadTemplate = PayloadTemplate,
        IsEnabled = IsEnabled != 0,
        LastRunAt = RowParsing.ParseNullableDateTimeOffset(LastRunAt),
        NextRunAt = RowParsing.ParseDateTimeOffset(NextRunAt)
    };
}

internal sealed class ExecutionHistoryRow
{
    public string Id { get; init; } = string.Empty;

    public string JobId { get; init; } = string.Empty;

    public string StartedAt { get; init; } = string.Empty;

    public string? CompletedAt { get; init; }

    public string Outcome { get; init; } = string.Empty;

    public ExecutionHistory ToEntity() => new()
    {
        Id = Guid.Parse(Id),
        JobId = Guid.Parse(JobId),
        StartedAt = RowParsing.ParseDateTimeOffset(StartedAt),
        CompletedAt = RowParsing.ParseNullableDateTimeOffset(CompletedAt),
        Outcome = Outcome
    };
}

internal sealed class AlertRow
{
    public string Id { get; init; } = string.Empty;

    public AlertSeverity Severity { get; init; }

    public string Message { get; init; } = string.Empty;

    public string RaisedAt { get; init; } = string.Empty;

    public Alert ToEntity() => new()
    {
        Id = Guid.Parse(Id),
        Severity = Severity,
        Message = Message,
        RaisedAt = RowParsing.ParseDateTimeOffset(RaisedAt)
    };
}
