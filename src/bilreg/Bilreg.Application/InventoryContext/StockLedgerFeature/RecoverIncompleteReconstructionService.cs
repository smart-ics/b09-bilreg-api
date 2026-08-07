using Ardalis.GuardClauses;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.Shared;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S8 — Controlled recovery for incomplete (or terminal) additive reconstruction output.
/// Deletes scope-scoped Ledger rows in a short TX without touching legacy authority.
/// After recovery, the next Phase A claim recreates Scope from scratch.
/// Safe no-op when no additive rows exist for the scope.
/// </summary>
public sealed class RecoverIncompleteReconstructionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIncompleteReconstructionRecoveryPort _recoveryPort;

    public RecoverIncompleteReconstructionService(
        IUnitOfWork unitOfWork,
        IIncompleteReconstructionRecoveryPort recoveryPort)
    {
        _unitOfWork = unitOfWork;
        _recoveryPort = recoveryPort;
    }

    /// <summary>
    /// Removes Movement/Line/Layer/Position/Scope/Idempotency rows for the reconstruction scope.
    /// Does not rewrite <c>tb_stok</c> or <c>tb_buku</c>.
    /// </summary>
    public RecoverIncompleteReconstructionResult Recover(IStockLedgerScopeKey scope)
    {
        Guard.Against.Null(scope, nameof(scope));
        Guard.Against.NullOrWhiteSpace(scope.BrgId, nameof(scope.BrgId));
        Guard.Against.NullOrWhiteSpace(scope.ReceiptSourceId, nameof(scope.ReceiptSourceId));

        var key = StockLedgerScopeKeyType.Create(scope.BrgId, scope.ReceiptSourceId);
        var movementId = LegacyReconstructionBaselineCalculator.BuildMovementId(key);
        var idempotencyKey = BuildReconstructionIdempotencyKey(key);

        using var tx = _unitOfWork.Begin();
        var deleted = _recoveryPort.DeleteAdditiveOutput(key, movementId, idempotencyKey);
        tx.Complete();

        return deleted > 0
            ? RecoverIncompleteReconstructionResult.Recovered(deleted)
            : RecoverIncompleteReconstructionResult.NoOp();
    }

    /// <summary>
    /// Same key shape as <c>ReconstructStockLedgerBaselineHandler</c> reconstruction idempotency.
    /// </summary>
    public static string BuildReconstructionIdempotencyKey(IStockLedgerScopeKey scopeKey)
        => $"RECON|{scopeKey.BrgId}|{scopeKey.ReceiptSourceId}|baseline";
}

public enum RecoverIncompleteReconstructionOutcomeEnum
{
    /// <summary>One or more additive Ledger rows were deleted for the scope.</summary>
    Recovered = 1,

    /// <summary>No additive reconstruction output existed; durable state unchanged.</summary>
    NoOp = 2
}

public sealed record RecoverIncompleteReconstructionResult(
    RecoverIncompleteReconstructionOutcomeEnum Outcome,
    int DeletedRowCount)
{
    public static RecoverIncompleteReconstructionResult Recovered(int deletedRowCount)
        => new(RecoverIncompleteReconstructionOutcomeEnum.Recovered, deletedRowCount);

    public static RecoverIncompleteReconstructionResult NoOp()
        => new(RecoverIncompleteReconstructionOutcomeEnum.NoOp, DeletedRowCount: 0);
}
