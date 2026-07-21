namespace Taksaka.Infrastructure.Persistence.Queries;

internal static class AlertQueries
{
    public const string Insert = """
        INSERT INTO TAKS_Alert (Id, Severity, Message, RaisedAt)
        VALUES (@Id, @Severity, @Message, @RaisedAt)
        """;

    public const string SelectRecent = """
        SELECT Id, Severity, Message, RaisedAt
        FROM TAKS_Alert
        ORDER BY RaisedAt DESC
        LIMIT @Limit
        """;
}
