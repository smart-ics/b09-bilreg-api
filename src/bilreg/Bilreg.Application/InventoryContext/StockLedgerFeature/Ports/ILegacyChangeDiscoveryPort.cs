using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;

/// <summary>
/// G-13 — Incremental Legacy Movement Discovery contract.
/// Selected mechanism (ADR-stock-ledger-legacy-change-discovery): deterministic history fingerprint
/// + bounded delta/replay against authoritative <c>tb_buku</c> / <c>tb_stok</c>.
/// On fingerprint mismatch, discovery MUST set-diff Ledger-known journal identities and/or
/// perform scoped re-derive — hash drift alone is not a delete event stream.
/// <para>
/// Phase ownership: Phase 3. Do not hard-code watermark-alone or <c>fs_kd_trs</c>-alone cursors.
/// This port does not implement Freshness Gate orchestration and must not advance
/// Synchronization Position (G-14/G-15 catch-up owns position advancement).
/// </para>
/// </summary>
public interface ILegacyChangeDiscoveryPort
{
    /// <summary>
    /// Computes the current authority fingerprint for the Reconstruction Scope.
    /// Returned position remains mechanism-neutral opaque bytes + algorithm version.
    /// </summary>
    SynchronizationPositionType ComputeCurrentFingerprint(IStockLedgerScopeKey scope);

    /// <summary>
    /// Compares the stored Synchronization Position with current authority and,
    /// on mismatch, returns set-diff / bounded-replay discovery outcomes.
    /// </summary>
    LegacyChangeDiscoveryResult DiscoverChanges(
        IStockLedgerScopeKey scope,
        SynchronizationPositionType? storedPosition);
}

public enum LegacyChangeDiscoveryOutcomeEnum
{
    Unchanged = 1,
    ChangesDetected = 2,
    Undeterminable = 3
}

/// <summary>
/// Kinds of material delta discovery may surface after fingerprint mismatch.
/// Empirical detection of all mutation kinds remains Phase 3 / G-13 acceptance work.
/// </summary>
public enum LegacyDiscoveredDeltaKindEnum
{
    JournalInsert = 1,
    JournalUpdate = 2,
    JournalVoidDelete = 3,
    BalanceDelete = 4,
    BalanceUpdate = 5,
    RequiresScopedReDerive = 6
}

public sealed record LegacyChangeDiscoveryResult(
    LegacyChangeDiscoveryOutcomeEnum Outcome,
    SynchronizationPositionType? CurrentFingerprint,
    IReadOnlyList<LegacyDiscoveredDeltaType> Deltas,
    string? Explanation);

public sealed record LegacyDiscoveredDeltaType(
    LegacyDiscoveredDeltaKindEnum Kind,
    string? LegacyJournalId,
    string? LayananId,
    decimal? QuantityIn,
    decimal? QuantityOut,
    DateTime? MutationTime,
    string? Explanation = null);
