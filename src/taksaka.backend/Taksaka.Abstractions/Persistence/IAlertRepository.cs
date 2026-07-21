using Taksaka.Core.Entities;

namespace Taksaka.Abstractions.Persistence;

public interface IAlertRepository
{
    Task InsertAsync(Alert alert, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Alert>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
}
