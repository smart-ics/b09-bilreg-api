namespace Taksaka.Infrastructure.Database.Schema;

internal static class SqlServerSchema
{
    public static IReadOnlyList<string> Statements { get; } =
    [
        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TAKS_Job')
        CREATE TABLE TAKS_Job (
            Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            Payload NVARCHAR(MAX) NOT NULL,
            Priority INT NOT NULL,
            Status INT NOT NULL,
            WorkerName NVARCHAR(200) NULL,
            RetryCount INT NOT NULL CONSTRAINT DF_TAKS_Job_RetryCount DEFAULT 0,
            CreatedAt DATETIMEOFFSET NOT NULL,
            EnqueuedAt DATETIMEOFFSET NULL,
            NextRetryAt DATETIMEOFFSET NULL,
            DeadLetterReason NVARCHAR(MAX) NULL,
            SourceJobId UNIQUEIDENTIFIER NULL,
            LockOwner NVARCHAR(200) NULL,
            LockedUntil DATETIMEOFFSET NULL
        )
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TAKS_Job_Dequeue' AND object_id = OBJECT_ID('TAKS_Job'))
        CREATE INDEX IX_TAKS_Job_Dequeue ON TAKS_Job (Status, Priority DESC, EnqueuedAt)
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TAKS_ExecutionHistory')
        CREATE TABLE TAKS_ExecutionHistory (
            Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            JobId UNIQUEIDENTIFIER NOT NULL,
            StartedAt DATETIMEOFFSET NOT NULL,
            CompletedAt DATETIMEOFFSET NULL,
            Outcome NVARCHAR(500) NOT NULL
        )
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TAKS_ExecutionHistory_JobId' AND object_id = OBJECT_ID('TAKS_ExecutionHistory'))
        CREATE INDEX IX_TAKS_ExecutionHistory_JobId ON TAKS_ExecutionHistory (JobId)
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TAKS_Schedule')
        CREATE TABLE TAKS_Schedule (
            Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            Name NVARCHAR(200) NOT NULL,
            CronExpression NVARCHAR(100) NOT NULL,
            WorkerName NVARCHAR(200) NOT NULL,
            PayloadTemplate NVARCHAR(MAX) NOT NULL,
            IsEnabled BIT NOT NULL CONSTRAINT DF_TAKS_Schedule_IsEnabled DEFAULT 1,
            LastRunAt DATETIMEOFFSET NULL,
            NextRunAt DATETIMEOFFSET NOT NULL
        )
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TAKS_Alert')
        CREATE TABLE TAKS_Alert (
            Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            Severity INT NOT NULL,
            Message NVARCHAR(MAX) NOT NULL,
            RaisedAt DATETIMEOFFSET NOT NULL
        )
        """,
        """
        IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TAKS_Configuration')
        CREATE TABLE TAKS_Configuration (
            [Key] NVARCHAR(200) NOT NULL PRIMARY KEY,
            Value NVARCHAR(MAX) NOT NULL,
            UpdatedAt DATETIMEOFFSET NOT NULL
        )
        """
    ];
}
