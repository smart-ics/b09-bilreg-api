using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStokMutasiDal :
    IInsert<StokMutasiDto>
{
    bool Exists(string trsReffId, int movementKind, string stokLokasiId);
    bool ExistsReversalFor(string originalStokMutasiId);
    IEnumerable<StokMutasiDto> ListByTrsReffId(string trsReffId);
}

public class StokMutasiDal : IStokMutasiDal
{
    private readonly DatabaseOptions _opt;

    public StokMutasiDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StokMutasiDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokMutasi(
                StokMutasiId, StokLokasiId, StokBatchId,
                BrgId, BrgMasukReffId, LayananId, TglEd,
                TrsReffId, MovementKind, QtyIn, QtyOut, Hpp,
                PoReffId, TglMutasi, ReversesMutasiId,
                CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES(
                @StokMutasiId, @StokLokasiId, @StokBatchId,
                @BrgId, @BrgMasukReffId, @LayananId, @TglEd,
                @TrsReffId, @MovementKind, @QtyIn, @QtyOut, @Hpp,
                @PoReffId, @TglMutasi, @ReversesMutasiId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public bool Exists(string trsReffId, int movementKind, string stokLokasiId)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM BILRG_StokMutasi
            WHERE TrsReffId = @TrsReffId
              AND MovementKind = @MovementKind
              AND StokLokasiId = @StokLokasiId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TrsReffId", trsReffId, SqlDbType.VarChar);
        dp.AddParam("@MovementKind", movementKind, SqlDbType.Int);
        dp.AddParam("@StokLokasiId", stokLokasiId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp) > 0;
    }

    public bool ExistsReversalFor(string originalStokMutasiId)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM BILRG_StokMutasi
            WHERE ReversesMutasiId = @ReversesMutasiId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@ReversesMutasiId", originalStokMutasiId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp) > 0;
    }

    public IEnumerable<StokMutasiDto> ListByTrsReffId(string trsReffId)
    {
        const string sql = """
            SELECT
                aa.StokMutasiId, aa.StokLokasiId, aa.StokBatchId,
                aa.BrgId, aa.BrgMasukReffId, aa.LayananId, aa.TglEd,
                aa.TrsReffId, aa.MovementKind, aa.QtyIn, aa.QtyOut, aa.Hpp,
                aa.PoReffId, aa.TglMutasi, aa.ReversesMutasiId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate
            FROM BILRG_StokMutasi aa
            WHERE aa.TrsReffId = @TrsReffId
            ORDER BY aa.TglMutasi
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TrsReffId", trsReffId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StokMutasiDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(StokMutasiDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@StokMutasiId", dto.StokMutasiId, SqlDbType.VarChar);
        dp.AddParam("@StokLokasiId", dto.StokLokasiId, SqlDbType.VarChar);
        dp.AddParam("@StokBatchId", dto.StokBatchId, SqlDbType.VarChar);
        dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
        dp.AddParam("@BrgMasukReffId", dto.BrgMasukReffId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@TglEd", dto.TglEd, SqlDbType.DateTime);
        dp.AddParam("@TrsReffId", dto.TrsReffId, SqlDbType.VarChar);
        dp.AddParam("@MovementKind", dto.MovementKind, SqlDbType.Int);
        dp.AddParam("@QtyIn", dto.QtyIn, SqlDbType.Decimal);
        dp.AddParam("@QtyOut", dto.QtyOut, SqlDbType.Decimal);
        dp.AddParam("@Hpp", dto.Hpp, SqlDbType.Decimal);
        dp.AddParam("@PoReffId", dto.PoReffId, SqlDbType.VarChar);
        dp.AddParam("@TglMutasi", dto.TglMutasi, SqlDbType.DateTime);
        dp.AddParam("@ReversesMutasiId", dto.ReversesMutasiId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        return dp;
    }
}
