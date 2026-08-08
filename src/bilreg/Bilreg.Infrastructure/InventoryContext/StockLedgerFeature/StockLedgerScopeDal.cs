using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStockLedgerScopeDal :
    IInsert<StockLedgerScopeDto>,
    IUpdate<StockLedgerScopeDto>,
    IGetData<StockLedgerScopeDto, IStockLedgerScopeKey>
{
    /// <summary>
    /// P2-S5 — claim-safe update: writes only when durable ReconstructionStatus
    /// equals <paramref name="expectedReconstructionStatus"/>. Returns affected row count.
    /// </summary>
    int UpdateWhenReconstructionStatus(StockLedgerScopeDto dto, int expectedReconstructionStatus);

    /// <summary>
    /// P3-S4 — sync-claim-safe update: writes only when durable SynchronizationState
    /// equals <paramref name="expectedSynchronizationState"/>. Returns affected row count.
    /// </summary>
    int UpdateWhenSynchronizationState(StockLedgerScopeDto dto, int expectedSynchronizationState);
}

public class StockLedgerScopeDal : IStockLedgerScopeDal
{
    private readonly DatabaseOptions _opt;

    public StockLedgerScopeDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StockLedgerScopeDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokLedgerScope (
                BrgId, ReceiptSourceId,
                ReconstructionStatus, SynchronizationState,
                SynchronizationPositionOpaque, AlgorithmVersion,
                ReconstructionBasisVersion, InconsistencyReason,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @BrgId, @ReceiptSourceId,
                @ReconstructionStatus, @SynchronizationState,
                @SynchronizationPositionOpaque, @AlgorithmVersion,
                @ReconstructionBasisVersion, @InconsistencyReason,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public void Update(StockLedgerScopeDto dto)
    {
        const string sql = """
            UPDATE BILRG_StokLedgerScope SET
                ReconstructionStatus = @ReconstructionStatus,
                SynchronizationState = @SynchronizationState,
                SynchronizationPositionOpaque = @SynchronizationPositionOpaque,
                AlgorithmVersion = @AlgorithmVersion,
                ReconstructionBasisVersion = @ReconstructionBasisVersion,
                InconsistencyReason = @InconsistencyReason,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE
                BrgId = @BrgId
                AND ReceiptSourceId = @ReceiptSourceId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public int UpdateWhenReconstructionStatus(StockLedgerScopeDto dto, int expectedReconstructionStatus)
    {
        const string sql = """
            UPDATE BILRG_StokLedgerScope SET
                ReconstructionStatus = @ReconstructionStatus,
                SynchronizationState = @SynchronizationState,
                SynchronizationPositionOpaque = @SynchronizationPositionOpaque,
                AlgorithmVersion = @AlgorithmVersion,
                ReconstructionBasisVersion = @ReconstructionBasisVersion,
                InconsistencyReason = @InconsistencyReason,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE
                BrgId = @BrgId
                AND ReceiptSourceId = @ReceiptSourceId
                AND ReconstructionStatus = @ExpectedReconstructionStatus
            """;

        var dp = MapParams(dto);
        dp.AddParam("@ExpectedReconstructionStatus", expectedReconstructionStatus, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public int UpdateWhenSynchronizationState(StockLedgerScopeDto dto, int expectedSynchronizationState)
    {
        const string sql = """
            UPDATE BILRG_StokLedgerScope SET
                ReconstructionStatus = @ReconstructionStatus,
                SynchronizationState = @SynchronizationState,
                SynchronizationPositionOpaque = @SynchronizationPositionOpaque,
                AlgorithmVersion = @AlgorithmVersion,
                ReconstructionBasisVersion = @ReconstructionBasisVersion,
                InconsistencyReason = @InconsistencyReason,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE
                BrgId = @BrgId
                AND ReceiptSourceId = @ReceiptSourceId
                AND SynchronizationState = @ExpectedSynchronizationState
            """;

        var dp = MapParams(dto);
        dp.AddParam("@ExpectedSynchronizationState", expectedSynchronizationState, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public StockLedgerScopeDto GetData(IStockLedgerScopeKey key)
    {
        const string sql = """
            SELECT
                aa.BrgId, aa.ReceiptSourceId,
                aa.ReconstructionStatus, aa.SynchronizationState,
                aa.SynchronizationPositionOpaque, aa.AlgorithmVersion,
                aa.ReconstructionBasisVersion, aa.InconsistencyReason,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokLedgerScope aa
            WHERE
                aa.BrgId = @BrgId
                AND aa.ReceiptSourceId = @ReceiptSourceId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", key.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", key.ReceiptSourceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockLedgerScopeDto>(sql, dp);
    }

    private static DynamicParameters MapParams(StockLedgerScopeDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", dto.ReceiptSourceId, SqlDbType.VarChar);
        dp.AddParam("@ReconstructionStatus", dto.ReconstructionStatus, SqlDbType.Int);
        dp.AddParam("@SynchronizationState", dto.SynchronizationState, SqlDbType.Int);
        dp.Add("@SynchronizationPositionOpaque", dto.SynchronizationPositionOpaque, DbType.Binary);
        dp.AddParam("@AlgorithmVersion", dto.AlgorithmVersion, SqlDbType.VarChar);
        dp.AddParam("@ReconstructionBasisVersion", dto.ReconstructionBasisVersion, SqlDbType.VarChar);
        dp.AddParam("@InconsistencyReason", dto.InconsistencyReason, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
