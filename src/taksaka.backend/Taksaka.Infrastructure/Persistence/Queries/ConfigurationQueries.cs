namespace Taksaka.Infrastructure.Persistence.Queries;

internal static class ConfigurationQueries
{
    public const string SelectByKey = """
        SELECT [Key], Value, UpdatedAt
        FROM TAKS_Configuration
        WHERE [Key] = @Key
        """;

    public const string UpsertSqlite = """
        INSERT INTO TAKS_Configuration ([Key], Value, UpdatedAt)
        VALUES (@Key, @Value, @UpdatedAt)
        ON CONFLICT([Key]) DO UPDATE SET
            Value = excluded.Value,
            UpdatedAt = excluded.UpdatedAt
        """;

    public const string UpsertSqlServer = """
        MERGE TAKS_Configuration AS target
        USING (SELECT @Key AS [Key], @Value AS Value, @UpdatedAt AS UpdatedAt) AS source
        ON target.[Key] = source.[Key]
        WHEN MATCHED THEN
            UPDATE SET Value = source.Value, UpdatedAt = source.UpdatedAt
        WHEN NOT MATCHED THEN
            INSERT ([Key], Value, UpdatedAt)
            VALUES (source.[Key], source.Value, source.UpdatedAt);
        """;
}
