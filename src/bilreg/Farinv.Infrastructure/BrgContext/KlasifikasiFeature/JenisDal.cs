using Dapper;
using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.KlasifikasiFeature;

public interface IJenisDal :
    IInsert<JenisDto>,
    IUpdate<JenisDto>,
    IDelete<IJenisKey>,
    IGetData<JenisDto, IJenisKey>,
    IListData<JenisDto>
{
}

public class JenisDal : IJenisDal
{
    private readonly DatabaseOptions _opt;

    public JenisDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(JenisDto dto)
    {
        const string sql = """
            INSERT INTO tb_jenis_barang (
                fs_kd_jenis_barang,
                fs_nm_jenis_barang
            ) VALUES (
                @fs_kd_jenis_barang,
                @fs_nm_jenis_barang
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_barang", dto.fs_kd_jenis_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jenis_barang", dto.fs_nm_jenis_barang, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(JenisDto dto)
    {
        const string sql = """
            UPDATE tb_jenis_barang
            SET
                fs_nm_jenis_barang = @fs_nm_jenis_barang
            WHERE
                fs_kd_jenis_barang = @fs_kd_jenis_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_barang", dto.fs_kd_jenis_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jenis_barang", dto.fs_nm_jenis_barang, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IJenisKey key)
    {
        const string sql = """
            DELETE FROM tb_jenis_barang
            WHERE fs_kd_jenis_barang = @fs_kd_jenis_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_barang", key.JenisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public JenisDto GetData(IJenisKey key)
    {
        const string sql = """
            SELECT
                fs_kd_jenis_barang,
                fs_nm_jenis_barang
            FROM tb_jenis_barang
            WHERE fs_kd_jenis_barang = @fs_kd_jenis_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_barang", key.JenisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<JenisDto>(sql, dp);
    }

    public IEnumerable<JenisDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_jenis_barang,
                fs_nm_jenis_barang
            FROM tb_jenis_barang
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JenisDto>(sql);
    }
}