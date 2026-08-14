using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStokLokasiDal :
    IInsert<StokLokasiDto>,
    IGetData<StokLokasiDto, IStokLokasiKey>
{
    int UpdateConditional(StokLokasiDto dto, long expectedVersion);
    IEnumerable<StokLokasiDto> ListByStokBatchId(string stokBatchId);
    IEnumerable<StokLokasiDto> ListAllocationCandidates(string brgId, string layananId);
}

public class StokLokasiDal : IStokLokasiDal
{
    private const string SelectFrom = """
        SELECT
            aa.StokLokasiId, aa.StokBatchId, aa.BrgId, aa.BrgMasukReffId,
            aa.LayananId, aa.TglEd, aa.NoBatch, aa.QtySisa, aa.Version, aa.TglMasuk,
            aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate
        FROM BILRG_StokLokasi aa
        """;

    private readonly DatabaseOptions _opt;

    public StokLokasiDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StokLokasiDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokLokasi(
                StokLokasiId, StokBatchId, BrgId, BrgMasukReffId,
                LayananId, TglEd, NoBatch, QtySisa, Version, TglMasuk,
                CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES(
                @StokLokasiId, @StokBatchId, @BrgId, @BrgMasukReffId,
                @LayananId, @TglEd, @NoBatch, @QtySisa, @Version, @TglMasuk,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public int UpdateConditional(StokLokasiDto dto, long expectedVersion)
    {
        // Preserve denormalized TglMasuk on update; never DELETE depleted rows.
        const string sql = """
            UPDATE BILRG_StokLokasi SET
                QtySisa = @QtySisa,
                NoBatch = @NoBatch,
                TglMasuk = @TglMasuk,
                Version = @Version,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE StokLokasiId = @StokLokasiId
              AND Version = @ExpectedVersion
            """;
        var dp = BuildParams(dto);
        dp.AddParam("@ExpectedVersion", expectedVersion, SqlDbType.BigInt);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public StokLokasiDto GetData(IStokLokasiKey key)
    {
        var sql = $"{SelectFrom}\nWHERE aa.StokLokasiId = @StokLokasiId";
        var dp = new DynamicParameters();
        dp.AddParam("@StokLokasiId", key.StokLokasiId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StokLokasiDto>(sql, dp);
    }

    public IEnumerable<StokLokasiDto> ListByStokBatchId(string stokBatchId)
    {
        var sql = $"{SelectFrom}\nWHERE aa.StokBatchId = @StokBatchId";
        var dp = new DynamicParameters();
        dp.AddParam("@StokBatchId", stokBatchId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StokLokasiDto>(sql, dp);
    }

    public IEnumerable<StokLokasiDto> ListAllocationCandidates(string brgId, string layananId)
    {
        var sql = $"""
            {SelectFrom}
            WHERE aa.BrgId = @BrgId
              AND aa.LayananId = @LayananId
              AND aa.QtySisa > 0
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", layananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StokLokasiDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(StokLokasiDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@StokLokasiId", dto.StokLokasiId, SqlDbType.VarChar);
        dp.AddParam("@StokBatchId", dto.StokBatchId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BrgMasukReffId", dto.BrgMasukReffId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@TglEd", dto.TglEd, SqlDbType.DateTime);
        dp.AddParam("@NoBatch", dto.NoBatch, SqlDbType.VarChar);
        dp.AddParam("@QtySisa", dto.QtySisa, SqlDbType.Decimal);
        dp.AddParam("@Version", dto.Version, SqlDbType.BigInt);
        dp.AddParam("@TglMasuk", dto.TglMasuk, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        return dp;
    }
}
