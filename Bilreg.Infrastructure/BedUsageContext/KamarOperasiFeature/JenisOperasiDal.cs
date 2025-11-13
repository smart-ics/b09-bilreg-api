using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IJenisOperasiDal :
    IInsert<JenisOperasiDto>,
    IUpdate<JenisOperasiDto>,
    IDelete<IJenisOperasiKey>,
    IGetData<JenisOperasiDto, IJenisOperasiKey>,
    IListData<JenisOperasiDto>
{
}

public class JenisOperasiDal : IJenisOperasiDal
{
    private readonly DatabaseOptions _opt;

    public JenisOperasiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(JenisOperasiDto dto)
    {
        const string sql = """
           INSERT INTO ta_jenis_operasi(fs_kd_jenis_operasi, fs_nm_jenis_operasi)
           VALUES (@fs_kd_jenis_operasi, @fs_nm_jenis_operasi)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_operasi", dto.fs_kd_jenis_operasi, SqlDbType.VarChar); 
        dp.AddParam("@fs_nm_jenis_operasi", dto.fs_nm_jenis_operasi, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(JenisOperasiDto dto)
    {
        const string sql = """
           UPDATE
                ta_jenis_operasi
            SET 
                fs_nm_jenis_operasi = @fs_nm_jenis_operasi
           WHERE
                fs_kd_jenis_operasi = @fs_kd_jenis_operasi
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_operasi", dto.fs_kd_jenis_operasi, SqlDbType.VarChar); 
        dp.AddParam("@fs_nm_jenis_operasi", dto.fs_nm_jenis_operasi, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IJenisOperasiKey key)
    {
        const string sql = """
           DELETE FROM
                ta_jenis_operasi
           WHERE
                fs_kd_jenis_operasi = @fs_kd_jenis_operasi
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_operasi", key.JenisOperasiId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public JenisOperasiDto GetData(IJenisOperasiKey key)
    {
        const string sql = """
           SELECT fs_kd_jenis_operasi, fs_nm_jenis_operasi
           FROM ta_jenis_operasi
           WHERE fs_kd_jenis_operasi = @fs_kd_jenis_operasi
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_operasi", key.JenisOperasiId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<JenisOperasiDto>(sql, dp);
    }

    public IEnumerable<JenisOperasiDto> ListData()
    {
        const string sql = """
           SELECT fs_kd_jenis_operasi, fs_nm_jenis_operasi
           FROM ta_jenis_operasi
           """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JenisOperasiDto>(sql);
    }
}