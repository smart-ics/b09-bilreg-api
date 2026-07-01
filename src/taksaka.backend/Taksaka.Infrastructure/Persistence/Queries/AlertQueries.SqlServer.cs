namespace Taksaka.Infrastructure.Persistence.Queries;

internal static class AlertQueriesSqlServer
{
    public const string SelectRecent = """
        SELECT TOP (@Limit) Id, Severity, Message, RaisedAt
        FROM TAKS_Alert
        ORDER BY RaisedAt DESC
        """;
}
