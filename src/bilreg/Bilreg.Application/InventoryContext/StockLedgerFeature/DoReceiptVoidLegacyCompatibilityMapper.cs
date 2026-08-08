using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P4-S4 — Maps full Native DO Receipt void targets to legacy compatibility write DTOs
/// and post-void fingerprint projections. Default mode: compensating <c>DO_V</c>
/// (<c>xVoidDelete=False</c>). Not a generic void framework.
/// </summary>
public static class DoReceiptVoidLegacyCompatibilityMapper
{
    private const string MutationKindDoVoid = "DO_V";
    private const string PrefixBuku = "BK";

    public sealed record VoidLineTarget(
        string LayananId,
        decimal Quantity,
        decimal UnitCost,
        DateOnly? ExpirationDate,
        string? Batch,
        string? PurchaseOrderId,
        string LegacyRowId,
        string? SmallestUnitId = null);

    public static LegacyCompatibilityWriteRequest Map(
        ISourceTransactionReferenceKey sourceTransaction,
        IStockLedgerScopeKey scope,
        DateTime mutationTime,
        IReadOnlyList<VoidLineTarget> lines)
    {
        ArgumentNullException.ThrowIfNull(sourceTransaction);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
            throw new ArgumentException("At least one void line is required.", nameof(lines));
        if (mutationTime == default)
            throw new ArgumentException("MutationTime is required.", nameof(mutationTime));

        var balances = new List<LegacyCompatibilityBalanceMutationType>(lines.Count);
        var journals = new List<LegacyCompatibilityJournalEntryType>(lines.Count);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line.LayananId))
                throw new ArgumentException("Void line requires LayananId.");
            if (string.IsNullOrWhiteSpace(line.LegacyRowId))
                throw new ArgumentException("Void line requires LegacyRowId.");
            if (line.Quantity <= 0m)
                throw new ArgumentException("Void line requires positive Quantity.");

            var bukuId = NunaId.NewLegacyCompact(PrefixBuku);
            var brgId = scope.BrgId.Trim();
            var doId = scope.ReceiptSourceId.Trim();
            var layananId = line.LayananId.Trim();
            var stokId = line.LegacyRowId.Trim();

            balances.Add(new LegacyCompatibilityBalanceMutationType(
                LegacyBalanceMutationActionEnum.Delete,
                brgId,
                doId,
                layananId,
                Quantity: line.Quantity,
                UnitCost: line.UnitCost,
                ExpirationDate: line.ExpirationDate,
                Batch: line.Batch,
                PurchaseOrderId: line.PurchaseOrderId,
                LegacyRowId: stokId,
                SmallestUnitId: line.SmallestUnitId));

            journals.Add(new LegacyCompatibilityJournalEntryType(
                LegacyJournalId: bukuId,
                BrgId: brgId,
                ReceiptSourceId: doId,
                LayananId: layananId,
                QuantityIn: 0m,
                QuantityOut: line.Quantity,
                UnitCost: line.UnitCost,
                ExpirationDate: line.ExpirationDate,
                Batch: line.Batch,
                MutationKindId: MutationKindDoVoid,
                MutationTransactionId: doId,
                MutationTime: mutationTime,
                IsVoid: true,
                SmallestUnitId: line.SmallestUnitId));
        }

        return new LegacyCompatibilityWriteRequest(
            sourceTransaction,
            StockMovementKindEnum.Reversal,
            scope,
            balances,
            journals);
    }

    /// <summary>
    /// Projects post-void fingerprint inputs: balances after Delete removals,
    /// journals = prior journals + new <c>DO_V</c> entries.
    /// </summary>
    public static (
        IReadOnlyList<LegacyStockBalanceType> Balances,
        IReadOnlyList<LegacyStockJournalEntryType> Journals)
        ProjectPostVoidFingerprintSnapshot(
            IReadOnlyList<LegacyStockBalanceType> priorBalances,
            IReadOnlyList<LegacyStockJournalEntryType> priorJournals,
            LegacyCompatibilityWriteRequest voidWrite)
    {
        ArgumentNullException.ThrowIfNull(priorBalances);
        ArgumentNullException.ThrowIfNull(priorJournals);
        ArgumentNullException.ThrowIfNull(voidWrite);
        if (voidWrite.BalanceMutations.Count != voidWrite.JournalEntries.Count)
        {
            throw new InvalidOperationException(
                "Fingerprint snapshot requires BalanceMutations and JournalEntries paired 1:1.");
        }

        var deletedIds = voidWrite.BalanceMutations
            .Where(b => b.Action == LegacyBalanceMutationActionEnum.Delete)
            .Select(b => b.LegacyRowId!.Trim())
            .ToHashSet(StringComparer.Ordinal);

        var reducedById = voidWrite.BalanceMutations
            .Where(b => b.Action == LegacyBalanceMutationActionEnum.Upsert)
            .ToDictionary(
                b => b.LegacyRowId!.Trim(),
                b => b,
                StringComparer.Ordinal);

        var balances = new List<LegacyStockBalanceType>();
        foreach (var prior in priorBalances)
        {
            var id = prior.LegacyRowId?.Trim();
            if (string.IsNullOrWhiteSpace(id))
                continue;
            if (deletedIds.Contains(id))
                continue;

            if (reducedById.TryGetValue(id, out var mutation))
            {
                var remaining = prior.Quantity - mutation.Quantity;
                if (remaining <= 0m)
                    continue;

                balances.Add(prior with
                {
                    Quantity = remaining,
                    LastMutationTime = voidWrite.JournalEntries
                        .FirstOrDefault(j =>
                            string.Equals(j.LayananId, prior.LayananId, StringComparison.Ordinal))
                        ?.MutationTime
                        ?? prior.LastMutationTime
                });
                continue;
            }

            balances.Add(prior);
        }

        var journals = priorJournals.ToList();
        for (var i = 0; i < voidWrite.JournalEntries.Count; i++)
        {
            var j = voidWrite.JournalEntries[i];
            var b = voidWrite.BalanceMutations[i];
            journals.Add(new LegacyStockJournalEntryType(
                j.LegacyJournalId,
                j.BrgId,
                j.ReceiptSourceId,
                j.LayananId,
                j.QuantityIn,
                j.QuantityOut,
                j.UnitCost,
                j.ExpirationDate,
                j.Batch,
                j.MutationKindId,
                j.MutationTransactionId,
                j.MutationTime,
                PurchaseOrderId: b.PurchaseOrderId));
        }

        return (balances, journals);
    }
}
