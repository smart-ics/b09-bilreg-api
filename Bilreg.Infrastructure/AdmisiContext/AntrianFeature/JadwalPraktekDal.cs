using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JadwalFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class JadwalPraktekDal : IJadwalPraktekDal
{
    private readonly DatabaseOptions _opt;

    public JadwalPraktekDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(JadwalPraktekType model)
    {
        const string sql = @"
           INSERT INTO BILRG_JadwalPraktek(
                JadwalPraktekId, DokterId, SmfId, Hari, JamMulai, JamSelesai)
            VALUES (
                @JadwalPraktekId, @DokterId, @SmfId, @Hari, @JamMulai, @JamSelesai)";
        
        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekId", model.JadwalPraktekId, SqlDbType.VarChar);
        dp.AddParam("@DokterId", model.Dokter.PetugasMedisId, SqlDbType.VarChar);
        dp.AddParam("@SmfId", model.Smf.SmfId, SqlDbType.VarChar);
        dp.AddParam("@Hari", model.Hari, SqlDbType.Int);
        dp.AddParam("@JamMulai", model.JamMulai.ToString(@"hh\:mm"), SqlDbType.VarChar);
        dp.AddParam("@JamSelesai", model.JamSelesai.ToString(@"hh\:mm"), SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(JadwalPraktekType model)
    {
        const string sql = @"
            UPDATE BILRG_JadwalPraktek 
            SET 
                DokterId = @DokterId,
                SmfId = @SmfId,
                Hari = @Hari,
                JamMulai = @JamMulai,
                JamSelesai = @JamSelesai
            WHERE 
                JadwalPraktekId = @JadwalPraktekId";

        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekId", model.JadwalPraktekId, SqlDbType.VarChar);
        dp.AddParam("@DokterId", model.Dokter.PetugasMedisId, SqlDbType.VarChar);
        dp.AddParam("@SmfId", model.Smf.SmfId, SqlDbType.VarChar);
        dp.AddParam("@Hari", model.Hari, SqlDbType.Int);
        dp.AddParam("@JamMulai", model.JamMulai.ToString(@"hh\:mm"), SqlDbType.VarChar);
        dp.AddParam("@JamSelesai", model.JamSelesai.ToString(@"hh\:mm"), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IJadwalPraktekKey key)
    {
        const string sql = @"
            DELETE FROM BILRG_JadwalPraktek 
            WHERE JadwalPraktekId = @JadwalPraktekId";
    
        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekId", key.JadwalPraktekId, SqlDbType.VarChar);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<JadwalPraktekType> GetData(IJadwalPraktekKey key)
    {
        const string sql = @"
            SELECT
                aa.JadwalPraktekId, aa.DokterId, aa.SmfId, aa.Hari, aa.JamMulai, aa.JamSelesai,
                ISNULL(bb.fs_nm_peg, '-') AS DokterName,
                ISNULL(cc.fs_nm_smf, '-') AS SmfName
            FROM 
                BILRG_JadwalPraktek aa
                LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
                LEFT JOIN ta_smf cc ON aa.SmfId = cc.fs_kd_smf
            WHERE
                aa.JadwalPraktekId = @JadwalPraktekId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekId", key.JadwalPraktekId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.ReadSingle<JadwalPraktekDto>(sql, dp))
            .Map(x => x.ToModel());
    }

    public MayBe<IEnumerable<JadwalPraktekType>> ListData(IPetugasMedisKey filter)
    {
        const string sql = @"
            SELECT
                aa.JadwalPraktekId, aa.DokterId, aa.SmfId, aa.Hari, aa.JamMulai, aa.JamSelesai,
                ISNULL(bb.fs_nm_peg, '-') AS DokterName,
                ISNULL(cc.fs_nm_smf, '-') AS SmfName
            FROM 
                BILRG_JadwalPraktek aa
                LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
                LEFT JOIN ta_smf cc ON aa.SmfId = cc.fs_kd_smf
            WHERE
                aa.DokterId = @DokterId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@DokterId", filter.PetugasMedisId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.Read<JadwalPraktekDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));
    }

    public MayBe<IEnumerable<JadwalPraktekType>> ListData(ISmfKey filter)
    {
        const string sql = @"
            SELECT
                aa.JadwalPraktekId, aa.DokterId, aa.SmfId, aa.Hari, aa.JamMulai, aa.JamSelesai,
                ISNULL(bb.fs_nm_peg, '-') AS DokterName,
                ISNULL(cc.fs_nm_smf, '-') AS SmfName
            FROM 
                BILRG_JadwalPraktek aa
                LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
                LEFT JOIN ta_smf cc ON aa.SmfId = cc.fs_kd_smf
            WHERE
                aa.SmfId = @SmfId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@SmfId", filter.SmfId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.Read<JadwalPraktekDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));
    }
}

public record JadwalPraktekDto(
    string JadwalPraktekId,
    string DokterId,
    string SmfId,
    int Hari,
    string JamMulai,
    string JamSelesai,
    string DokterName,
    string SmfName)
{
    public JadwalPraktekType ToModel()
    {
        var dokter = new PetugasMedisReff(DokterId, DokterName);
        var smf = new SmfType(SmfId, SmfName);
        var hari = (DayOfWeek)Hari;
        var jamMulai = TimeSpan.Parse(JamMulai);
        var jamSelesai = TimeSpan.Parse(JamSelesai);
        return new JadwalPraktekType(JadwalPraktekId, dokter, smf, hari, jamMulai, jamSelesai);
    }
}