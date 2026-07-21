using System.Data;
using Dapper;
using Microsoft.Extensions.Options;
using Taksaka.Abstractions.Persistence;
using Taksaka.Core.Entities;
using Taksaka.Core.Enums;
using Taksaka.Infrastructure.Configuration;
using Taksaka.Infrastructure.Persistence.Mapping;
using Taksaka.Infrastructure.Persistence.Queries;

namespace Taksaka.Infrastructure.Persistence.Repositories;

public sealed class QueueRepository(
    IDbConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IQueueRepository
{
    public async Task EnqueueAsync(Job job, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var connection = transaction?.Connection ?? connectionFactory.CreateConnection();
        var ownsConnection = transaction is null;

        try
        {
            if (ownsConnection)
            {
                connection.Open();
            }

            job.Status = JobStatus.Queued;
            job.EnqueuedAt = DateTimeOffset.UtcNow;
            job.NextRetryAt = null;
            job.LockOwner = null;
            job.LockedUntil = null;

            await connection.ExecuteAsync(new CommandDefinition(
                QueueQueries.Enqueue,
                new
                {
                    Queued = (int)JobStatus.Queued,
                    job.EnqueuedAt,
                    job.Id
                },
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

    public async Task<Job?> TryDequeueAsync(string lockOwner, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        return options.Value.Provider == DatabaseProvider.SqlServer
            ? await TryDequeueSqlServerAsync(lockOwner, lockDuration, cancellationToken)
            : await TryDequeueSqliteAsync(lockOwner, lockDuration, cancellationToken);
    }

    public async Task<int> GetQueueDepthAsync(CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            QueueQueries.QueueDepth,
            new
            {
                Queued = (int)JobStatus.Queued,
                RetryWaiting = (int)JobStatus.RetryWaiting,
                Now = DateTimeOffset.UtcNow
            },
            cancellationToken: cancellationToken));
    }

    private async Task<Job?> TryDequeueSqlServerAsync(string lockOwner, TimeSpan lockDuration, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        var now = DateTimeOffset.UtcNow;
        var row = await connection.QuerySingleOrDefaultAsync<JobRow>(new CommandDefinition(
            QueueQueriesSqlServer.DequeueAndLock,
            new
            {
                Queued = (int)JobStatus.Queued,
                RetryWaiting = (int)JobStatus.RetryWaiting,
                Running = (int)JobStatus.Running,
                Now = now,
                LockOwner = lockOwner,
                LockedUntil = now.Add(lockDuration)
            },
            cancellationToken: cancellationToken));

        return row?.ToEntity();
    }

    private async Task<Job?> TryDequeueSqliteAsync(string lockOwner, TimeSpan lockDuration, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var now = DateTimeOffset.UtcNow;

        var candidate = await connection.QuerySingleOrDefaultAsync<JobRow>(new CommandDefinition(
            QueueQueriesSqlite.SelectNextCandidate,
            new
            {
                Queued = (int)JobStatus.Queued,
                RetryWaiting = (int)JobStatus.RetryWaiting,
                Now = now
            },
            transaction,
            cancellationToken: cancellationToken));

        if (candidate is null)
        {
            transaction.Commit();
            return null;
        }

        var claimed = await connection.ExecuteAsync(new CommandDefinition(
            QueueQueriesSqlite.ClaimJob,
            new
            {
                candidate.Id,
                Queued = (int)JobStatus.Queued,
                RetryWaiting = (int)JobStatus.RetryWaiting,
                Running = (int)JobStatus.Running,
                LockOwner = lockOwner,
                LockedUntil = now.Add(lockDuration)
            },
            transaction,
            cancellationToken: cancellationToken));

        if (claimed == 0)
        {
            transaction.Rollback();
            return null;
        }

        transaction.Commit();

        var entity = candidate.ToEntity();
        entity.Status = JobStatus.Running;
        entity.LockOwner = lockOwner;
        entity.LockedUntil = now.Add(lockDuration);
        return entity;
    }
}
