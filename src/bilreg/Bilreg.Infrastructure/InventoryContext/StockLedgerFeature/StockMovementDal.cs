using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStockMovementDal :
    IInsert<StockMovementDto>,
    IGetData<StockMovementDto, IStockMovementKey>
{
}

public class StockMovementDal : IStockMovementDal
{
    private readonly DatabaseOptions _opt;

    public StockMovementDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StockMovementDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokMovement (
                StockMovementId, SourceTransactionId, MovementKind,
                EffectiveBusinessTime, Origin,
                ReversedMovementId, CorrectedMovementId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @StockMovementId, @SourceTransactionId, @MovementKind,
                @EffectiveBusinessTime, @Origin,
                @ReversedMovementId, @CorrectedMovementId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public StockMovementDto GetData(IStockMovementKey key)
    {
        const string sql = """
            SELECT
                aa.StockMovementId, aa.SourceTransactionId, aa.MovementKind,
                aa.EffectiveBusinessTime, aa.Origin,
                aa.ReversedMovementId, aa.CorrectedMovementId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokMovement aa
            WHERE aa.StockMovementId = @StockMovementId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@StockMovementId", key.StockMovementId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockMovementDto>(sql, dp);
    }

    private static DynamicParameters MapParams(StockMovementDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@StockMovementId", dto.StockMovementId, SqlDbType.VarChar);
        dp.AddParam("@SourceTransactionId", dto.SourceTransactionId, SqlDbType.VarChar);
        dp.AddParam("@MovementKind", dto.MovementKind, SqlDbType.Int);
        dp.AddParam("@EffectiveBusinessTime", dto.EffectiveBusinessTime, SqlDbType.DateTime);
        dp.AddParam("@Origin", dto.Origin, SqlDbType.Int);
        dp.AddParam("@ReversedMovementId", dto.ReversedMovementId, SqlDbType.VarChar);
        dp.AddParam("@CorrectedMovementId", dto.CorrectedMovementId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
