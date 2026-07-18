using Dapper;
using Microsoft.Extensions.Options;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Infrastructure.Configuration;
using Taksaka.Infrastructure.Persistence.Mapping;
using Taksaka.Infrastructure.Persistence.Queries;

namespace Taksaka.Infrastructure.Persistence.Repositories;

public sealed class AlertRepository(
    IDbConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IAlertRepository
{
    public async Task InsertAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        await connection.ExecuteAsync(new CommandDefinition(
            AlertQueries.Insert,
            new
            {
                alert.Id,
                Severity = (int)alert.Severity,
                alert.Message,
                alert.RaisedAt
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Alert>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        var sql = options.Value.Provider == DatabaseProvider.SqlServer
            ? AlertQueriesSqlServer.SelectRecent
            : AlertQueries.SelectRecent;

        var rows = await connection.QueryAsync<AlertRow>(new CommandDefinition(
            sql,
            new { Limit = limit },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToEntity()).ToList();
    }
}
