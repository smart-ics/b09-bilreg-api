using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.WardFeature;

public interface IKamarDal :
    IInsert<KamarDto>,
    IUpdate<KamarDto>,
    IDelete<IKamarKey>,
    IGetData<KamarDto, IKamarKey>,
    IListData<KamarDto>
{
}

public class KamarDal : IKamarDal
{
    private readonly DatabaseOptions _opt;

    public KamarDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(KamarDto dto)
    {
        const string sql = """
                           INSERT INTO ta_kamar(
                               fs_kd_kamar, fs_nm_kamar, fs_kd_bangsal, fs_kd_kelas)
                           VALUES( 
                               @fs_kd_kamar, @fs_nm_kamar, @fs_kd_bangsal, @fs_kd_kelas)
                           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar", dto.fs_kd_kamar, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kamar", dto.fs_nm_kamar, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bangsal", dto.fs_kd_bangsal, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas", dto.fs_kd_kelas, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KamarDto dto)
    {
        const string sql = """
                           UPDATE 
                               ta_kamar
                           SET
                               fs_nm_kamar = @fs_nm_kamar,
                               fs_kd_bangsal = @fs_kd_bangsal,
                               fs_kd_kelas = @fs_kd_kelas
                           WHERE
                               fs_kd_kamar = @fs_kd_kamar
                           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar", dto.fs_kd_kamar, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kamar", dto.fs_nm_kamar, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bangsal", dto.fs_kd_bangsal, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas", dto.fs_kd_kelas, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKamarKey key)
    {
        const string sql = """
                           DELETE FROM 
                               ta_kamar
                           WHERE
                               fs_kd_kamar = @fs_kd_kamar
                           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar", key.KamarId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KamarDto GetData(IKamarKey key)
    {
        const string sql = """
                           SELECT
                               aa.fs_kd_kamar, aa.fs_nm_kamar, aa.fs_kd_bangsal, aa.fs_kd_kelas,
                               ISNULL(bb.fs_nm_bangsal, '') fs_nm_bangsal,
                               ISNULL(cc.fs_nm_kelas, '') fs_nm_kelas
                           FROM 
                               ta_kamar aa
                               LEFT JOIN ta_bangsal bb ON aa.fs_kd_bangsal = bb.fs_kd_bangsal
                               LEFT JOIN ta_kelas cc ON aa.fs_kd_kelas = cc.fs_kd_kelas
                           WHERE
                               aa.fs_kd_kamar = @fs_kd_kamar
                           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar", key.KamarId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<KamarDto>(sql, dp);
        return result;
    }

    public IEnumerable<KamarDto> ListData()
    {
        const string sql = """
                           SELECT
                               aa.fs_kd_kamar, aa.fs_nm_kamar, aa.fs_kd_bangsal, aa.fs_kd_kelas,
                               ISNULL(bb.fs_nm_bangsal, '') fs_nm_bangsal,
                               ISNULL(cc.fs_nm_kelas, '') fs_nm_kelas
                           FROM 
                               ta_kamar aa
                               LEFT JOIN ta_bangsal bb ON aa.fs_kd_bangsal = bb.fs_kd_bangsal
                               LEFT JOIN ta_kelas cc ON aa.fs_kd_kelas = cc.fs_kd_kelas
                           """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KamarDto>(sql);
    }
}