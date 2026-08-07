using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S8 — Scope-scoped delete of additive reconstruction output.
/// Never issues DML against <c>tb_stok</c> / <c>tb_buku</c>.
/// </summary>
public sealed class IncompleteReconstructionRecoveryPort : IIncompleteReconstructionRecoveryPort
{
    private readonly DatabaseOptions _opt;

    public IncompleteReconstructionRecoveryPort(IOptions<DatabaseOptions> opt)
        => _opt = opt.Value;

    public int DeleteAdditiveOutput(
        IStockLedgerScopeKey scope,
        string movementId,
        string idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(movementId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        const string sql = """
            DELETE FROM BILRG_StokMovementLine WHERE StockMovementId = @MovementId;
            DELETE FROM BILRG_StokMovement WHERE StockMovementId = @MovementId;
            DELETE FROM BILRG_StokLayer WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            DELETE FROM BILRG_StokPosition WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            DELETE FROM BILRG_StokLedgerScope WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId;
            DELETE FROM BILRG_StokSourceIdempotency
            WHERE IdempotencyKind = @Kind AND IdempotencyKey = @IdempotencyKey;
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        return conn.Execute(
            sql,
            new
            {
                MovementId = movementId,
                scope.BrgId,
                scope.ReceiptSourceId,
                Kind = (int)StockSourceIdempotencyKindEnum.ReconstructionBaseline,
                IdempotencyKey = idempotencyKey
            });
    }
}
