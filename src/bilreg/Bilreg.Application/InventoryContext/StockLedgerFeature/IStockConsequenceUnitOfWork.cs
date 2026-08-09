using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// G-18 foundation — short Stock Ledger consequence transaction boundary.
/// Commits Movement, Layer/Position, Scope coexistence state, and source idempotency
/// in one ambient SQL transaction, optionally invoking
/// <see cref="ILegacyCompatibilityWriterPort"/> before commit.
/// <para>
/// Phase 1: Ledger-side atomicity only. Live legacy writer is Phase 4+;
/// tests inject a throw-capable fake. No production stock write endpoint.
/// </para>
/// <para>
/// P2-S6: Reconstruction baseline commits reuse this UoW with
/// <see cref="StockSourceIdempotencyKindEnum.ReconstructionBaseline"/> and optional
/// conditional Scope status update (Phase C claim revalidation).
/// </para>
/// <para>
/// P3-S4: Sync catch-up uses <see cref="StockSourceIdempotencyKindEnum.SyncBatch"/>;
/// quantity-neutral bootstrap / omission intents use
/// <see cref="CommitSyncEvidence"/>.
/// </para>
/// </summary>
public interface IStockConsequenceUnitOfWork
{
    /// <summary>
    /// Persists one Stock Ledger consequence draft atomically.
    /// Duplicate source idempotency keys return
    /// <see cref="StockConsequenceCommitOutcomeEnum.AlreadyCommitted"/> without re-writing.
    /// </summary>
    StockConsequenceCommitResult Commit(StockConsequenceDraft draft);

    /// <summary>
    /// P3-S4 — persists SyncBatch idempotency evidence without a Movement
    /// (identity bootstrap / representational omission). Duplicate keys are quantity-neutral.
    /// </summary>
    StockConsequenceCommitResult CommitSyncEvidence(StockSyncEvidenceDraft draft);
}

/// <summary>
/// Prepared Ledger-side consequence pieces ready for one short UoW commit.
/// Caller owns domain construction (receipt/FIFO/etc.); this draft is persistence coordination only.
/// </summary>
public sealed record StockConsequenceDraft(
    string IdempotencyKey,
    DateTime ProcessedAt,
    StockMovementModel Movement,
    IReadOnlyList<StockPositionModel> Positions,
    StockLedgerScopeStateModel? ScopeState,
    LegacyCompatibilityWriteRequest? LegacyWrite,
    StockSourceIdempotencyKindEnum IdempotencyKind = StockSourceIdempotencyKindEnum.SourceConsequence,
    ReconstructionStatusEnum? ExpectedPriorReconstructionStatus = null,
    IReadOnlyList<StockLedgerScopeStateModel>? AdditionalScopeStates = null,
    IReadOnlyList<StockLayerLegacyBindingType>? LayerLegacyBindings = null);

/// <summary>
/// P3-S4 — movement-less SyncBatch evidence (bootstrap identity keys / omission).
/// </summary>
public sealed record StockSyncEvidenceDraft(
    string IdempotencyKey,
    DateTime ProcessedAt,
    string BrgId,
    string ReceiptSourceId,
    string? StockMovementId = null,
    string? SourceTransactionId = null,
    StockLedgerScopeStateModel? ScopeState = null,
    SynchronizationStateEnum? ExpectedPriorSynchronizationState = null);

public sealed record StockConsequenceCommitResult(
    StockConsequenceCommitOutcomeEnum Outcome,
    string IdempotencyKey,
    string StockMovementId,
    string IdempotencyId);

public enum StockConsequenceCommitOutcomeEnum
{
    Committed = 1,
    AlreadyCommitted = 2
}
