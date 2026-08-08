using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P5-S3 — Maps trusted FIFO allocation slices to allocation-explicit MT transfer
/// legacy write DTOs and post-transfer fingerprint projections.
/// Resolves <c>LegacyRowId</c> from live authority balances; does not re-FIFO.
/// </summary>
public static class TransferLegacyCompatibilityMapper
{
    private const string MutationKindMtOut = "MT_OUT";
    private const string MutationKindMtIn = "MT_IN";
    private const string PrefixBuku = "BK";
    private const string PrefixStok = "ST";

    /// <summary>
    /// Compact legacy BK/ST id (10 chars). Uses Ulid random tail — <c>NunaId.NewLegacyCompact</c>
    /// is timestamp-heavy and collides under rapid multi-leg generation in disposable DBs.
    /// </summary>
    private static string NewCompactLegacyId(string prefix)
    {
        var ulid = Ulid.NewUlid().ToString();
        return $"{prefix}{ulid[^8..]}";
    }
    /// <summary>
    /// One allocation-explicit transfer slice (Ledger plan → legacy OUT/IN pair).
    /// </summary>
    public sealed record TransferAllocationSlice(
        string BrgId,
        string ReceiptSourceId,
        string SourceLayananId,
        string DestinationLayananId,
        decimal Quantity,
        decimal UnitCost,
        DateOnly? ExpirationDate,
        string? Batch,
        string? PurchaseOrderId,
        string? SmallestUnitId = null);

    /// <summary>
    /// Fail-closed LegacyRowId resolution kinds for transfer mapper.
    /// </summary>
    public enum TransferLegacyRowResolutionFailureKind
    {
        NoMatch = 1,
        Ambiguous = 2
    }

    /// <summary>
    /// Thrown when source <c>tb_stok</c> row resolution cannot proceed safely.
    /// </summary>
    public sealed class TransferLegacyRowResolutionException : InvalidOperationException
    {
        public TransferLegacyRowResolutionException(
            TransferLegacyRowResolutionFailureKind kind,
            string message)
            : base(message)
        {
            Kind = kind;
        }

        public TransferLegacyRowResolutionFailureKind Kind { get; }
    }

    /// <summary>
    /// Builds interleaved OUT/IN DTO pairs from allocation slices.
    /// Resolves targeted source <c>tb_stok</c> rows from <paramref name="sourceBalances"/>;
    /// fails closed when matching is ambiguous or insufficient.
    /// </summary>
    public static LegacyCompatibilityWriteRequest Map(
        ISourceTransactionReferenceKey sourceTransaction,
        string mtTransactionId,
        DateTime mutationTime,
        IReadOnlyList<TransferAllocationSlice> slices,
        IReadOnlyList<LegacyStockBalanceType> sourceBalances)
    {
        ArgumentNullException.ThrowIfNull(sourceTransaction);
        ArgumentNullException.ThrowIfNull(slices);
        ArgumentNullException.ThrowIfNull(sourceBalances);
        if (slices.Count == 0)
            throw new ArgumentException("At least one transfer slice is required.", nameof(slices));
        if (string.IsNullOrWhiteSpace(mtTransactionId))
            throw new ArgumentException("MT transaction id is required.", nameof(mtTransactionId));
        if (mutationTime == default)
            throw new ArgumentException("MutationTime is required.", nameof(mutationTime));

        var mtId = mtTransactionId.Trim();
        var remaining = sourceBalances
            .Where(b => !string.IsNullOrWhiteSpace(b.LegacyRowId) && b.Quantity > 0m)
            .Select(b => b with { })
            .ToList();

        var balances = new List<LegacyCompatibilityBalanceMutationType>(slices.Count * 2);
        var journals = new List<LegacyCompatibilityJournalEntryType>(slices.Count * 2);

        for (var i = 0; i < slices.Count; i++)
        {
            var slice = slices[i];
            ValidateSlice(slice, i);

            if (!TryResolveSourceRow(remaining, slice, out var matchIndex, out var failureKind, out var error))
                throw new TransferLegacyRowResolutionException(failureKind, error);

            var sourceRow = remaining[matchIndex];
            var sourceStokId = sourceRow.LegacyRowId!.Trim();
            var remainingAfter = sourceRow.Quantity - slice.Quantity;
            var outAction = remainingAfter <= 0m
                ? LegacyBalanceMutationActionEnum.Delete
                : LegacyBalanceMutationActionEnum.Upsert;

            if (remainingAfter <= 0m)
                remaining.RemoveAt(matchIndex);
            else
                remaining[matchIndex] = sourceRow with { Quantity = remainingAfter };

            var outBukuId = NewCompactLegacyId(PrefixBuku);
            var inBukuId = NewCompactLegacyId(PrefixBuku);
            var inStokId = NewCompactLegacyId(PrefixStok);
            var brgId = slice.BrgId.Trim();
            var doId = slice.ReceiptSourceId.Trim();
            var sourceLoc = slice.SourceLayananId.Trim();
            var destLoc = slice.DestinationLayananId.Trim();
            var batch = NormalizeOptional(slice.Batch) ?? NormalizeOptional(sourceRow.Batch);
            var po = NormalizeOptional(slice.PurchaseOrderId)
                ?? NormalizeOptional(sourceRow.PurchaseOrderId);
            var satuan = NormalizeOptional(slice.SmallestUnitId);

            balances.Add(new LegacyCompatibilityBalanceMutationType(
                outAction,
                brgId,
                doId,
                sourceLoc,
                Quantity: slice.Quantity,
                UnitCost: slice.UnitCost,
                ExpirationDate: slice.ExpirationDate,
                Batch: batch,
                PurchaseOrderId: po,
                LegacyRowId: sourceStokId,
                SmallestUnitId: satuan));
            journals.Add(new LegacyCompatibilityJournalEntryType(
                LegacyJournalId: outBukuId,
                BrgId: brgId,
                ReceiptSourceId: doId,
                LayananId: sourceLoc,
                QuantityIn: 0m,
                QuantityOut: slice.Quantity,
                UnitCost: slice.UnitCost,
                ExpirationDate: slice.ExpirationDate,
                Batch: batch,
                MutationKindId: MutationKindMtOut,
                MutationTransactionId: mtId,
                MutationTime: mutationTime,
                IsVoid: false,
                SmallestUnitId: satuan));

            balances.Add(new LegacyCompatibilityBalanceMutationType(
                LegacyBalanceMutationActionEnum.Upsert,
                brgId,
                doId,
                destLoc,
                Quantity: slice.Quantity,
                UnitCost: slice.UnitCost,
                ExpirationDate: slice.ExpirationDate,
                Batch: batch,
                PurchaseOrderId: po,
                LegacyRowId: inStokId,
                SmallestUnitId: satuan));
            journals.Add(new LegacyCompatibilityJournalEntryType(
                LegacyJournalId: inBukuId,
                BrgId: brgId,
                ReceiptSourceId: doId,
                LayananId: destLoc,
                QuantityIn: slice.Quantity,
                QuantityOut: 0m,
                UnitCost: slice.UnitCost,
                ExpirationDate: slice.ExpirationDate,
                Batch: batch,
                MutationKindId: MutationKindMtIn,
                MutationTransactionId: mtId,
                MutationTime: mutationTime,
                IsVoid: false,
                SmallestUnitId: satuan));
        }

        var primaryScope = StockLedgerScopeKeyType.Create(
            slices[0].BrgId,
            slices[0].ReceiptSourceId);

        return new LegacyCompatibilityWriteRequest(
            sourceTransaction,
            StockMovementKindEnum.Transfer,
            primaryScope,
            balances,
            journals);
    }

    /// <summary>
    /// Projects post-transfer fingerprint inputs for one Reconstruction Scope:
    /// apply MT_OUT depletes / deletes at source, insert MT_IN balances at destination,
    /// append MT journals belonging to the scope.
    /// </summary>
    public static (
        IReadOnlyList<LegacyStockBalanceType> Balances,
        IReadOnlyList<LegacyStockJournalEntryType> Journals)
        ProjectPostTransferFingerprintSnapshot(
            IStockLedgerScopeKey scope,
            IReadOnlyList<LegacyStockBalanceType> priorBalances,
            IReadOnlyList<LegacyStockJournalEntryType> priorJournals,
            LegacyCompatibilityWriteRequest transferWrite)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(priorBalances);
        ArgumentNullException.ThrowIfNull(priorJournals);
        ArgumentNullException.ThrowIfNull(transferWrite);
        if (transferWrite.BalanceMutations.Count != transferWrite.JournalEntries.Count)
        {
            throw new InvalidOperationException(
                "Fingerprint snapshot requires BalanceMutations and JournalEntries paired 1:1.");
        }

        var scopeBrg = scope.BrgId.Trim();
        var scopeDo = scope.ReceiptSourceId.Trim();

        var scopedPairs = new List<(LegacyCompatibilityBalanceMutationType Balance, LegacyCompatibilityJournalEntryType Journal)>();
        for (var i = 0; i < transferWrite.BalanceMutations.Count; i++)
        {
            var b = transferWrite.BalanceMutations[i];
            var j = transferWrite.JournalEntries[i];
            if (!string.Equals(b.BrgId.Trim(), scopeBrg, StringComparison.Ordinal)
                || !string.Equals(b.ReceiptSourceId.Trim(), scopeDo, StringComparison.Ordinal))
            {
                continue;
            }

            scopedPairs.Add((b, j));
        }

        var deletedIds = scopedPairs
            .Where(p =>
                string.Equals(p.Journal.MutationKindId, MutationKindMtOut, StringComparison.Ordinal)
                && p.Balance.Action == LegacyBalanceMutationActionEnum.Delete)
            .Select(p => p.Balance.LegacyRowId!.Trim())
            .ToHashSet(StringComparer.Ordinal);

        var reducedById = scopedPairs
            .Where(p =>
                string.Equals(p.Journal.MutationKindId, MutationKindMtOut, StringComparison.Ordinal)
                && p.Balance.Action == LegacyBalanceMutationActionEnum.Upsert)
            .GroupBy(p => p.Balance.LegacyRowId!.Trim(), StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.Balance.Quantity),
                StringComparer.Ordinal);

        var balances = new List<LegacyStockBalanceType>();
        foreach (var prior in priorBalances)
        {
            var id = prior.LegacyRowId?.Trim();
            if (string.IsNullOrWhiteSpace(id))
                continue;
            if (deletedIds.Contains(id))
                continue;

            if (reducedById.TryGetValue(id, out var taken))
            {
                var remainingQty = prior.Quantity - taken;
                if (remainingQty <= 0m)
                    continue;

                balances.Add(prior with
                {
                    Quantity = remainingQty,
                    LastMutationTime = scopedPairs
                        .Select(p => p.Journal.MutationTime)
                        .DefaultIfEmpty(prior.LastMutationTime ?? default)
                        .Max()
                });
                continue;
            }

            balances.Add(prior);
        }

        foreach (var (balance, journal) in scopedPairs
                     .Where(p => string.Equals(p.Journal.MutationKindId, MutationKindMtIn, StringComparison.Ordinal)))
        {
            balances.Add(new LegacyStockBalanceType(
                balance.BrgId,
                balance.ReceiptSourceId,
                balance.LayananId,
                balance.Quantity,
                balance.UnitCost,
                balance.ExpirationDate,
                balance.Batch,
                balance.PurchaseOrderId,
                balance.LegacyRowId,
                ReceiptTime: null,
                LastMutationTime: journal.MutationTime));
        }

        var journals = priorJournals.ToList();
        foreach (var (balance, journal) in scopedPairs)
        {
            journals.Add(new LegacyStockJournalEntryType(
                journal.LegacyJournalId,
                journal.BrgId,
                journal.ReceiptSourceId,
                journal.LayananId,
                journal.QuantityIn,
                journal.QuantityOut,
                journal.UnitCost,
                journal.ExpirationDate,
                journal.Batch,
                journal.MutationKindId,
                journal.MutationTransactionId,
                journal.MutationTime,
                PurchaseOrderId: balance.PurchaseOrderId));
        }

        return (balances, journals);
    }

    private static void ValidateSlice(TransferAllocationSlice slice, int index)
    {
        if (string.IsNullOrWhiteSpace(slice.BrgId))
            throw new ArgumentException($"Transfer slice {index} requires BrgId.");
        if (string.IsNullOrWhiteSpace(slice.ReceiptSourceId))
            throw new ArgumentException($"Transfer slice {index} requires ReceiptSourceId.");
        if (string.IsNullOrWhiteSpace(slice.SourceLayananId))
            throw new ArgumentException($"Transfer slice {index} requires SourceLayananId.");
        if (string.IsNullOrWhiteSpace(slice.DestinationLayananId))
            throw new ArgumentException($"Transfer slice {index} requires DestinationLayananId.");
        if (string.Equals(
                slice.SourceLayananId.Trim(),
                slice.DestinationLayananId.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Transfer slice {index} source and destination LayananId must differ.");
        }

        if (slice.Quantity <= 0m)
            throw new ArgumentException($"Transfer slice {index} requires positive Quantity.");
    }

    private static bool TryResolveSourceRow(
        List<LegacyStockBalanceType> remaining,
        TransferAllocationSlice slice,
        out int matchIndex,
        out TransferLegacyRowResolutionFailureKind failureKind,
        out string error)
    {
        matchIndex = -1;
        failureKind = TransferLegacyRowResolutionFailureKind.NoMatch;
        error = string.Empty;

        var candidates = remaining
            .Select((b, i) => (Balance: b, Index: i))
            .Where(x =>
                string.Equals(x.Balance.BrgId, slice.BrgId.Trim(), StringComparison.Ordinal)
                && string.Equals(
                    x.Balance.ReceiptSourceId,
                    slice.ReceiptSourceId.Trim(),
                    StringComparison.Ordinal)
                && string.Equals(
                    x.Balance.LayananId,
                    slice.SourceLayananId.Trim(),
                    StringComparison.Ordinal)
                && x.Balance.UnitCost == slice.UnitCost
                && Nullable.Equals(x.Balance.ExpirationDate, slice.ExpirationDate)
                && string.Equals(
                    NormalizeOptional(x.Balance.Batch) ?? string.Empty,
                    NormalizeOptional(slice.Batch) ?? string.Empty,
                    StringComparison.Ordinal)
                && x.Balance.Quantity >= slice.Quantity)
            .ToList();

        if (candidates.Count == 0)
        {
            failureKind = TransferLegacyRowResolutionFailureKind.NoMatch;
            error =
                $"No legacy source balance for Item '{slice.BrgId}' DO '{slice.ReceiptSourceId}' "
                + $"at '{slice.SourceLayananId}' with qty ≥ {slice.Quantity}, "
                + $"HPP={slice.UnitCost}, ED={FormatEd(slice.ExpirationDate)}, "
                + $"Batch='{NormalizeOptional(slice.Batch) ?? "(none)"}'; "
                + "transfer mapper fails closed.";
            return false;
        }

        if (candidates.Count > 1)
        {
            // Prefer unique exact-qty match when present; otherwise fail closed.
            var exact = candidates.Where(c => c.Balance.Quantity == slice.Quantity).ToList();
            if (exact.Count == 1)
            {
                matchIndex = exact[0].Index;
                return true;
            }

            failureKind = TransferLegacyRowResolutionFailureKind.Ambiguous;
            error =
                $"Ambiguous legacy source balance match for Item '{slice.BrgId}' "
                + $"DO '{slice.ReceiptSourceId}' at '{slice.SourceLayananId}' "
                + $"({candidates.Count} candidates); transfer mapper fails closed.";
            return false;
        }

        matchIndex = candidates[0].Index;
        return true;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FormatEd(DateOnly? expirationDate)
        => expirationDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
           ?? "(none)";
}
