namespace Taksaka.Infrastructure.Persistence.Queries;

internal static class JobQueries
{
    public const string Insert = """
        INSERT INTO TAKS_Job (
            Id, Payload, Priority, Status, WorkerName, RetryCount, CreatedAt,
            EnqueuedAt, NextRetryAt, DeadLetterReason, SourceJobId, LockOwner, LockedUntil)
        VALUES (
            @Id, @Payload, @Priority, @Status, @WorkerName, @RetryCount, @CreatedAt,
            @EnqueuedAt, @NextRetryAt, @DeadLetterReason, @SourceJobId, @LockOwner, @LockedUntil)
        """;

    public const string SelectById = """
        SELECT Id, Payload, Priority, Status, WorkerName, RetryCount, CreatedAt,
               EnqueuedAt, NextRetryAt, DeadLetterReason, SourceJobId, LockOwner, LockedUntil
        FROM TAKS_Job
        WHERE Id = @Id
        """;

    public const string Update = """
        UPDATE TAKS_Job
        SET Payload = @Payload,
            Priority = @Priority,
            Status = @Status,
            WorkerName = @WorkerName,
            RetryCount = @RetryCount,
            EnqueuedAt = @EnqueuedAt,
            NextRetryAt = @NextRetryAt,
            DeadLetterReason = @DeadLetterReason,
            LockOwner = @LockOwner,
            LockedUntil = @LockedUntil
        WHERE Id = @Id
        """;

    public const string SelectRetryReady = """
        SELECT Id, Payload, Priority, Status, WorkerName, RetryCount, CreatedAt,
               EnqueuedAt, NextRetryAt, DeadLetterReason, SourceJobId, LockOwner, LockedUntil
        FROM TAKS_Job
        WHERE Status = @RetryWaiting
          AND NextRetryAt IS NOT NULL
          AND NextRetryAt <= @AsOf
        """;
}
