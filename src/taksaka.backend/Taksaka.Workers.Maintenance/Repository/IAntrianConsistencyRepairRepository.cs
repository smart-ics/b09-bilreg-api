using Taksaka.Workers.Maintenance.Models;

namespace Taksaka.Workers.Maintenance.Repository;

internal interface IAntrianConsistencyRepairRepository
{
    Task<IReadOnlyList<AntrianConsistencyItem>> FindInconsistentAsync(
        int batchSize,
        CancellationToken cancellationToken);

    Task<int> RepairAsync(
        string antrianId,
        int noUrut,
        string registrationId,
        CancellationToken cancellationToken);
}
