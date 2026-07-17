using System.Data;
using Taksaka.Core.Entities;

namespace Taksaka.Abstractions.Persistence;

public interface IJobRepository
{
    Task InsertAsync(Job job, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(Job job, IDbTransaction? transaction = null, CancellationToken cancellationToken = default);

    Task<Job> CreateReplayAsync(Guid sourceJobId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Job>> GetRetryReadyAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);
}
