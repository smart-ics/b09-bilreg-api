using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStockLayerDal :
    IInsert<StockLayerDto>,
    IUpdate<StockLayerDto>,
    IGetData<StockLayerDto, IStockLayerKey>,
    IListData<StockLayerDto, IStockWriteScopeKey>
{
}

public class StockLayerDal : IStockLayerDal
{
    private readonly DatabaseOptions _opt;

    public StockLayerDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StockLayerDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokLayer (
                StockLayerId, BrgId, ReceiptSourceId, LayananId,
                LayerFormingMovementId, InitialQuantity, RemainingQuantity,
                AmountPerUnit, ExpirationDate, EffectiveReceiptTime,
                Origin, Batch,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @StockLayerId, @BrgId, @ReceiptSourceId, @LayananId,
                @LayerFormingMovementId, @InitialQuantity, @RemainingQuantity,
                @AmountPerUnit, @ExpirationDate, @EffectiveReceiptTime,
                @Origin, @Batch,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public void Update(StockLayerDto dto)
    {
        // Depleted layers remain; RemainingQuantity may become 0. Never DELETE here.
        const string sql = """
            UPDATE BILRG_StokLayer SET
                RemainingQuantity = @RemainingQuantity,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE StockLayerId = @StockLayerId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@StockLayerId", dto.StockLayerId, SqlDbType.VarChar);
        dp.AddParam("@RemainingQuantity", dto.RemainingQuantity, SqlDbType.Decimal);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public StockLayerDto GetData(IStockLayerKey key)
    {
        const string sql = """
            SELECT
                aa.StockLayerId, aa.BrgId, aa.ReceiptSourceId, aa.LayananId,
                aa.LayerFormingMovementId, aa.InitialQuantity, aa.RemainingQuantity,
                aa.AmountPerUnit, aa.ExpirationDate, aa.EffectiveReceiptTime,
                aa.Origin, aa.Batch,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokLayer aa
            WHERE aa.StockLayerId = @StockLayerId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@StockLayerId", key.StockLayerId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockLayerDto>(sql, dp);
    }

    public IEnumerable<StockLayerDto> ListData(IStockWriteScopeKey filter)
    {
        const string sql = """
            SELECT
                aa.StockLayerId, aa.BrgId, aa.ReceiptSourceId, aa.LayananId,
                aa.LayerFormingMovementId, aa.InitialQuantity, aa.RemainingQuantity,
                aa.AmountPerUnit, aa.ExpirationDate, aa.EffectiveReceiptTime,
                aa.Origin, aa.Batch,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokLayer aa
            WHERE
                aa.BrgId = @BrgId
                AND aa.ReceiptSourceId = @ReceiptSourceId
                AND aa.LayananId = @LayananId
            ORDER BY
                aa.EffectiveReceiptTime ASC,
                aa.StockLayerId ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", filter.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", filter.ReceiptSourceId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", filter.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StockLayerDto>(sql, dp) ?? [];
    }

    private static DynamicParameters MapParams(StockLayerDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@StockLayerId", dto.StockLayerId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", dto.ReceiptSourceId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayerFormingMovementId", dto.LayerFormingMovementId, SqlDbType.VarChar);
        dp.AddParam("@InitialQuantity", dto.InitialQuantity, SqlDbType.Decimal);
        dp.AddParam("@RemainingQuantity", dto.RemainingQuantity, SqlDbType.Decimal);
        dp.AddParam("@AmountPerUnit", dto.AmountPerUnit, SqlDbType.Decimal);
        dp.AddParam("@ExpirationDate", dto.ExpirationDate, SqlDbType.DateTime);
        dp.AddParam("@EffectiveReceiptTime", dto.EffectiveReceiptTime, SqlDbType.DateTime);
        dp.AddParam("@Origin", dto.Origin, SqlDbType.Int);
        dp.AddParam("@Batch", dto.Batch, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
