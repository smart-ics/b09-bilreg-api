using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P4-S2 — Maps authorized DO Receipt line facts to legacy compatibility write DTOs
/// and fingerprint snapshot projections. Pre-assigns BK/ST ids so Scope position can be
/// computed before UoW legacy Apply. Not a generic stock framework.
/// </summary>
public static class DoReceiptLegacyCompatibilityMapper
{
    private const string MutationKindDo = "DO";
    private const string PrefixBuku = "BK";
    private const string PrefixStok = "ST";

    public static LegacyCompatibilityWriteRequest Map(
        ISourceTransactionReferenceKey sourceTransaction,
        IStockLedgerScopeKey scope,
        DateTime mutationTime,
        IReadOnlyList<DoReceiptLineFact> lines)
    {
        ArgumentNullException.ThrowIfNull(sourceTransaction);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(lines);
        if (lines.Count == 0)
            throw new ArgumentException("At least one DO receipt line is required.", nameof(lines));
        if (mutationTime == default)
            throw new ArgumentException("MutationTime is required.", nameof(mutationTime));

        var balances = new List<LegacyCompatibilityBalanceMutationType>(lines.Count);
        var journals = new List<LegacyCompatibilityJournalEntryType>(lines.Count);

        foreach (var line in lines.OrderBy(l => l.LineNumber))
        {
            if (string.IsNullOrWhiteSpace(line.LayananId))
                throw new ArgumentException($"Line {line.LineNumber} requires LayananId.");
            if (line.Quantity <= 0m)
                throw new ArgumentException($"Line {line.LineNumber} requires positive Quantity.");

            var bukuId = NunaId.NewLegacyCompact(PrefixBuku);
            var stokId = NunaId.NewLegacyCompact(PrefixStok);
            var brgId = scope.BrgId.Trim();
            var doId = scope.ReceiptSourceId.Trim();
            var layananId = line.LayananId.Trim();

            balances.Add(new LegacyCompatibilityBalanceMutationType(
                LegacyBalanceMutationActionEnum.Upsert,
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
                QuantityIn: line.Quantity,
                QuantityOut: 0m,
                UnitCost: line.UnitCost,
                ExpirationDate: line.ExpirationDate,
                Batch: line.Batch,
                MutationKindId: MutationKindDo,
                MutationTransactionId: doId,
                MutationTime: mutationTime,
                IsVoid: false,
                SmallestUnitId: line.SmallestUnitId));
        }

        return new LegacyCompatibilityWriteRequest(
            sourceTransaction,
            StockMovementKindEnum.Receipt,
            scope,
            balances,
            journals);
    }

    /// <summary>
    /// Projects the mapped write request into fingerprint calculator inputs.
    /// Ids must already be pre-assigned on journal/balance DTOs (1:1 by index).
    /// </summary>
    public static (
        IReadOnlyList<LegacyStockBalanceType> Balances,
        IReadOnlyList<LegacyStockJournalEntryType> Journals)
        ToFingerprintSnapshot(LegacyCompatibilityWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.BalanceMutations.Count != request.JournalEntries.Count)
        {
            throw new InvalidOperationException(
                "Fingerprint snapshot requires BalanceMutations and JournalEntries paired 1:1.");
        }

        var balances = new List<LegacyStockBalanceType>(request.BalanceMutations.Count);
        var journals = new List<LegacyStockJournalEntryType>(request.JournalEntries.Count);

        for (var i = 0; i < request.BalanceMutations.Count; i++)
        {
            var b = request.BalanceMutations[i];
            var j = request.JournalEntries[i];

            balances.Add(new LegacyStockBalanceType(
                b.BrgId,
                b.ReceiptSourceId,
                b.LayananId,
                b.Quantity,
                b.UnitCost,
                b.ExpirationDate,
                b.Batch,
                b.PurchaseOrderId,
                b.LegacyRowId,
                ReceiptTime: null,
                LastMutationTime: j.MutationTime));

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
