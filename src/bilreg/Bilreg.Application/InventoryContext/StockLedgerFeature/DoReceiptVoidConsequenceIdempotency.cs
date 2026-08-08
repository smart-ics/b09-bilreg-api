using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P4-S4 — SourceConsequence idempotency key for Native DO Receipt void.
/// Distinct from post key (<c>DO|…</c>) and Phase 3 <c>SYNC|…</c> keys.
/// </summary>
public static class DoReceiptVoidConsequenceIdempotency
{
    public static string BuildSourceConsequenceKey(
        IStockLedgerScopeKey scope,
        string voidSourceTransactionId)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (string.IsNullOrWhiteSpace(scope.BrgId))
            throw new ArgumentException("BrgId is required.", nameof(scope));
        if (string.IsNullOrWhiteSpace(scope.ReceiptSourceId))
            throw new ArgumentException("ReceiptSourceId is required.", nameof(scope));
        if (string.IsNullOrWhiteSpace(voidSourceTransactionId))
            throw new ArgumentException("VoidSourceTransactionId is required.", nameof(voidSourceTransactionId));

        return $"DO_VOID|{scope.BrgId.Trim()}|{scope.ReceiptSourceId.Trim()}|{voidSourceTransactionId.Trim()}";
    }
}
