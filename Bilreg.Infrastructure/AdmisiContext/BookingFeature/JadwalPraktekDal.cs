using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.BookingFeature;

public class JadwalPraktekDal
{
    private readonly DatabaseOptions _opt;

    public JadwalPraktekDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(JadwalPraktekDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_JadwalPraktek(
               JadwalPraktekId, DokterId, LayananId, Hari, JamMulai, JamSelesai)
            VALUES (
               @JadwalPraktekId, @DokterId, @LayananId, @Hari, @JamMulai, @JamSelesai)
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekId", dto.JadwalPraktekId, SqlDbType.VarChar);
        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@Hari", dto.Hari, SqlDbType.Int);
        dp.AddParam("@JamMulai", dto.JamMulai, SqlDbType.VarChar);
        dp.AddParam("@JamSelesai", dto.JamSelesai, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(JadwalPraktekDto dto)
    {
        const string sql = """
            UPDATE BILRG_JadwalPraktek 
            SET 
               DokterId = @DokterId,
               LayananId = @LayananId,
               Hari = @Hari,
               JamMulai = @JamMulai,
               JamSelesai = @JamSelesai
            WHERE 
               JadwalPraktekId = @JadwalPraktekId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekId", dto.JadwalPraktekId, SqlDbType.VarChar);
        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@Hari", dto.Hari, SqlDbType.Int);
        dp.AddParam("@JamMulai", dto.JamMulai, SqlDbType.VarChar);
        dp.AddParam("@JamSelesai", dto.JamSelesai, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IJadwalPraktekKey key)
    {
        const string sql = """
            DELETE FROM BILRG_JadwalPraktek 
            WHERE JadwalPraktekId = @JadwalPraktekId
            """;
    
        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekId", key.JadwalPraktekId, SqlDbType.VarChar);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public JadwalPraktekDto GetData(IJadwalPraktekKey key)
    {
        const string sql = """
            SELECT
               aa.JadwalPraktekId, aa.DokterId, aa.LayananId, 
               aa.Hari, aa.JamMulai, aa.JamSelesai,
               ISNULL(bb.fs_nm_peg, '-') AS DokterName,
               ISNULL(cc.fs_nm_layanan, '-') AS LayananName
            FROM 
               BILRG_JadwalPraktek aa
               LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
            WHERE
               aa.JadwalPraktekId = @JadwalPraktekId
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekId", key.JadwalPraktekId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<JadwalPraktekDto>(sql, dp);
    }

    public IEnumerable<JadwalPraktekDto> ListData(IPetugasMedisKey filter)
    {
        const string sql = """
            SELECT
               aa.JadwalPraktekId, aa.DokterId, aa.LayananId, 
               aa.Hari, aa.JamMulai, aa.JamSelesai,
               ISNULL(bb.fs_nm_peg, '-') AS DokterName,
               ISNULL(cc.fs_nm_layanan, '-') AS LayananName
            FROM 
               BILRG_JadwalPraktek aa
               LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
            WHERE
               aa.DokterId = @DokterId
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@DokterId", filter.PetugasMedisId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<JadwalPraktekDto>(sql, dp);
        return result;
    }
}
