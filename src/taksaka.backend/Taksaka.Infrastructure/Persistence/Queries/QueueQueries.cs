namespace Taksaka.Infrastructure.Persistence.Queries;

internal static class QueueQueries
{
    public const string Enqueue = """
        UPDATE TAKS_Job
        SET Status = @Queued,
            EnqueuedAt = @EnqueuedAt,
            NextRetryAt = NULL,
            LockOwner = NULL,
            LockedUntil = NULL
        WHERE Id = @Id
        """;

    public const string QueueDepth = """
        SELECT COUNT(1)
        FROM TAKS_Job
        WHERE Status IN (@Queued, @RetryWaiting)
          AND (NextRetryAt IS NULL OR NextRetryAt <= @Now)
        """;
}
