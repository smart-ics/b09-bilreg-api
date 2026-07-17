using Dapper;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Infrastructure.Persistence.Mapping;
using Taksaka.Infrastructure.Persistence.Queries;

namespace Taksaka.Infrastructure.Persistence.Repositories;

public sealed class ExecutionHistoryRepository(IDbConnectionFactory connectionFactory) : IExecutionHistoryRepository
{
    public async Task InsertAsync(ExecutionHistory entry, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        await connection.ExecuteAsync(new CommandDefinition(
            ExecutionHistoryQueries.Insert,
            new
            {
                entry.Id,
                entry.JobId,
                entry.StartedAt,
                entry.CompletedAt,
                entry.Outcome
            },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ExecutionHistory>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        var rows = await connection.QueryAsync<ExecutionHistoryRow>(new CommandDefinition(
            ExecutionHistoryQueries.SelectByJobId,
            new { JobId = jobId },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToEntity()).ToList();
    }
}
