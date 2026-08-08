using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S1 — Pure fingerprint compare + set-diff classification for Legacy Change Discovery (G-13).
/// SQL-free and side-effect free; live adapter supplies legacy snapshot + Ledger-known identity sets.
/// </summary>
public static class LegacyChangeDiscoveryClassifier
{
    public sealed record KnownLegacyIdentitySet(
        IReadOnlyDictionary<LegacyJournalIdentity, LegacyJournalMaterialSnapshot> Journals,
        IReadOnlyDictionary<LegacyBalanceIdentity, LegacyBalanceMaterialSnapshot> Balances);

    public static LegacyChangeDiscoveryResult Classify(
        IStockLedgerScopeKey scope,
        SynchronizationPositionType? storedPosition,
        SynchronizationPositionType currentFingerprint,
        IReadOnlyList<LegacyStockBalanceType> currentBalances,
        IReadOnlyList<LegacyStockJournalEntryType> currentJournals,
        KnownLegacyIdentitySet known)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(currentFingerprint);
        ArgumentNullException.ThrowIfNull(currentBalances);
        ArgumentNullException.ThrowIfNull(currentJournals);
        ArgumentNullException.ThrowIfNull(known);

        if (storedPosition is null)
        {
            return new LegacyChangeDiscoveryResult(
                LegacyChangeDiscoveryOutcomeEnum.Undeterminable,
                currentFingerprint,
                Array.Empty<LegacyDiscoveredDeltaType>(),
                "No stored Synchronization Position; discovery requires a reconstructed baseline or prior sync position.");
        }

        if (!string.Equals(
                storedPosition.AlgorithmVersion,
                LegacyReconstructionBasisCalculator.AlgorithmVersion,
                StringComparison.Ordinal))
        {
            return new LegacyChangeDiscoveryResult(
                LegacyChangeDiscoveryOutcomeEnum.Undeterminable,
                currentFingerprint,
                Array.Empty<LegacyDiscoveredDeltaType>(),
                $"Stored algorithm version '{storedPosition.AlgorithmVersion}' does not match " +
                $"current '{LegacyReconstructionBasisCalculator.AlgorithmVersion}'.");
        }

        if (storedPosition.Equals(currentFingerprint))
        {
            return new LegacyChangeDiscoveryResult(
                LegacyChangeDiscoveryOutcomeEnum.Unchanged,
                currentFingerprint,
                Array.Empty<LegacyDiscoveredDeltaType>(),
                null);
        }

        if (TryFindDuplicateJournalIdentities(currentJournals, out var duplicateJournalReason))
        {
            return new LegacyChangeDiscoveryResult(
                LegacyChangeDiscoveryOutcomeEnum.Undeterminable,
                currentFingerprint,
                Array.Empty<LegacyDiscoveredDeltaType>(),
                duplicateJournalReason);
        }

        if (TryFindDuplicateBalanceIdentities(currentBalances, out var duplicateBalanceReason))
        {
            return new LegacyChangeDiscoveryResult(
                LegacyChangeDiscoveryOutcomeEnum.Undeterminable,
                currentFingerprint,
                Array.Empty<LegacyDiscoveredDeltaType>(),
                duplicateBalanceReason);
        }

        if (known.Journals.Count == 0 && known.Balances.Count == 0)
        {
            return new LegacyChangeDiscoveryResult(
                LegacyChangeDiscoveryOutcomeEnum.ChangesDetected,
                currentFingerprint,
                [
                    new LegacyDiscoveredDeltaType(
                        LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive,
                        LegacyJournalId: null,
                        LayananId: null,
                        QuantityIn: null,
                        QuantityOut: null,
                        MutationTime: null,
                        Explanation:
                            "Fingerprint mismatch with no Ledger-known legacy identities to set-diff safely; " +
                            "scoped re-derive is required (hash drift alone is not a delete event stream).")
                ],
                "Legacy authority fingerprint differs from stored Synchronization Position.");
        }

        var deltas = new List<LegacyDiscoveredDeltaType>();
        var currentJournalMap = BuildJournalMap(currentJournals);
        var currentBalanceMap = BuildBalanceMap(currentBalances);

        foreach (var (knownIdentity, knownMaterial) in known.Journals)
        {
            if (currentJournalMap.TryGetValue(knownIdentity, out var current))
            {
                if (!JournalFingerprintMaterialEquals(current, knownMaterial))
                {
                    deltas.Add(new LegacyDiscoveredDeltaType(
                        LegacyDiscoveredDeltaKindEnum.JournalUpdate,
                        current.LegacyJournalId,
                        current.LayananId,
                        current.QuantityIn,
                        current.QuantityOut,
                        current.MutationTime,
                        "Surviving legacy journal material attributes differ from Ledger-known snapshot."));
                }

                continue;
            }

            deltas.Add(new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.JournalVoidDelete,
                knownIdentity.LegacyJournalId,
                knownIdentity.LayananId,
                null,
                null,
                null,
                "Ledger-known legacy journal identity is absent from surviving tb_buku rows."));
        }

        foreach (var current in currentJournalMap.Values)
        {
            var identity = new LegacyJournalIdentity(current.LayananId, current.LegacyJournalId);
            if (!known.Journals.ContainsKey(identity))
            {
                deltas.Add(new LegacyDiscoveredDeltaType(
                    LegacyDiscoveredDeltaKindEnum.JournalInsert,
                    current.LegacyJournalId,
                    current.LayananId,
                    current.QuantityIn,
                    current.QuantityOut,
                    current.MutationTime,
                    "Legacy journal identity is present in tb_buku but not Ledger-known."));
            }
        }

        foreach (var (knownIdentity, knownMaterial) in known.Balances)
        {
            if (currentBalanceMap.TryGetValue(knownIdentity, out var current))
            {
                if (!BalanceFingerprintMaterialEquals(current, knownMaterial))
                {
                    deltas.Add(new LegacyDiscoveredDeltaType(
                        LegacyDiscoveredDeltaKindEnum.BalanceUpdate,
                        LegacyJournalId: current.LegacyRowId,
                        current.LayananId,
                        QuantityIn: current.Quantity,
                        QuantityOut: null,
                        MutationTime: current.LastMutationTime,
                        Explanation: "Surviving tb_stok balance material attributes differ from Ledger-known snapshot."));
                }

                continue;
            }

            deltas.Add(new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.BalanceDelete,
                LegacyJournalId: knownIdentity.LegacyRowId,
                knownIdentity.LayananId,
                null,
                null,
                null,
                "Ledger-known legacy balance identity is absent from surviving tb_stok rows."));
        }

        if (deltas.Count == 0)
        {
            return new LegacyChangeDiscoveryResult(
                LegacyChangeDiscoveryOutcomeEnum.ChangesDetected,
                currentFingerprint,
                [
                    new LegacyDiscoveredDeltaType(
                        LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive,
                        LegacyJournalId: null,
                        LayananId: null,
                        QuantityIn: null,
                        QuantityOut: null,
                        MutationTime: null,
                        Explanation:
                            "Fingerprint mismatch could not be classified by set-diff; scoped re-derive is required.")
                ],
                "Legacy authority fingerprint differs from stored Synchronization Position.");
        }

        if (HasUnclassifiedNewBalanceRows(currentBalances, known.Balances))
        {
            deltas.Add(new LegacyDiscoveredDeltaType(
                LegacyDiscoveredDeltaKindEnum.RequiresScopedReDerive,
                LegacyJournalId: null,
                LayananId: null,
                QuantityIn: null,
                QuantityOut: null,
                MutationTime: null,
                Explanation:
                    "Surviving tb_stok row identities are present without Ledger-known balance anchors; scoped re-derive is required."));
        }

        return new LegacyChangeDiscoveryResult(
            LegacyChangeDiscoveryOutcomeEnum.ChangesDetected,
            currentFingerprint,
            deltas,
            "Legacy authority fingerprint differs from stored Synchronization Position.");
    }

    public static KnownLegacyIdentitySet ParseKnownIdentities(IEnumerable<string> idempotencyKeys)
    {
        var journals = new Dictionary<LegacyJournalIdentity, LegacyJournalMaterialSnapshot>();
        var balances = new Dictionary<LegacyBalanceIdentity, LegacyBalanceMaterialSnapshot>();

        foreach (var key in idempotencyKeys)
        {
            if (LegacyChangeDiscoveryIdentityKeys.TryParseJournalKey(key, out var journalIdentity, out var journalMaterial))
            {
                journals[journalIdentity] = journalMaterial;
                continue;
            }

            if (LegacyChangeDiscoveryIdentityKeys.TryParseBalanceKey(key, out var balanceIdentity, out var balanceMaterial))
                balances[balanceIdentity] = balanceMaterial;
        }

        return new KnownLegacyIdentitySet(journals, balances);
    }

    private static bool HasUnclassifiedNewBalanceRows(
        IReadOnlyList<LegacyStockBalanceType> currentBalances,
        IReadOnlyDictionary<LegacyBalanceIdentity, LegacyBalanceMaterialSnapshot> knownBalances)
    {
        foreach (var balance in currentBalances)
        {
            if (string.IsNullOrWhiteSpace(balance.LegacyRowId))
                continue;

            var identity = new LegacyBalanceIdentity(balance.LayananId, balance.LegacyRowId);
            if (!knownBalances.ContainsKey(identity))
                return true;
        }

        return false;
    }

    private static Dictionary<LegacyJournalIdentity, LegacyStockJournalEntryType> BuildJournalMap(
        IReadOnlyList<LegacyStockJournalEntryType> journals)
        => journals.ToDictionary(
            j => new LegacyJournalIdentity(j.LayananId, j.LegacyJournalId),
            j => j);

    private static Dictionary<LegacyBalanceIdentity, LegacyStockBalanceType> BuildBalanceMap(
        IReadOnlyList<LegacyStockBalanceType> balances)
        => balances
            .Where(b => !string.IsNullOrWhiteSpace(b.LegacyRowId))
            .ToDictionary(
                b => new LegacyBalanceIdentity(b.LayananId, b.LegacyRowId!),
                b => b);

    private static bool TryFindDuplicateJournalIdentities(
        IReadOnlyList<LegacyStockJournalEntryType> journals,
        out string reason)
    {
        var seen = new HashSet<LegacyJournalIdentity>();
        foreach (var journal in journals)
        {
            var identity = new LegacyJournalIdentity(journal.LayananId, journal.LegacyJournalId);
            if (!seen.Add(identity))
            {
                reason =
                    $"Ambiguous legacy journal identity '{journal.LegacyJournalId}' at location '{journal.LayananId}' " +
                    "appears more than once in surviving tb_buku rows.";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryFindDuplicateBalanceIdentities(
        IReadOnlyList<LegacyStockBalanceType> balances,
        out string reason)
    {
        var seen = new HashSet<LegacyBalanceIdentity>();
        foreach (var balance in balances)
        {
            if (string.IsNullOrWhiteSpace(balance.LegacyRowId))
                continue;

            var identity = new LegacyBalanceIdentity(balance.LayananId, balance.LegacyRowId);
            if (!seen.Add(identity))
            {
                reason =
                    $"Ambiguous legacy balance identity '{balance.LegacyRowId}' at location '{balance.LayananId}' " +
                    "appears more than once in surviving tb_stok rows.";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    internal static bool JournalFingerprintMaterialEquals(
        LegacyStockJournalEntryType current,
        LegacyJournalMaterialSnapshot known)
        => current.MutationKindId == known.MutationKindId
           && current.MutationTransactionId == known.MutationTransactionId
           && current.QuantityIn == known.QuantityIn
           && current.QuantityOut == known.QuantityOut
           && current.UnitCost == known.UnitCost
           && current.ExpirationDate == known.ExpirationDate
           && current.Batch == known.Batch
           && current.MutationTime == known.MutationTime
           && current.PurchaseOrderId == known.PurchaseOrderId;

    internal static bool BalanceFingerprintMaterialEquals(
        LegacyStockBalanceType current,
        LegacyBalanceMaterialSnapshot known)
        => current.Quantity == known.Quantity
           && current.UnitCost == known.UnitCost
           && current.ExpirationDate == known.ExpirationDate
           && current.Batch == known.Batch
           && current.PurchaseOrderId == known.PurchaseOrderId;
}
