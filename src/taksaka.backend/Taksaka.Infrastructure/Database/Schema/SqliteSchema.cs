namespace Taksaka.Infrastructure.Database.Schema;

internal static class SqliteSchema
{
    public static IReadOnlyList<string> Statements { get; } =
    [
        """
        CREATE TABLE IF NOT EXISTS TAKS_Job (
            Id TEXT NOT NULL PRIMARY KEY,
            Payload TEXT NOT NULL,
            Priority INTEGER NOT NULL,
            Status INTEGER NOT NULL,
            WorkerName TEXT NULL,
            RetryCount INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            EnqueuedAt TEXT NULL,
            NextRetryAt TEXT NULL,
            DeadLetterReason TEXT NULL,
            SourceJobId TEXT NULL,
            LockOwner TEXT NULL,
            LockedUntil TEXT NULL
        )
        """,
        """
        CREATE INDEX IF NOT EXISTS IX_TAKS_Job_Dequeue
        ON TAKS_Job (Status, Priority DESC, EnqueuedAt)
        """,
        """
        CREATE TABLE IF NOT EXISTS TAKS_ExecutionHistory (
            Id TEXT NOT NULL PRIMARY KEY,
            JobId TEXT NOT NULL,
            StartedAt TEXT NOT NULL,
            CompletedAt TEXT NULL,
            Outcome TEXT NOT NULL
        )
        """,
        """
        CREATE INDEX IF NOT EXISTS IX_TAKS_ExecutionHistory_JobId
        ON TAKS_ExecutionHistory (JobId)
        """,
        """
        CREATE TABLE IF NOT EXISTS TAKS_Schedule (
            Id TEXT NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL,
            CronExpression TEXT NOT NULL,
            WorkerName TEXT NOT NULL,
            PayloadTemplate TEXT NOT NULL,
            IsEnabled INTEGER NOT NULL DEFAULT 1,
            LastRunAt TEXT NULL,
            NextRunAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS TAKS_Alert (
            Id TEXT NOT NULL PRIMARY KEY,
            Severity INTEGER NOT NULL,
            Message TEXT NOT NULL,
            RaisedAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE IF NOT EXISTS TAKS_Configuration (
            [Key] TEXT NOT NULL PRIMARY KEY,
            Value TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        )
        """
    ];
}
