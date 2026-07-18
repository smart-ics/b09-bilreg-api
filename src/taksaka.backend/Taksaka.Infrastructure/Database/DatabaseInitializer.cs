using Dapper;
using Microsoft.Extensions.Options;
using Taksaka.Abstractions.Persistence;
using Taksaka.Infrastructure.Configuration;
using Taksaka.Infrastructure.Database.Schema;

namespace Taksaka.Infrastructure.Database;

public sealed class DatabaseInitializer(
    IDbConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IDatabaseInitializer
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var statements = options.Value.Provider == DatabaseProvider.SqlServer
            ? SqlServerSchema.Statements
            : SqliteSchema.Statements;

        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        foreach (var statement in statements)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await connection.ExecuteAsync(new CommandDefinition(statement, cancellationToken: cancellationToken));
        }
    }
}
