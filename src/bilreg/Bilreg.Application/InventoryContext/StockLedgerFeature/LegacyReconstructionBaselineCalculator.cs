using System.Security.Cryptography;
using System.Text;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S4 — Pure calculation of a proposed reconstructed Stock Ledger baseline from a
/// scoped P2-S1 legacy snapshot (balances + journals). Classifies <c>Balanced</c> vs
/// <c>Inconsistent</c> with an explicit reason. Does not claim, persist, fingerprint,
/// or transfer runtime authority.
/// <para>
/// Balance-anchored: surviving <c>tb_stok</c> Remaining Quantity is authoritative;
/// <c>tb_buku</c> enriches provenance and supports depleted reconstructed layers when
/// zero-balance legacy rows are absent (BR-STL-065–068, BR-STL-080, BR-STL-102–105).
/// </para>
/// </summary>
public static class LegacyReconstructionBaselineCalculator
{
    /// <summary>
    /// Deterministic Effective Receipt Time when neither balance nor journal supplies one.
    /// </summary>
    public static readonly DateTime UnknownReconstructedReceiptTime =
        new(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    /// Calculates a proposed baseline for one Item + Receipt Source across all Stock Locations.
    /// Equivalent inputs produce deterministic output (stable ordering and synthetic identities).
    /// </summary>
    public static ReconstructionBaselineCalculationResult Calculate(
        IStockLedgerScopeKey scope,
        IReadOnlyList<LegacyStockBalanceType> balances,
        IReadOnlyList<LegacyStockJournalEntryType> journals)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(balances);
        ArgumentNullException.ThrowIfNull(journals);

        var scopeKey = StockLedgerScopeKeyType.Create(scope.BrgId, scope.ReceiptSourceId);
        var item = BrgObatType.Key(scope.BrgId);
        var receiptSource = ReceiptSourceType.Create(scope.ReceiptSourceId);

        var orderedBalances = NormalizeBalances(balances);
        var orderedJournals = NormalizeJournals(journals);

        if (TryFindScopeMismatch(scopeKey, orderedBalances, orderedJournals, out var scopeReason))
            return Inconsistent(scopeKey, scopeReason);

        if (TryFindInvalidBalanceQuantity(orderedBalances, out var qtyReason))
            return Inconsistent(scopeKey, qtyReason);

        // Provenance key: location + unit cost + expiration date.
        // Batch is informational only and must not drive allocation or layer identity (BR-STL-015).
        var journalGroups = orderedJournals
            .GroupBy(j => ProvenanceKey.FromJournal(j))
            .ToDictionary(g => g.Key, g => JournalAggregate.From(g));

        var balanceGroups = orderedBalances
            .GroupBy(b => ProvenanceKey.FromBalance(b))
            .ToDictionary(g => g.Key, g => g.ToList());

        var allKeys = journalGroups.Keys
            .Union(balanceGroups.Keys)
            .OrderBy(k => k.LayananId, StringComparer.Ordinal)
            .ThenBy(k => k.UnitCost)
            .ThenBy(k => k.ExpirationDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        if (allKeys.Count == 0)
        {
            return ReconstructionBaselineCalculationResult.Balanced(
                scopeKey,
                establishingMovement: null,
                proposedPositions: Array.Empty<StockPositionModel>(),
                usedDeterministicOrderingFallback: false);
        }

        var proposedLayers = new List<ProposedLayerDraft>();
        var usedFallbackOrdering = false;
        var layerSeq = 0;

        foreach (var key in allKeys)
        {
            balanceGroups.TryGetValue(key, out var balanceRows);
            balanceRows ??= [];
            journalGroups.TryGetValue(key, out var journalAgg);

            var balanceQty = balanceRows.Sum(b => b.Quantity);
            var hasJournal = journalAgg is not null;
            var journalIn = journalAgg?.QuantityIn ?? 0m;
            var journalOut = journalAgg?.QuantityOut ?? 0m;
            var journalNet = journalIn - journalOut;

            if (hasJournal && journalNet != balanceQty)
            {
                return Inconsistent(
                    scopeKey,
                    $"Quantity mismatch at location '{key.LayananId}' for unit cost {key.UnitCost} / expiration '{FormatEd(key.ExpirationDate)}': " +
                    $"legacy balance remaining {balanceQty} does not equal journal net {journalNet} (in={journalIn}, out={journalOut}).");
            }

            if (balanceQty > 0m)
            {
                if (balanceRows.Count > 1 && hasJournal)
                {
                    return Inconsistent(
                        scopeKey,
                        $"Material ambiguity at location '{key.LayananId}' for unit cost {key.UnitCost} / expiration '{FormatEd(key.ExpirationDate)}': " +
                        $"multiple surviving balance rows share provenance attributes while journal history exists; " +
                        $"Initial Quantity / layer provenance cannot be assigned without inventing identities (BR-STL-105).");
                }

                foreach (var balance in balanceRows.OrderBy(b => b.LegacyRowId ?? string.Empty, StringComparer.Ordinal))
                {
                    layerSeq++;
                    var initial = hasJournal ? journalIn : balance.Quantity;
                    if (!hasJournal)
                        usedFallbackOrdering = true;

                    var receiptTime = ResolveReceiptTime(balance, journalAgg);
                    if (receiptTime == UnknownReconstructedReceiptTime)
                        usedFallbackOrdering = true;

                    proposedLayers.Add(new ProposedLayerDraft(
                        Sequence: layerSeq,
                        LayananId: key.LayananId,
                        InitialQuantity: initial,
                        RemainingQuantity: balance.Quantity,
                        UnitCost: balance.UnitCost,
                        ExpirationDate: balance.ExpirationDate,
                        Batch: balance.Batch,
                        EffectiveReceiptTime: receiptTime,
                        IsDepleted: false,
                        LegacyRowId: balance.LegacyRowId));
                }
            }
            else if (hasJournal)
            {
                // No surviving tb_stok row: retain depleted reconstructed layer when history supports it.
                if (journalNet != 0m)
                {
                    return Inconsistent(
                        scopeKey,
                        $"Quantity mismatch at location '{key.LayananId}' for unit cost {key.UnitCost} / expiration '{FormatEd(key.ExpirationDate)}': " +
                        $"no surviving legacy balance but journal net is {journalNet}.");
                }

                if (journalIn <= 0m)
                {
                    return Inconsistent(
                        scopeKey,
                        $"Material ambiguity at location '{key.LayananId}' for unit cost {key.UnitCost} / expiration '{FormatEd(key.ExpirationDate)}': " +
                        $"journal history has no inbound quantity to establish a reconstructed layer without inventing identities (BR-STL-065).");
                }

                layerSeq++;
                proposedLayers.Add(new ProposedLayerDraft(
                    Sequence: layerSeq,
                    LayananId: key.LayananId,
                    InitialQuantity: journalIn,
                    RemainingQuantity: 0m,
                    UnitCost: key.UnitCost,
                    ExpirationDate: key.ExpirationDate,
                    Batch: journalAgg!.Batch,
                    EffectiveReceiptTime: journalAgg.EarliestMutationTime,
                    IsDepleted: true,
                    LegacyRowId: null));
            }
        }

        // Authority check: proposed Remaining must equal sum of surviving balances (never force-balance).
        var proposedRemaining = proposedLayers.Sum(l => l.RemainingQuantity);
        var legacyRemaining = orderedBalances.Sum(b => b.Quantity);
        if (proposedRemaining != legacyRemaining)
        {
            return Inconsistent(
                scopeKey,
                $"Quantity mismatch for scope '{scopeKey.BrgId}' / '{scopeKey.ReceiptSourceId}': " +
                $"proposed Remaining Quantity {proposedRemaining} does not equal legacy authority {legacyRemaining}.");
        }

        if (proposedLayers.Count == 0)
        {
            return ReconstructionBaselineCalculationResult.Balanced(
                scopeKey,
                establishingMovement: null,
                proposedPositions: Array.Empty<StockPositionModel>(),
                usedDeterministicOrderingFallback: usedFallbackOrdering);
        }

        var movementId = BuildMovementId(scopeKey);
        var sourceRef = SourceTransactionReferenceType.Create(
            $"RECON|{scopeKey.BrgId}|{scopeKey.ReceiptSourceId}|baseline");
        var movementKey = StockMovementModel.Key(movementId);

        var lines = new List<StockMovementLineType>();
        var lineNo = 0;
        var layersByLocation = new SortedDictionary<string, List<StockLayerModel>>(StringComparer.Ordinal);
        var bindings = new List<StockLayerLegacyBindingType>();
        var boundAt = DateTime.Now;

        foreach (var draft in proposedLayers
                     .OrderBy(l => l.LayananId, StringComparer.Ordinal)
                     .ThenBy(l => l.Sequence))
        {
            lineNo++;
            var layerId = BuildLayerId(scopeKey, draft.LayananId, draft.Sequence);
            var location = LayananType.Key(draft.LayananId);
            var valuation = UnitValuationType.Create(draft.UnitCost);

            var layer = StockLayerModel.Create(
                item,
                receiptSource,
                location,
                movementKey,
                draft.InitialQuantity,
                valuation,
                draft.EffectiveReceiptTime,
                StockFactOriginEnum.Reconstructed,
                draft.ExpirationDate,
                draft.Batch,
                stockLayerId: layerId,
                remainingQuantity: draft.RemainingQuantity);

            if (!layersByLocation.TryGetValue(draft.LayananId, out var list))
            {
                list = [];
                layersByLocation[draft.LayananId] = list;
            }

            list.Add(layer);

            if (!string.IsNullOrWhiteSpace(draft.LegacyRowId))
            {
                bindings.Add(StockLayerLegacyBindingType.FromLayer(
                    layer,
                    draft.LegacyRowId!,
                    boundAt));
            }

            lines.Add(StockMovementLineType.Create(
                lineNo,
                item,
                receiptSource,
                location,
                StockMovementDirectionEnum.Inbound,
                draft.InitialQuantity,
                valuation,
                StockFactOriginEnum.Reconstructed,
                StockLayerModel.Key(layerId)));
        }

        var effectiveBusinessTime = proposedLayers.Max(l => l.EffectiveReceiptTime);
        var movement = StockMovementModel.CreateReceipt(
            sourceRef,
            effectiveBusinessTime,
            lines,
            StockFactOriginEnum.Reconstructed,
            movementId);

        var positions = layersByLocation
            .Select(kvp => StockPositionModel.Create(
                StockWriteScopeKeyType.Create(scopeKey.BrgId, scopeKey.ReceiptSourceId, kvp.Key),
                kvp.Value,
                version: 0))
            .ToArray();

        return ReconstructionBaselineCalculationResult.Balanced(
            scopeKey,
            movement,
            positions,
            usedFallbackOrdering,
            bindings);
    }

    private static ReconstructionBaselineCalculationResult Inconsistent(
        StockLedgerScopeKeyType scopeKey,
        string reason)
        => ReconstructionBaselineCalculationResult.Inconsistent(scopeKey, reason);

    private static bool TryFindScopeMismatch(
        StockLedgerScopeKeyType scopeKey,
        IReadOnlyList<LegacyStockBalanceType> balances,
        IReadOnlyList<LegacyStockJournalEntryType> journals,
        out string reason)
    {
        foreach (var balance in balances)
        {
            if (!string.Equals(balance.BrgId, scopeKey.BrgId, StringComparison.Ordinal)
                || !string.Equals(balance.ReceiptSourceId, scopeKey.ReceiptSourceId, StringComparison.Ordinal))
            {
                reason =
                    $"Snapshot balance row is outside reconstruction scope '{scopeKey.BrgId}' / '{scopeKey.ReceiptSourceId}' " +
                    $"(found '{balance.BrgId}' / '{balance.ReceiptSourceId}').";
                return true;
            }
        }

        foreach (var journal in journals)
        {
            if (!string.Equals(journal.BrgId, scopeKey.BrgId, StringComparison.Ordinal)
                || !string.Equals(journal.ReceiptSourceId, scopeKey.ReceiptSourceId, StringComparison.Ordinal))
            {
                reason =
                    $"Snapshot journal row is outside reconstruction scope '{scopeKey.BrgId}' / '{scopeKey.ReceiptSourceId}' " +
                    $"(found '{journal.BrgId}' / '{journal.ReceiptSourceId}').";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryFindInvalidBalanceQuantity(
        IReadOnlyList<LegacyStockBalanceType> balances,
        out string reason)
    {
        foreach (var balance in balances)
        {
            if (balance.Quantity <= 0m)
            {
                reason =
                    $"Surviving legacy balance at location '{balance.LayananId}' has non-positive quantity {balance.Quantity}; " +
                    "zero-balance legacy rows are expected to be absent and negative quantity is irreconcilable.";
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    private static DateTime ResolveReceiptTime(
        LegacyStockBalanceType balance,
        JournalAggregate? journalAgg)
    {
        if (balance.ReceiptTime is { } receiptTime && receiptTime != default)
            return receiptTime;

        if (journalAgg is not null)
            return journalAgg.EarliestMutationTime;

        if (balance.LastMutationTime is { } lastMutation && lastMutation != default)
            return lastMutation;

        return UnknownReconstructedReceiptTime;
    }

    private static List<LegacyStockBalanceType> NormalizeBalances(
        IReadOnlyList<LegacyStockBalanceType> balances)
        => balances
            .OrderBy(b => b.LayananId, StringComparer.Ordinal)
            .ThenBy(b => b.LegacyRowId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(b => b.BrgId, StringComparer.Ordinal)
            .ThenBy(b => b.ReceiptSourceId, StringComparer.Ordinal)
            .ThenBy(b => b.Quantity)
            .ThenBy(b => b.UnitCost)
            .ToList();

    private static List<LegacyStockJournalEntryType> NormalizeJournals(
        IReadOnlyList<LegacyStockJournalEntryType> journals)
        => journals
            .OrderBy(j => j.MutationTime)
            .ThenBy(j => j.LegacyJournalId, StringComparer.Ordinal)
            .ThenBy(j => j.LayananId, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Deterministic Movement id fitting <c>BILRG_StokMovement.StockMovementId VARCHAR(26)</c>.
    /// </summary>
    public static string BuildMovementId(IStockLedgerScopeKey scope)
        => DeterministicAccountableId($"RBL|{scope.BrgId}|{scope.ReceiptSourceId}|MOV");

    /// <summary>
    /// Deterministic Layer id fitting <c>BILRG_StokLayer.StockLayerId VARCHAR(26)</c>.
    /// </summary>
    public static string BuildLayerId(IStockLedgerScopeKey scope, string layananId, int sequence)
        => DeterministicAccountableId(
            $"RBL|{scope.BrgId}|{scope.ReceiptSourceId}|{layananId}|{sequence:D4}");

    /// <summary>
    /// SHA-256 truncated hex (26 chars) — durable identity seed that fits ULID-width columns
    /// while remaining deterministic for equivalent reconstruction inputs (BR-STL-070/071).
    /// </summary>
    public static string DeterministicAccountableId(string seed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(seed);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(digest.AsSpan(0, 13));
    }

    private static string FormatEd(DateOnly? expirationDate)
        => expirationDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "(none)";

    private readonly record struct ProvenanceKey(string LayananId, decimal UnitCost, DateOnly? ExpirationDate)
    {
        public static ProvenanceKey FromBalance(LegacyStockBalanceType balance)
            => new(balance.LayananId, balance.UnitCost, balance.ExpirationDate);

        public static ProvenanceKey FromJournal(LegacyStockJournalEntryType journal)
            => new(journal.LayananId, journal.UnitCost, journal.ExpirationDate);
    }

    private sealed class JournalAggregate
    {
        public decimal QuantityIn { get; private init; }
        public decimal QuantityOut { get; private init; }
        public DateTime EarliestMutationTime { get; private init; }
        public string? Batch { get; private init; }

        public static JournalAggregate From(IEnumerable<LegacyStockJournalEntryType> rows)
        {
            var list = rows
                .OrderBy(j => j.MutationTime)
                .ThenBy(j => j.LegacyJournalId, StringComparer.Ordinal)
                .ToList();

            return new JournalAggregate
            {
                QuantityIn = list.Sum(j => j.QuantityIn),
                QuantityOut = list.Sum(j => j.QuantityOut),
                EarliestMutationTime = list[0].MutationTime,
                Batch = list.Select(j => j.Batch).FirstOrDefault(b => !string.IsNullOrWhiteSpace(b))
            };
        }
    }

    private sealed record ProposedLayerDraft(
        int Sequence,
        string LayananId,
        decimal InitialQuantity,
        decimal RemainingQuantity,
        decimal UnitCost,
        DateOnly? ExpirationDate,
        string? Batch,
        DateTime EffectiveReceiptTime,
        bool IsDepleted,
        string? LegacyRowId);
}

/// <summary>
/// Outcome of P2-S4 baseline calculation. Suitable for Phase C persist when Balanced,
/// or for Scope <c>Inconsistent</c> marking when not. Not a persistence command.
/// </summary>
public enum ReconstructionBaselineOutcomeEnum
{
    Balanced = 1,
    Inconsistent = 2
}

/// <summary>
/// Proposed reconstructed baseline payload produced by
/// <see cref="LegacyReconstructionBaselineCalculator"/>.
/// </summary>
public sealed record ReconstructionBaselineCalculationResult
{
    private ReconstructionBaselineCalculationResult(
        ReconstructionBaselineOutcomeEnum outcome,
        StockLedgerScopeKeyType scopeKey,
        string? inconsistencyReason,
        StockMovementModel? establishingMovement,
        IReadOnlyList<StockPositionModel> proposedPositions,
        bool usedDeterministicOrderingFallback,
        IReadOnlyList<StockLayerLegacyBindingType> proposedBindings)
    {
        Outcome = outcome;
        ScopeKey = scopeKey;
        InconsistencyReason = inconsistencyReason;
        EstablishingMovement = establishingMovement;
        ProposedPositions = proposedPositions;
        UsedDeterministicOrderingFallback = usedDeterministicOrderingFallback;
        ProposedBindings = proposedBindings;
    }

    public ReconstructionBaselineOutcomeEnum Outcome { get; }
    public StockLedgerScopeKeyType ScopeKey { get; }
    public string? InconsistencyReason { get; }
    /// <summary>
    /// Single establishing Receipt movement with <see cref="StockFactOriginEnum.Reconstructed"/>,
    /// or null when Balanced with no layers / when Inconsistent.
    /// </summary>
    public StockMovementModel? EstablishingMovement { get; }
    public IReadOnlyList<StockPositionModel> ProposedPositions { get; }
    /// <summary>
    /// True when calculation used a deterministic fallback (e.g. missing receipt time or
    /// balance-only Initial) that does not change material quantity/provenance outcomes (BR-STL-104).
    /// </summary>
    public bool UsedDeterministicOrderingFallback { get; }
    /// <summary>
    /// P5-S3 — coexistence bindings for layers established from surviving <c>tb_stok</c> rows.
    /// </summary>
    public IReadOnlyList<StockLayerLegacyBindingType> ProposedBindings { get; }

    public bool IsBalanced => Outcome == ReconstructionBaselineOutcomeEnum.Balanced;
    public bool IsInconsistent => Outcome == ReconstructionBaselineOutcomeEnum.Inconsistent;

    public decimal ProposedRemainingQuantity
        => ProposedPositions.Sum(p => p.TotalRemainingQuantity);

    public static ReconstructionBaselineCalculationResult Balanced(
        StockLedgerScopeKeyType scopeKey,
        StockMovementModel? establishingMovement,
        IReadOnlyList<StockPositionModel> proposedPositions,
        bool usedDeterministicOrderingFallback,
        IReadOnlyList<StockLayerLegacyBindingType>? proposedBindings = null)
        => new(
            ReconstructionBaselineOutcomeEnum.Balanced,
            scopeKey,
            inconsistencyReason: null,
            establishingMovement,
            proposedPositions,
            usedDeterministicOrderingFallback,
            proposedBindings ?? Array.Empty<StockLayerLegacyBindingType>());

    public static ReconstructionBaselineCalculationResult Inconsistent(
        StockLedgerScopeKeyType scopeKey,
        string reason)
        => new(
            ReconstructionBaselineOutcomeEnum.Inconsistent,
            scopeKey,
            reason,
            establishingMovement: null,
            proposedPositions: Array.Empty<StockPositionModel>(),
            usedDeterministicOrderingFallback: false,
            proposedBindings: Array.Empty<StockLayerLegacyBindingType>());
}
