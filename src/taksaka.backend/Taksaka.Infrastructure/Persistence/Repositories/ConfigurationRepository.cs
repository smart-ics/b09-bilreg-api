using Dapper;
using Microsoft.Extensions.Options;
using Taksaka.Abstractions.Persistence;
using Taksaka.Infrastructure.Configuration;
using Taksaka.Infrastructure.Persistence.Queries;

namespace Taksaka.Infrastructure.Persistence.Repositories;

public sealed class ConfigurationRepository(
    IDbConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IConfigurationRepository
{
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        return await connection.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
            "SELECT Value FROM TAKS_Configuration WHERE [Key] = @Key",
            new { Key = key },
            cancellationToken: cancellationToken));
    }

    public async Task SetValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        var sql = options.Value.Provider == DatabaseProvider.SqlServer
            ? ConfigurationQueries.UpsertSqlServer
            : ConfigurationQueries.UpsertSqlite;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Key = key, Value = value, UpdatedAt = DateTimeOffset.UtcNow },
            cancellationToken: cancellationToken));
    }
}
