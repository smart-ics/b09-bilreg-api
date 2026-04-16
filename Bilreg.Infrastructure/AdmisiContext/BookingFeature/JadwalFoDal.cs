using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data.SqlClient;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IJadwalFoDal :
    IListData<JadwalFoDto>
{ }
public class JadwalFoDal : IJadwalFoDal
{
    private readonly DatabaseOptions _opt;

    public JadwalFoDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<JadwalFoDto> ListData()
    {
        const string sql = """
            SELECT 
            	'JADW' + Format(rank() over(order by aa.fs_kd_dokter, aa.fs_kd_layanan, aa.fs_kd_fruang, aa.fn_hari-1, aa.fs_jam_mulai),'000') as JadwalPraktekId,
            	aa.fs_kd_dokter AS DokterId,
               	aa.fs_kd_layanan AS LayananId,
            	aa.fs_kd_fruang AS RuangId,
               	aa.fn_hari - 1 AS Hari,
               	aa.fs_jam_mulai_real AS JamMulai,
               	aa.fs_jam_selesai AS JamSelesai,
               	aa.fn_max AS MaxPasien,
               	ISNULL(bb.fs_nm_peg,'') AS DokterName,
               	ISNULL(cc.fs_nm_layanan,'') AS LayananName,
               	ISNULL(cc.fs_kd_layanan_dk,'') AS LayananDkId,
               	ISNULL(dd.fs_nm_layanan_dk,'') AS LayananDkName,
               	ISNULL(ee.GroupSpesialisId, '') AS GroupSpesialisId,
               	ISNULL(ff.GroupSpesialisName,'') AS GroupSpesialisName,
            	ISNULL(gg.RuangName,'') AS RuangName,
            	ISNULL(gg.PrefixAntrian,'') AS PrefixAntrian
            FROM 
               	ta_jadwal_dokter aa
               	LEFT JOIN td_peg bb ON aa.fs_kd_dokter = bb.fs_kd_peg AND bb.fb_aktif_dinas = 1
               	LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan 
               	LEFT JOIN ta_layanan_Dk dd ON cc.fs_kd_layanan_dk = dd.fs_kd_layanan_dk 
               	LEFT JOIN td_peg2 ee ON aa.fs_kd_dokter = ee.fs_kd_peg 
               	LEFT JOIN BILRG_GroupSpesialis ff ON ee.GroupSpesialisId = ff.GroupSpesialisId
            	LEFT JOIN HiDok_Ruang gg ON aa.fs_kd_fruang = gg.RuangID 
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<JadwalFoDto>(sql);
        return result;
    }
}

public record JadwalFoDto(
    string JadwalPraktekId,
    string DokterId,
    string LayananId,
    string RuangId,
    decimal Hari,
    string JamMulai,
    string JamSelesai,
    decimal MaxPasien,
    string DokterName,
    string LayananName,
    string LayananDkId,
    string LayananDkName,
    string GroupSpesialisId,
    string GroupSpesialisName,
    string RuangName,
    string PrefixAntrian);