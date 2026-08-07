using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;

/// <summary>
/// G-05 — Legacy reconstruction read adapter contract.
/// Parameterized reads of authoritative legacy stock (<c>tb_stok</c>) and journal
/// (<c>tb_buku</c>) facts by Item + Receipt Source across all Stock Locations.
/// <para>
/// Live adapter: Phase 2 (<c>LegacyStockReadPort</c>). Phase 1 defined the contract only.
/// Does not implement reconstruction orchestration, fingerprint, or Freshness Gate.
/// </para>
/// Does not mutate legacy tables and does not encode runtime authority transfer.
/// </summary>
public interface ILegacyStockReadPort
{
    /// <summary>
    /// Surviving current legacy balance rows for the Reconstruction Scope
    /// (zero-quantity <c>tb_stok</c> rows are absent in legacy).
    /// </summary>
    IReadOnlyList<LegacyStockBalanceType> ListCurrentBalances(IStockLedgerScopeKey scope);

    /// <summary>
    /// Legacy journal rows for the Reconstruction Scope across all Stock Locations,
    /// including historical rows required for provenance, in deterministic input order.
    /// </summary>
    IReadOnlyList<LegacyStockJournalEntryType> ListJournalEntries(IStockLedgerScopeKey scope);
}

/// <summary>
/// Application-facing projection of one surviving legacy balance row (<c>tb_stok</c> shape).
/// Not a Domain aggregate and not an authority claim.
/// </summary>
public sealed record LegacyStockBalanceType(
    string BrgId,
    string ReceiptSourceId,
    string LayananId,
    decimal Quantity,
    decimal UnitCost,
    DateOnly? ExpirationDate,
    string? Batch,
    string? PurchaseOrderId,
    string? LegacyRowId,
    DateTime? ReceiptTime,
    DateTime? LastMutationTime);

/// <summary>
/// Application-facing projection of one legacy journal row (<c>tb_buku</c> shape).
/// Not a Domain aggregate and not an authority claim.
/// </summary>
public sealed record LegacyStockJournalEntryType(
    string LegacyJournalId,
    string BrgId,
    string ReceiptSourceId,
    string LayananId,
    decimal QuantityIn,
    decimal QuantityOut,
    decimal UnitCost,
    DateOnly? ExpirationDate,
    string? Batch,
    string MutationKindId,
    string MutationTransactionId,
    DateTime MutationTime,
    string? PurchaseOrderId);
