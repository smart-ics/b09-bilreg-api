using Ardalis.GuardClauses;

namespace Bilreg.Domain.InventoryContext.StockLedgerFeature;

/// <summary>
/// Pure-domain outbound allocation: Explicit ED → FEFO → FIFO (BR-STL-022…027).
/// NoBatch is not considered for eligibility or ordering (GAP-STL-005).
/// </summary>
public static class StockOutboundAllocator
{
    public static StockAllocationResult Allocate(
        string brgId,
        string layananId,
        decimal requestedQty,
        IEnumerable<StockAllocationCandidateType> candidates,
        DateTime? explicitTglEd = null)
    {
        Guard.Against.NullOrWhiteSpace(brgId);
        Guard.Against.NullOrWhiteSpace(layananId);
        Guard.Against.Null(candidates);
        if (requestedQty <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedQty), "Requested quantity must be positive.");

        // BR-STL-022: only requested Item + Stock Location; exclude depleted.
        var eligible = candidates
            .Where(c =>
                c.BrgId == brgId
                && c.LayananId == layananId
                && c.QtySisa > 0)
            .ToList();

        IEnumerable<StockAllocationCandidateType> ordered;

        if (explicitTglEd.HasValue)
        {
            // BR-STL-023: Explicit Expiry Selection — equal TglEd only.
            ordered = eligible
                .Where(c => c.TglEd == explicitTglEd.Value)
                .OrderBy(c => c.TglMasuk)
                .ThenBy(c => c.BrgMasukReffId, StringComparer.Ordinal);
        }
        else if (eligible.Any(c => c.TglEd != StockLedgerSentinel.EmptyDate))
        {
            // BR-STL-024: FEFO — earliest ED first; tie-break by receipt order.
            // Sentinel ED (absent) sorts last under ascending DateTime compare.
            ordered = eligible
                .OrderBy(c => c.TglEd)
                .ThenBy(c => c.TglMasuk)
                .ThenBy(c => c.BrgMasukReffId, StringComparer.Ordinal);
        }
        else
        {
            // BR-STL-025: FIFO by receipt order when no eligible balance has ED.
            ordered = eligible
                .OrderBy(c => c.TglMasuk)
                .ThenBy(c => c.BrgMasukReffId, StringComparer.Ordinal);
        }

        // BR-STL-026 / BR-STL-027: split across balances; never negative; explicit shortfall.
        var lines = new List<StockAllocationLineType>();
        var remaining = requestedQty;

        foreach (var candidate in ordered)
        {
            if (remaining <= 0)
                break;

            var take = Math.Min(candidate.QtySisa, remaining);
            if (take <= 0)
                continue;

            lines.Add(new StockAllocationLineType(
                candidate.StokLokasiId,
                candidate.StokBatchId,
                candidate.BrgMasukReffId,
                candidate.TglEd,
                candidate.TglMasuk,
                take));
            remaining -= take;
        }

        if (remaining > 0)
            return StockAllocationResult.Insufficient(requestedQty, lines);

        return StockAllocationResult.Success(requestedQty, lines);
    }
}
