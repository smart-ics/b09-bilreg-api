namespace Taksaka.Infrastructure.Persistence.Queries;

internal static class ExecutionHistoryQueries
{
    public const string Insert = """
        INSERT INTO TAKS_ExecutionHistory (Id, JobId, StartedAt, CompletedAt, Outcome)
        VALUES (@Id, @JobId, @StartedAt, @CompletedAt, @Outcome)
        """;

    public const string SelectByJobId = """
        SELECT Id, JobId, StartedAt, CompletedAt, Outcome
        FROM TAKS_ExecutionHistory
        WHERE JobId = @JobId
        ORDER BY StartedAt DESC
        """;
}
