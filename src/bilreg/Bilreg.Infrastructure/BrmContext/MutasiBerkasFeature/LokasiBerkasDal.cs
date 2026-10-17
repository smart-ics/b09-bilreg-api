using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using AppLokasiBerkasDto = Bilreg.Application.BrmContext.MutasiBerkasFeature.LokasiBerkasDto;

namespace Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;

public class LokasiBerkasDal : ILokasiBerkasDal, IListData<AppLokasiBerkasDto>
{
    private readonly DatabaseOptions _opt;

    public LokasiBerkasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<AppLokasiBerkasDto> ListData()
    {
        return ListAll();
    }

    public IEnumerable<AppLokasiBerkasDto> ListAll()
    {
        const string sql = """
            SELECT
                FS_KD_LOKASI_BERKAS, FS_NM_LOKASI_BERKAS, FS_KD_LAYANAN
            FROM TC_LOKASI_BERKAS
            ORDER BY FS_KD_LOKASI_BERKAS
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AppLokasiBerkasDto>(sql);
    }

    public AppLokasiBerkasDto? GetData(string lokasiId)
    {
        const string sql = """
            SELECT
                FS_KD_LOKASI_BERKAS, FS_NM_LOKASI_BERKAS, FS_KD_LAYANAN
            FROM TC_LOKASI_BERKAS
            WHERE FS_KD_LOKASI_BERKAS = @FS_KD_LOKASI_BERKAS
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_LOKASI_BERKAS", lokasiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<AppLokasiBerkasDto>(sql, dp);
    }

    public void Insert(AppLokasiBerkasDto dto)
    {
        const string sql = """
            INSERT INTO TC_LOKASI_BERKAS (
                FS_KD_LOKASI_BERKAS, FS_NM_LOKASI_BERKAS, FS_KD_LAYANAN
            ) VALUES (
                @FS_KD_LOKASI_BERKAS, @FS_NM_LOKASI_BERKAS, @FS_KD_LAYANAN
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_LOKASI_BERKAS", dto.FS_KD_LOKASI_BERKAS, SqlDbType.VarChar);
        dp.AddParam("@FS_NM_LOKASI_BERKAS", dto.FS_NM_LOKASI_BERKAS, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_LAYANAN", dto.FS_KD_LAYANAN, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(string lokasiId)
    {
        const string sql = """
            DELETE FROM TC_LOKASI_BERKAS
            WHERE FS_KD_LOKASI_BERKAS = @FS_KD_LOKASI_BERKAS
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_LOKASI_BERKAS", lokasiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
