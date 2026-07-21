using System.Data;
using Dapper;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;
using Taksaka.Infrastructure.Persistence.Mapping;
using Taksaka.Infrastructure.Persistence.Queries;

namespace Taksaka.Infrastructure.Persistence.Repositories;

public sealed class JobRepository(IDbConnectionFactory connectionFactory) : IJobRepository
{
    public async Task InsertAsync(Job job, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var connection = transaction?.Connection ?? connectionFactory.CreateConnection();
        var ownsConnection = transaction is null;

        try
        {
            if (ownsConnection)
            {
                connection.Open();
            }

            await connection.ExecuteAsync(new CommandDefinition(
                JobQueries.Insert,
                JobParameters.From(job),
                transaction,
                cancellationToken: cancellationToken));
        }
        finally
        {
            if (ownsConnection)
            {
                connection.Dispose();
            }
        }
    }

    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        var row = await connection.QuerySingleOrDefaultAsync<JobRow>(new CommandDefinition(
            JobQueries.SelectById,
            new { Id = id },
            cancellationToken: cancellationToken));

        return row?.ToEntity();
    }

    public async Task UpdateAsync(Job job, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var connection = transaction?.Connection ?? connectionFactory.CreateConnection();
        var ownsConnection = transaction is null;

        try
        {
            if (ownsConnection)
            {
                connection.Open();
            }

            await connection.ExecuteAsync(new CommandDefinition(
                JobQueries.Update,
                JobParameters.From(job),
                transaction,
                cancellationToken: cancellationToken));
        }
        finally
        {
            if (ownsConnection)
            {
                connection.Dispose();
            }
        }
    }

    public async Task<Job> CreateReplayAsync(Guid sourceJobId, CancellationToken cancellationToken = default)
    {
        var source = await GetByIdAsync(sourceJobId, cancellationToken)
            ?? throw new InvalidOperationException($"Job {sourceJobId} was not found.");

        var replay = new Job
        {
            Payload = source.Payload,
            Priority = source.Priority,
            WorkerName = source.WorkerName,
            SourceJobId = sourceJobId,
            Status = JobStatus.Created
        };

        await InsertAsync(replay, cancellationToken: cancellationToken);
        return replay;
    }

    public async Task<IReadOnlyList<Job>> GetRetryReadyAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        var rows = await connection.QueryAsync<JobRow>(new CommandDefinition(
            JobQueries.SelectRetryReady,
            new { RetryWaiting = JobStatus.RetryWaiting, AsOf = asOf },
            cancellationToken: cancellationToken));

        return rows.Select(row => row.ToEntity()).ToList();
    }
}

internal static class JobParameters
{
    public static object From(Job job) => new
    {
        job.Id,
        job.Payload,
        Priority = (int)job.Priority,
        Status = (int)job.Status,
        job.WorkerName,
        job.RetryCount,
        job.CreatedAt,
        job.EnqueuedAt,
        job.NextRetryAt,
        job.DeadLetterReason,
        job.SourceJobId,
        job.LockOwner,
        job.LockedUntil
    };
}
