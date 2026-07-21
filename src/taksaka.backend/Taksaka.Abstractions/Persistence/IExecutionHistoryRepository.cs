using Taksaka.Core.Entities;

namespace Taksaka.Abstractions.Persistence;

public interface IExecutionHistoryRepository
{
    Task InsertAsync(ExecutionHistory entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExecutionHistory>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
}
