using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStockBatchDal :
    IInsert<StockBatchDto>,
    IGetData<StockBatchDto, IStockBatchKey>
{
    int UpdateConditional(StockBatchDto dto, long expectedVersion);
    StockBatchDto GetByNaturalKey(string brgId, string brgMasukReffId);
}

public class StockBatchDal : IStockBatchDal
{
    private const string SelectFrom = """
        SELECT
            aa.StokBatchId, aa.BrgId, aa.BrgMasukReffId,
            aa.QtySisa, aa.Hpp, aa.TglMasuk, aa.PoReffId, aa.Version,
            aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate
        FROM BILRG_StokBatch aa
        """;

    private readonly DatabaseOptions _opt;

    public StockBatchDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StockBatchDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokBatch(
                StokBatchId, BrgId, BrgMasukReffId,
                QtySisa, Hpp, TglMasuk, PoReffId, Version,
                CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES(
                @StokBatchId, @BrgId, @BrgMasukReffId,
                @QtySisa, @Hpp, @TglMasuk, @PoReffId, @Version,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public int UpdateConditional(StockBatchDto dto, long expectedVersion)
    {
        const string sql = """
            UPDATE BILRG_StokBatch SET
                QtySisa = @QtySisa,
                Version = @Version,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE StokBatchId = @StokBatchId
              AND Version = @ExpectedVersion
            """;
        var dp = BuildParams(dto);
        dp.AddParam("@ExpectedVersion", expectedVersion, SqlDbType.BigInt);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public StockBatchDto GetData(IStockBatchKey key)
    {
        var sql = $"{SelectFrom}\nWHERE aa.StokBatchId = @StokBatchId";
        var dp = new DynamicParameters();
        dp.AddParam("@StokBatchId", key.StokBatchId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockBatchDto>(sql, dp);
    }

    public StockBatchDto GetByNaturalKey(string brgId, string brgMasukReffId)
    {
        var sql = $"{SelectFrom}\nWHERE aa.BrgId = @BrgId AND aa.BrgMasukReffId = @BrgMasukReffId";
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@BrgMasukReffId", brgMasukReffId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StockBatchDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(StockBatchDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@StokBatchId", dto.StokBatchId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BrgMasukReffId", dto.BrgMasukReffId, SqlDbType.VarChar);
        dp.AddParam("@QtySisa", dto.QtySisa, SqlDbType.Decimal);
        dp.AddParam("@Hpp", dto.Hpp, SqlDbType.Decimal);
        dp.AddParam("@TglMasuk", dto.TglMasuk, SqlDbType.DateTime);
        dp.AddParam("@PoReffId", dto.PoReffId, SqlDbType.VarChar);
        dp.AddParam("@Version", dto.Version, SqlDbType.BigInt);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        return dp;
    }
}
