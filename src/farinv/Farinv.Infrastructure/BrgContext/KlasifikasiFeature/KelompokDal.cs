using Dapper;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public interface IKelompokDal :
    IInsert<KelompokDto>,
    IUpdate<KelompokDto>,
    IDelete<IKelompokKey>,
    IGetData<KelompokDto, IKelompokKey>,
    IListData<KelompokDto>
{
}

public class KelompokDal : IKelompokDal
{
    private readonly DatabaseOptions _opt;

    public KelompokDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KelompokDto dto)
    {
        const string sql = """
            INSERT INTO tb_kelompok (
                fs_kd_kelompok,
                fs_nm_kelompok
            ) VALUES (
                @fs_kd_kelompok,
                @fs_nm_kelompok
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelompok", dto.fs_kd_kelompok, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelompok", dto.fs_nm_kelompok, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KelompokDto dto)
    {
        const string sql = """
            UPDATE tb_kelompok
            SET
                fs_nm_kelompok = @fs_nm_kelompok
            WHERE
                fs_kd_kelompok = @fs_kd_kelompok
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelompok", dto.fs_kd_kelompok, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelompok", dto.fs_nm_kelompok, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKelompokKey key)
    {
        const string sql = """
            DELETE FROM tb_kelompok
            WHERE fs_kd_kelompok = @fs_kd_kelompok
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelompok", key.KelompokId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KelompokDto GetData(IKelompokKey key)
    {
        const string sql = """
            SELECT
                fs_kd_kelompok,
                fs_nm_kelompok
            FROM tb_kelompok
            WHERE fs_kd_kelompok = @fs_kd_kelompok
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelompok", key.KelompokId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KelompokDto>(sql, dp);
    }

    public IEnumerable<KelompokDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_kelompok,
                fs_nm_kelompok
            FROM tb_kelompok
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KelompokDto>(sql);
    }
}