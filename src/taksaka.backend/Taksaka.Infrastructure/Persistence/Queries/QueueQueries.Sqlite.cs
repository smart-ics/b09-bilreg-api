namespace Taksaka.Infrastructure.Persistence.Queries;

internal static class QueueQueriesSqlite
{
    public const string SelectNextCandidate = """
        SELECT Id, Payload, Priority, Status, WorkerName, RetryCount, CreatedAt,
               EnqueuedAt, NextRetryAt, DeadLetterReason, SourceJobId, LockOwner, LockedUntil
        FROM TAKS_Job
        WHERE Status IN (@Queued, @RetryWaiting)
          AND (LockedUntil IS NULL OR LockedUntil < @Now)
          AND (NextRetryAt IS NULL OR NextRetryAt <= @Now)
        ORDER BY Priority DESC, EnqueuedAt ASC
        LIMIT 1
        """;

    public const string ClaimJob = """
        UPDATE TAKS_Job
        SET Status = @Running,
            LockOwner = @LockOwner,
            LockedUntil = @LockedUntil
        WHERE Id = @Id
          AND Status IN (@Queued, @RetryWaiting)
        """;
}
