using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Shared.Helpers;
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
               JadwalPraktekId, DokterId, LayananId, Hari, JamMulai, JamSelesai, MaxPasien)
            VALUES (
               @JadwalPraktekId, @DokterId, @LayananId, @Hari, @JamMulai, @JamSelesai, @MaxPasien)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekId", dto.JadwalPraktekId, SqlDbType.VarChar);
        dp.AddParam("@DokterId", dto.DokterId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@Hari", dto.Hari, SqlDbType.Int);
        dp.AddParam("@JamMulai", dto.JamMulai, SqlDbType.VarChar);
        dp.AddParam("@JamSelesai", dto.JamSelesai, SqlDbType.VarChar);
        dp.AddParam("@MaxPasien", dto.MaxPasien, SqlDbType.Int);

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
               JamSelesai = @JamSelesai,
               MaxPasien = @MaxPasien
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
        dp.AddParam("@MaxPasien", dto.MaxPasien, SqlDbType.Int);

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
               aa.Hari, aa.JamMulai, aa.JamSelesai, aa.MaxPasien,
               ISNULL(bb.fs_nm_peg, '-') AS DokterName,
               ISNULL(cc.fs_nm_layanan, '-') AS LayananName,
               ISNULL(cc.fs_kd_layanan_dk,'') AS LayananDkId,
               ISNULL(dd.fs_nm_layanan_dk,'') AS LayananDkName,
               ISNULL(ee.GroupSpesialisId,'') AS GroupSpesialisId,
               ISNULL(ff.GroupSpesialisName,'') AS GroupSpesialisName
            FROM 
               BILRG_JadwalPraktek aa
               LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
               LEFT JOIN ta_layanan_dk dd ON cc.fs_kd_layanan_dk = dd.fs_kd_layanan_dk
               LEFT JOIN td_peg2 ee ON aa.DokterId = ee.fs_kd_peg 
               LEFT JOIN BILRG_GroupSpesialis ff ON ee.GroupSpesialisId = ff.GroupSpesialisId
            WHERE
               aa.JadwalPraktekId = @JadwalPraktekId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@JadwalPraktekId", key.JadwalPraktekId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<JadwalPraktekDto>(sql, dp);
    }

    public IEnumerable<JadwalPraktekDto> ListData(IPpaKey filter)
    {
        const string sql = """
            SELECT
               aa.JadwalPraktekId, aa.DokterId, aa.LayananId, 
               aa.Hari, aa.JamMulai, aa.JamSelesai, aa.MaxPasien,
               ISNULL(bb.fs_nm_peg, '-') AS DokterName,
               ISNULL(cc.fs_nm_layanan, '-') AS LayananName,
               ISNULL(cc.fs_kd_layanan_dk,'') AS LayananDkId,
               ISNULL(dd.fs_nm_layanan_dk,'') AS LayananDkName,
               ISNULL(ee.GroupSpesialisId,'') AS GroupSpesialisId,
               ISNULL(ff.GroupSpesialisName,'') AS GroupSpesialisName
            FROM 
               BILRG_JadwalPraktek aa
               LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
               LEFT JOIN ta_layanan_dk dd ON cc.fs_kd_layanan_dk = dd.fs_kd_layanan_dk
               LEFT JOIN td_peg2 ee ON aa.DokterId = ee.fs_kd_peg 
               LEFT JOIN BILRG_GroupSpesialis ff ON ee.GroupSpesialisId = ff.GroupSpesialisId
            WHERE
               aa.DokterId = @DokterId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@DokterId", filter.PpaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<JadwalPraktekDto>(sql, dp);
        return result;
    }

    public IEnumerable<JadwalPraktekDto> ListData(ILayananKey lyn)
    {
        const string sql = """
            SELECT
               aa.JadwalPraktekId, aa.DokterId, aa.LayananId, 
               aa.Hari, aa.JamMulai, aa.JamSelesai, aa.MaxPasien,
               ISNULL(bb.fs_nm_peg, '-') AS DokterName,
               ISNULL(cc.fs_nm_layanan, '-') AS LayananName,
               ISNULL(cc.fs_kd_layanan_dk,'') AS LayananDkId,
               ISNULL(dd.fs_nm_layanan_dk,'') AS LayananDkName,
               ISNULL(ee.GroupSpesialisId,'') AS GroupSpesialisId,
               ISNULL(ff.GroupSpesialisName,'') AS GroupSpesialisName
            FROM 
               BILRG_JadwalPraktek aa
               LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
               LEFT JOIN ta_layanan_dk dd ON cc.fs_kd_layanan_dk = dd.fs_kd_layanan_dk
               LEFT JOIN td_peg2 ee ON aa.DokterId = ee.fs_kd_peg 
               LEFT JOIN BILRG_GroupSpesialis ff ON ee.GroupSpesialisId = ff.GroupSpesialisId
            WHERE
               aa.LayananId = @LayananId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@LayananId", lyn.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<JadwalPraktekDto>(sql, dp);
        return result;
    }

    public IEnumerable<JadwalPraktekDto> ListData()
    {
        const string sql = """
            SELECT
               aa.JadwalPraktekId, aa.DokterId, aa.LayananId, 
               aa.Hari, aa.JamMulai, aa.JamSelesai, aa.MaxPasien,
               ISNULL(bb.fs_nm_peg, '-') AS DokterName,
               ISNULL(cc.fs_nm_layanan, '-') AS LayananName,
               ISNULL(cc.fs_kd_layanan_dk,'') AS LayananDkId,
               ISNULL(dd.fs_nm_layanan_dk,'') AS LayananDkName,
               ISNULL(ee.GroupSpesialisId,'') AS GroupSpesialisId,
               ISNULL(ff.GroupSpesialisName,'') AS GroupSpesialisName
            FROM 
               BILRG_JadwalPraktek aa
               LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
               LEFT JOIN ta_layanan_dk dd ON cc.fs_kd_layanan_dk = dd.fs_kd_layanan_dk
               LEFT JOIN td_peg2 ee ON aa.DokterId = ee.fs_kd_peg 
               LEFT JOIN BILRG_GroupSpesialis ff ON ee.GroupSpesialisId = ff.GroupSpesialisId
            """;


        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<JadwalPraktekDto>(sql);
        return result;
    }

    public IEnumerable<JadwalPraktekDto> ListData(ILayananDkKey lynDk)
    {
        const string sql = """
            SELECT
               aa.JadwalPraktekId, aa.DokterId, aa.LayananId, 
               aa.Hari, aa.JamMulai, aa.JamSelesai, aa.MaxPasien,
               ISNULL(bb.fs_nm_peg, '-') AS DokterName,
               ISNULL(cc.fs_nm_layanan, '-') AS LayananName,
               ISNULL(cc.fs_kd_layanan_dk,'') AS LayananDkId,
               ISNULL(dd.fs_nm_layanan_dk,'') AS LayananDkName,
               ISNULL(ee.GroupSpesialisId,'') AS GroupSpesialisId,
               ISNULL(ff.GroupSpesialisName,'') AS GroupSpesialisName
            FROM 
               BILRG_JadwalPraktek aa
               LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
               LEFT JOIN ta_layanan_dk dd ON cc.fs_kd_layanan_dk = dd.fs_kd_layanan_dk
               LEFT JOIN td_peg2 ee ON aa.DokterId = ee.fs_kd_peg 
               LEFT JOIN BILRG_GroupSpesialis ff ON ee.GroupSpesialisId = ff.GroupSpesialisId
            WHERE
               cc.fs_kd_layanan_dk = @LayananDkId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@LayananDkId", lynDk.LayananDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<JadwalPraktekDto>(sql, dp);
        return result;
    }

    public IEnumerable<JadwalPraktekDto> ListData(IGroupSpesialisKey grpSpesialis)
    {
        const string sql = """
            SELECT
               aa.JadwalPraktekId, aa.DokterId, aa.LayananId, 
               aa.Hari, aa.JamMulai, aa.JamSelesai, aa.MaxPasien,
               ISNULL(bb.fs_nm_peg, '-') AS DokterName,
               ISNULL(cc.fs_nm_layanan, '-') AS LayananName,
               ISNULL(cc.fs_kd_layanan_dk,'') AS LayananDkId,
               ISNULL(dd.fs_nm_layanan_dk,'') AS LayananDkName,
               ISNULL(ee.GroupSpesialisId,'') AS GroupSpesialisId,
               ISNULL(ff.GroupSpesialisName,'') AS GroupSpesialisName
            FROM 
               BILRG_JadwalPraktek aa
               LEFT JOIN td_peg bb ON aa.DokterId = bb.fs_kd_peg
               LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
               LEFT JOIN ta_layanan_dk dd ON cc.fs_kd_layanan_dk = dd.fs_kd_layanan_dk
               LEFT JOIN td_peg2 ee ON aa.DokterId = ee.fs_kd_peg 
               LEFT JOIN BILRG_GroupSpesialis ff ON ee.GroupSpesialisId = ff.GroupSpesialisId
            WHERE
               ee.GroupSpesialisId = @GroupSpesialisId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@GroupSpesialisId", grpSpesialis.GroupSpesialisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<JadwalPraktekDto>(sql, dp);
        return result;
    }
}
