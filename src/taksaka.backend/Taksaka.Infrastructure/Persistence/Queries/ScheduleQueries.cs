namespace Taksaka.Infrastructure.Persistence.Queries;

internal static class ScheduleQueries
{
    public const string Insert = """
        INSERT INTO TAKS_Schedule (
            Id, Name, CronExpression, WorkerName, PayloadTemplate, IsEnabled, LastRunAt, NextRunAt)
        VALUES (
            @Id, @Name, @CronExpression, @WorkerName, @PayloadTemplate, @IsEnabled, @LastRunAt, @NextRunAt)
        """;

    public const string SelectEnabled = """
        SELECT Id, Name, CronExpression, WorkerName, PayloadTemplate, IsEnabled, LastRunAt, NextRunAt
        FROM TAKS_Schedule
        WHERE IsEnabled = @IsEnabled
        """;

    public const string UpdateRunTimes = """
        UPDATE TAKS_Schedule
        SET LastRunAt = @LastRunAt,
            NextRunAt = @NextRunAt
        WHERE Id = @Id
        """;
}
