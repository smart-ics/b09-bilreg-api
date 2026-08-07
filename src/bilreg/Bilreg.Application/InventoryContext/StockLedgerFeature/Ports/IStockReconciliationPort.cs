using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;

/// <summary>
/// G-16 — Synchronization reconciliation and drift classification contract.
/// Reconciles Item + Receipt Source across all Stock Locations and classifies
/// intentional representational differences, pending synchronization, provenance limitation,
/// and material inconsistency.
/// <para>
/// Example intentional difference: deleted zero <c>tb_stok</c> row vs retained depleted Stock Layer.
/// Must not resolve differences by silently rewriting completed Stock Movements.
/// Does not transfer runtime authority.
/// </para>
/// Phase ownership: material drift classification in Phase 3; operational reporting views in Phase 8.
/// Phase 1 defines the contract only.
/// </summary>
public interface IStockReconciliationPort
{
    StockReconciliationResult Reconcile(IStockLedgerScopeKey scope);
}

public enum StockReconciliationOutcomeEnum
{
    Balanced = 1,
    IntentionalDifference = 2,
    PendingSynchronization = 3,
    ProvenanceLimitation = 4,
    MaterialInconsistency = 5
}

public enum StockReconciliationDifferenceKindEnum
{
    DepletedLayerVsAbsentLegacyRow = 1,
    QuantityMismatch = 2,
    MissingLegacyJournal = 3,
    MissingLedgerMovement = 4,
    ValuationMismatch = 5,
    Other = 9
}

public sealed record StockReconciliationResult(
    StockReconciliationOutcomeEnum Outcome,
    decimal? LegacyRemainingQuantity,
    decimal? LedgerRemainingQuantity,
    decimal? DifferenceQuantity,
    IReadOnlyList<StockReconciliationDifferenceType> Differences,
    string? Explanation);

public sealed record StockReconciliationDifferenceType(
    StockReconciliationDifferenceKindEnum Kind,
    string? LayananId,
    string? Explanation);
