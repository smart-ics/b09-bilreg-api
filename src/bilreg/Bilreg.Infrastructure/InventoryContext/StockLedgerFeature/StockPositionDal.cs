using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStockPositionDal :
    IInsert<StockPositionDto>,
    IGetData<StockPositionDto, IStockWriteScopeKey>
{
    int UpdateConditional(StockPositionDto dto, long expectedVersion);
}

public class StockPositionDal : IStockPositionDal
{
    private readonly DatabaseOptions _opt;

    public StockPositionDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StockPositionDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokPosition (
                BrgId, ReceiptSourceId, LayananId, Version,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @BrgId, @ReceiptSourceId, @LayananId, @Version,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public int UpdateConditional(StockPositionDto dto, long expectedVersion)
    {
        const string sql = """
            UPDATE BILRG_StokPosition SET
                Version = @Version,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE
                BrgId = @BrgId
                AND ReceiptSourceId = @ReceiptSourceId
                AND LayananId = @LayananId
                AND Version = @ExpectedVersion
            """;

        var dp = MapParams(dto);
        dp.AddParam("@ExpectedVersion", expectedVersion, SqlDbType.BigInt);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public StockPositionDto GetData(IStockWriteScopeKey key)
    {
        const string sql = """
            SELECT
                aa.BrgId, aa.ReceiptSourceId, aa.LayananId, aa.Version,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokPosition aa
            WHERE
                aa.BrgId = @BrgId
                AND aa.ReceiptSourceId = @ReceiptSourceId
                AND aa.LayananId = @LayananId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", key.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", key.ReceiptSourceId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", key.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockPositionDto>(sql, dp);
    }

    private static DynamicParameters MapParams(StockPositionDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@ReceiptSourceId", dto.ReceiptSourceId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@Version", dto.Version, SqlDbType.BigInt);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
