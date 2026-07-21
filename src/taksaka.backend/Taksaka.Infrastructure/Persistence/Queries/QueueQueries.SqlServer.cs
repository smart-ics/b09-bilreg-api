namespace Taksaka.Infrastructure.Persistence.Queries;

internal static class QueueQueriesSqlServer
{
    public const string DequeueAndLock = """
        ;WITH NextJob AS (
            SELECT TOP 1 Id
            FROM TAKS_Job WITH (UPDLOCK, READPAST, ROWLOCK)
            WHERE Status IN (@Queued, @RetryWaiting)
              AND (LockedUntil IS NULL OR LockedUntil < @Now)
              AND (NextRetryAt IS NULL OR NextRetryAt <= @Now)
            ORDER BY Priority DESC, EnqueuedAt ASC
        )
        UPDATE j
        SET Status = @Running,
            LockOwner = @LockOwner,
            LockedUntil = @LockedUntil
        OUTPUT INSERTED.Id, INSERTED.Payload, INSERTED.Priority, INSERTED.Status,
               INSERTED.WorkerName, INSERTED.RetryCount, INSERTED.CreatedAt,
               INSERTED.EnqueuedAt, INSERTED.NextRetryAt, INSERTED.DeadLetterReason,
               INSERTED.SourceJobId, INSERTED.LockOwner, INSERTED.LockedUntil
        FROM TAKS_Job j
        INNER JOIN NextJob n ON j.Id = n.Id
        """;
}
