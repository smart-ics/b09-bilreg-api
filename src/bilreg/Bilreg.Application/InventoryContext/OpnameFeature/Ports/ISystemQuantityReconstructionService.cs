using Bilreg.Domain.InventoryContext.OpnameFeature;

namespace Bilreg.Application.InventoryContext.OpnameFeature.Ports;

public interface ISystemQuantityReconstructionService
{
    Task<IReadOnlyDictionary<ItemTupleKey, decimal>> ReconstructBalancesAsync(
        string layananId,
        DateTime cutoffTime,
        IEnumerable<string>? filterBrgIds = null,
        CancellationToken cancellationToken = default);
}
