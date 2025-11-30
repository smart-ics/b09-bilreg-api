using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

// Resharper disable InconsistentNaming
namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public interface IKelasDal :
    IInsert<KelasDto>,
    IUpdate<KelasDto>,
    IDelete<IKelasKey>,
    IGetData<KelasDto, IKelasKey>,
    IListData<KelasDto>
{
}

public class KelasDal : IKelasDal
{
    private readonly DatabaseOptions _opt;

    public KelasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KelasDto dto)
    {
        const string sql = """
            INSERT INTO ta_kelas (fs_kd_kelas, fs_nm_kelas, fb_aktif, fs_kd_kelas_dk) 
            VALUES (@fs_kd_kelas, @fs_nm_kelas, @fb_aktif, @fs_kd_kelas_dk)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas", dto.fs_kd_kelas, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelas", dto.fs_nm_kelas, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kelas_dk", dto.fs_kd_kelas_dk, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KelasDto dto)
    {
        const string sql = """
            UPDATE 
                ta_kelas
            SET 
                fs_nm_kelas = @fs_kd_kelas,
                fs_kd_kelas_dk = @fs_nm_kelas,
                fb_aktif = @fb_aktif
            WHERE 
                fs_kd_kelas = @fs_kd_kelas_dk
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas", dto.fs_kd_kelas, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelas", dto.fs_nm_kelas, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kelas_dk", dto.fs_kd_kelas_dk, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKelasKey key)
    {
        const string sql = """
            DELETE FROM 
                ta_kelas
            WHERE 
                fs_kd_kelas = @fs_kd_kelas
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas", key.KelasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KelasDto GetData(IKelasKey key)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_kelas, aa.fs_nm_kelas, aa.fb_aktif, aa.fs_kd_kelas_dk,
                ISNULL(bb.fs_nm_kelas_dk, '') fs_nm_kelas_dk
            FROM 
                ta_kelas aa
                LEFT JOIN ta_kelas_dk bb ON aa.fs_kd_kelas_dk = bb.fs_kd_kelas_dk
            WHERE 
                aa.fs_kd_kelas = @fs_kd_kelas 
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas", key.KelasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KelasDto>(sql, dp);
    }

    public IEnumerable<KelasDto> ListData()
    {
        const string sql = """
            SELECT 
                aa.fs_kd_kelas, aa.fs_nm_kelas, aa.fb_aktif, aa.fs_kd_kelas_dk,
                ISNULL(bb.fs_nm_kelas_dk, '') fs_nm_kelas_dk
            FROM 
                ta_kelas aa
                LEFT JOIN ta_kelas_dk bb ON aa.fs_kd_kelas_dk = bb.fs_kd_kelas_dk 
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KelasDto>(sql);
    }
}