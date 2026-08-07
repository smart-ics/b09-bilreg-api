using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Deterministic ED-constrained FIFO allocation (BR-STL-029–038).
/// Does not use FEFO. Batch is never a selection or ordering key.
/// </summary>
public static class StockFifoAllocator
{
    /// <summary>
    /// Allocates quantity from candidate layers for the requested Item and Stock Location.
    /// When <paramref name="expirationDate"/> is supplied, only layers with that exact
    /// Expiration Date are eligible. Ordering: Effective Receipt Time ascending,
    /// then Stock Layer ID.
    /// </summary>
    public static StockAllocationResult Allocate(
        IEnumerable<StockLayerModel> layers,
        IBrgKey item,
        ILayananKey stockLocation,
        decimal quantity,
        DateOnly? expirationDate = null)
    {
        Guard.Against.Null(layers, nameof(layers));
        Guard.Against.Null(item, nameof(item));
        Guard.Against.Null(stockLocation, nameof(stockLocation));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        var allLayers = layers.ToArray();
        var eligible = SelectEligible(allLayers, item.BrgId, stockLocation.LayananId, expirationDate)
            .ToArray();

        var available = eligible.Sum(x => x.RemainingQuantity);
        if (available < quantity)
            return StockAllocationResult.Insufficient(quantity, available);

        var remainingToAllocate = quantity;
        var allocations = new List<StockLayerAllocationType>();
        var consumedById = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var layer in eligible)
        {
            if (remainingToAllocate == 0m)
                break;

            var take = Math.Min(layer.RemainingQuantity, remainingToAllocate);
            allocations.Add(new StockLayerAllocationType(
                layer.StockLayerId,
                layer.BrgId,
                layer.ReceiptSourceId,
                layer.LayananId,
                take,
                layer.UnitValuation,
                layer.ExpirationDate,
                layer.Origin));

            consumedById[layer.StockLayerId] = take;
            remainingToAllocate -= take;
        }

        if (remainingToAllocate != 0m)
            return StockAllocationResult.Insufficient(quantity, available);

        var updatedLayers = allLayers
            .Select(layer => consumedById.TryGetValue(layer.StockLayerId, out var taken)
                ? layer.Consume(taken)
                : layer)
            .ToArray();

        return StockAllocationResult.Fulfilled(quantity, allocations, updatedLayers);
    }

    internal static IEnumerable<StockLayerModel> SelectEligible(
        IEnumerable<StockLayerModel> layers,
        string brgId,
        string layananId,
        DateOnly? expirationDate)
    {
        var query = layers
            .Where(x => x.BrgId == brgId)
            .Where(x => x.LayananId == layananId)
            .Where(x => x.HasAvailableQuantity);

        if (expirationDate is { } ed)
            query = query.Where(x => x.ExpirationDate == ed);

        // Batch intentionally omitted from selection and ordering (not FEFO).
        return query
            .OrderBy(x => x.EffectiveReceiptTime)
            .ThenBy(x => x.StockLayerId, StringComparer.Ordinal);
    }
}
