using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStokLegacyBindingDal :
    IInsert<StokLegacyBindingDto>
{
    StokLegacyBindingDto GetByLegacyBukuId(string legacyBukuId);
    StokLegacyBindingDto GetByStokMutasiId(string stokMutasiId);
}

public class StokLegacyBindingDal : IStokLegacyBindingDal
{
    private const string SelectFrom = """
        SELECT
            aa.BindingId, aa.BindingKind, aa.StokMutasiId, aa.StokLokasiId,
            aa.LegacyBukuId, aa.LegacyStokId, aa.TrsReffId,
            aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate
        FROM BILRG_StokLegacyBinding aa
        """;

    private readonly DatabaseOptions _opt;

    public StokLegacyBindingDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(StokLegacyBindingDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_StokLegacyBinding(
                BindingId, BindingKind, StokMutasiId, StokLokasiId,
                LegacyBukuId, LegacyStokId, TrsReffId,
                CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES(
                @BindingId, @BindingKind, @StokMutasiId, @StokLokasiId,
                @LegacyBukuId, @LegacyStokId, @TrsReffId,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(dto));
    }

    public StokLegacyBindingDto GetByLegacyBukuId(string legacyBukuId)
    {
        var sql = $"{SelectFrom}\nWHERE aa.LegacyBukuId = @LegacyBukuId";
        var dp = new DynamicParameters();
        dp.AddParam("@LegacyBukuId", legacyBukuId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StokLegacyBindingDto>(sql, dp);
    }

    public StokLegacyBindingDto GetByStokMutasiId(string stokMutasiId)
    {
        var sql = $"{SelectFrom}\nWHERE aa.StokMutasiId = @StokMutasiId";
        var dp = new DynamicParameters();
        dp.AddParam("@StokMutasiId", stokMutasiId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<StokLegacyBindingDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(StokLegacyBindingDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@BindingId", dto.BindingId, SqlDbType.VarChar);
        dp.AddParam("@BindingKind", dto.BindingKind, SqlDbType.Int);
        dp.AddParam("@StokMutasiId", dto.StokMutasiId, SqlDbType.VarChar);
        dp.AddParam("@StokLokasiId", dto.StokLokasiId, SqlDbType.VarChar);
        dp.AddParam("@LegacyBukuId", dto.LegacyBukuId, SqlDbType.VarChar);
        dp.AddParam("@LegacyStokId", dto.LegacyStokId, SqlDbType.VarChar);
        dp.AddParam("@TrsReffId", dto.TrsReffId, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        return dp;
    }
}
