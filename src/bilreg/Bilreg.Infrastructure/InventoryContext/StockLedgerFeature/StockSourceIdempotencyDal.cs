using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStockSourceIdempotencyDal :
    IInsert<StockSourceIdempotencyDto>,
    IGetData<StockSourceIdempotencyDto, IStockSourceIdempotencyKey>
{
    StockSourceIdempotencyDto GetDataByBusinessKey(IStockSourceIdempotencyBusinessKey key);

    /// <summary>
    /// P3-S1 — Ledger-known legacy identity keys for one Reconstruction Scope (SyncBatch + SourceConsequence).
    /// </summary>
    IReadOnlyList<string> ListSyncIdentityKeysForScope(string brgId, string receiptSourceId);

    /// <summary>
    /// P3-S4 — Full SyncBatch / SourceConsequence identity rows for one Reconstruction Scope.
    /// </summary>
    IReadOnlyList<StockSourceIdempotencyDto> ListSyncIdentityRecordsForScope(string brgId, string receiptSourceId);
}

public class StockSourceIdempotencyDal : IStockSourceIdempotencyDal
{
    private readonly DatabaseOptions _opt;

    public StockSourceIdempotencyDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StockSourceIdempotencyDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokSourceIdempotency (
                IdempotencyId, IdempotencyKind, IdempotencyKey,
                SourceTransactionId, StockMovementId,
                BrgId, ReceiptSourceId, ProcessedAt,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @IdempotencyId, @IdempotencyKind, @IdempotencyKey,
                @SourceTransactionId, @StockMovementId,
                @BrgId, @ReceiptSourceId, @ProcessedAt,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public StockSourceIdempotencyDto GetData(IStockSourceIdempotencyKey key)
    {
        const string sql = """
            SELECT
                aa.IdempotencyId, aa.IdempotencyKind, aa.IdempotencyKey,
                aa.SourceTransactionId, aa.StockMovementId,
                aa.BrgId, aa.ReceiptSourceId, aa.ProcessedAt,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokSourceIdempotency aa
            WHERE aa.IdempotencyId = @IdempotencyId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IdempotencyId", key.IdempotencyId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockSourceIdempotencyDto>(sql, dp);
    }

    public StockSourceIdempotencyDto GetDataByBusinessKey(IStockSourceIdempotencyBusinessKey key)
    {
        const string sql = """
            SELECT
                aa.IdempotencyId, aa.IdempotencyKind, aa.IdempotencyKey,
                aa.SourceTransactionId, aa.StockMovementId,
                aa.BrgId, aa.ReceiptSourceId, aa.ProcessedAt,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokSourceIdempotency aa
            WHERE
                aa.IdempotencyKind = @IdempotencyKind
                AND aa.IdempotencyKey = @IdempotencyKey
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IdempotencyKind", (int)key.IdempotencyKind, SqlDbType.Int);
        dp.AddParam("@IdempotencyKey", key.IdempotencyKey, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockSourceIdempotencyDto>(sql, dp);
    }

    public IReadOnlyList<string> ListSyncIdentityKeysForScope(string brgId, string receiptSourceId)
    {
        const string sql = """
            SELECT aa.IdempotencyKey
            FROM BILRG_StokSourceIdempotency aa
            WHERE
                aa.BrgId = @BrgId
                AND aa.ReceiptSourceId = @ReceiptSourceId
                AND aa.IdempotencyKind IN (@SourceConsequence, @SyncBatch)
                AND aa.IdempotencyKey LIKE 'SYNC|%'
            ORDER BY aa.IdempotencyKey
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", receiptSourceId, SqlDbType.VarChar);
        dp.AddParam("@SourceConsequence", (int)StockSourceIdempotencyKindEnum.SourceConsequence, SqlDbType.Int);
        dp.AddParam("@SyncBatch", (int)StockSourceIdempotencyKindEnum.SyncBatch, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<SyncIdentityKeyRow>(sql, dp) ?? [];
        return rows.Select(x => x.IdempotencyKey).ToList();
    }

    public IReadOnlyList<StockSourceIdempotencyDto> ListSyncIdentityRecordsForScope(
        string brgId,
        string receiptSourceId)
    {
        const string sql = """
            SELECT
                aa.IdempotencyId, aa.IdempotencyKind, aa.IdempotencyKey,
                aa.SourceTransactionId, aa.StockMovementId,
                aa.BrgId, aa.ReceiptSourceId, aa.ProcessedAt,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokSourceIdempotency aa
            WHERE
                aa.BrgId = @BrgId
                AND aa.ReceiptSourceId = @ReceiptSourceId
                AND aa.IdempotencyKind IN (@SourceConsequence, @SyncBatch)
                AND aa.IdempotencyKey LIKE 'SYNC|%'
            ORDER BY aa.IdempotencyKey
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", receiptSourceId, SqlDbType.VarChar);
        dp.AddParam("@SourceConsequence", (int)StockSourceIdempotencyKindEnum.SourceConsequence, SqlDbType.Int);
        dp.AddParam("@SyncBatch", (int)StockSourceIdempotencyKindEnum.SyncBatch, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return (conn.Read<StockSourceIdempotencyDto>(sql, dp) ?? []).ToList();
    }

    private sealed record SyncIdentityKeyRow(string IdempotencyKey);

    private static DynamicParameters MapParams(StockSourceIdempotencyDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@IdempotencyId", dto.IdempotencyId, SqlDbType.VarChar);
        dp.AddParam("@IdempotencyKind", dto.IdempotencyKind, SqlDbType.Int);
        dp.AddParam("@IdempotencyKey", dto.IdempotencyKey, SqlDbType.VarChar);
        dp.AddParam("@SourceTransactionId", dto.SourceTransactionId, SqlDbType.VarChar);
        dp.AddParam("@StockMovementId", dto.StockMovementId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", dto.ReceiptSourceId, SqlDbType.VarChar);
        dp.AddParam("@ProcessedAt", dto.ProcessedAt, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
