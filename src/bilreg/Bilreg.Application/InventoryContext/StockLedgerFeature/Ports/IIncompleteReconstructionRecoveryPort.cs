using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;

/// <summary>
/// P2-S8 — Controlled recovery store for incomplete additive reconstruction output.
/// Deletes scope-scoped <c>BILRG_*</c> rows only; never touches legacy authority
/// (<c>tb_stok</c> / <c>tb_buku</c>). Disposable/test DB drills only.
/// </summary>
public interface IIncompleteReconstructionRecoveryPort
{
    /// <summary>
    /// Deletes additive Ledger reconstruction output for one Item + Receipt Source.
    /// Returns how many durable additive rows were removed (0 = nothing to recover).
    /// </summary>
    int DeleteAdditiveOutput(IStockLedgerScopeKey scope, string movementId, string idempotencyKey);
}
