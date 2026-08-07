using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;

/// <summary>
/// G-11 — Legacy Compatibility Writer contract.
/// Explicit adapter for authoritative legacy-compatible writebacks
/// (<c>tb_stok</c> insert/update/delete, <c>tb_buku</c> movement/void shapes, and required FO writebacks)
/// enlisted in the Stock Ledger consequence transaction.
/// <para>
/// Phase ownership: live writer is Phase 4+. P1-S8 injects a Test fake only —
/// Phase 1 must not register a production adapter that mutates legacy stock.
/// </para>
/// Does not transfer runtime authority; Stage B authority remains <c>tb_stok</c> + <c>tb_buku</c>.
/// </summary>
public interface ILegacyCompatibilityWriterPort
{
    /// <summary>
    /// Applies legacy-compatible stock consequences for one Stock Ledger consequence.
    /// Implementations must enlist in the caller's ambient SQL transaction when present.
    /// Failure must leave no partial durable legacy (or Ledger) consequence.
    /// </summary>
    void Apply(LegacyCompatibilityWriteRequest request);
}

/// <summary>
/// One native consequence's legacy writeback payload.
/// Shapes are intentionally explicit so later FO families can extend without inventing authority fields.
/// </summary>
public sealed record LegacyCompatibilityWriteRequest(
    ISourceTransactionReferenceKey SourceTransaction,
    StockMovementKindEnum MovementKind,
    IStockLedgerScopeKey? PrimaryScope,
    IReadOnlyList<LegacyCompatibilityBalanceMutationType> BalanceMutations,
    IReadOnlyList<LegacyCompatibilityJournalEntryType> JournalEntries);

public enum LegacyBalanceMutationActionEnum
{
    Upsert = 1,
    Delete = 2
}

/// <summary>
/// Intended mutation against a legacy balance row (<c>tb_stok</c> compatibility shape).
/// Delete-on-zero remains a compatibility concern for the live adapter (Phase 4+).
/// </summary>
public sealed record LegacyCompatibilityBalanceMutationType(
    LegacyBalanceMutationActionEnum Action,
    string BrgId,
    string ReceiptSourceId,
    string LayananId,
    decimal Quantity,
    decimal UnitCost,
    DateOnly? ExpirationDate,
    string? Batch,
    string? PurchaseOrderId,
    string? LegacyRowId);

/// <summary>
/// Intended journal write or void against legacy movement history (<c>tb_buku</c> compatibility shape).
/// </summary>
public sealed record LegacyCompatibilityJournalEntryType(
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
    bool IsVoid = false);
