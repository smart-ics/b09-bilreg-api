using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public interface IAntrianMapDal : 
    IInsert<AntrianMapDto>,
    IDelete<IAntrianMapHdrKey>,
    IListData<AntrianMapDto, IAntrianMapHdrKey>,
    IListData<AntrianMapDto, DateTime>
{
}
    
public class AntrianMapDal : IAntrianMapDal
{
    private readonly DatabaseOptions _opt;

    public AntrianMapDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AntrianMapDto dto)
    {
        const string sql = """
             INSERT INTO ta_no_antrian_map(
                 fs_kd_dokter, fs_kd_layanan, 
                 fd_tgl_jadwal, fs_jam_jadwal, fn_no_antrian,
                 fs_flag, fs_mr, fs_nm_pasien, fs_kd_trs_gen)
             VALUES(
                 @fs_kd_dokter, @fs_kd_layanan, 
                 @fd_tgl_jadwal, @fs_jam_jadwal, @fn_no_antrian,
                 @fs_flag, @fs_mr, @fs_nm_pasien, @fs_kd_trs_gen)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_dokter", dto.fs_kd_dokter, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_jadwal", dto.fd_tgl_jadwal, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_jadwal", dto.fs_jam_jadwal, SqlDbType.VarChar);
        dp.AddParam("@fn_no_antrian", dto.fn_no_antrian, SqlDbType.Decimal);
        dp.AddParam("@fs_flag", dto.fs_flag, SqlDbType.VarChar);
        dp.AddParam("@fs_mr", dto.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", dto.fs_nm_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_trs_gen", dto.fs_kd_trs_gen, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);


    }
    public void Delete(IAntrianMapHdrKey key)
    {
        const string sql = """
            DELETE FROM
                ta_no_antrian_map
            WHERE 
            	fd_tgl_jadwal = @tglJadwal
            	AND fs_kd_dokter = @dokterId
            	AND fs_kd_layanan = @layananId
            	AND fs_jam_jadwal = @jamJadwal
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@tglJadwal", key.TglJadwal.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@dokterId", key.DokterId, SqlDbType.VarChar);
        dp.AddParam("@layananId", key.LayananId, SqlDbType.VarChar);
        dp.AddParam("@jamJadwal", key.JamJadwal.ToString("HH:mm"), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    public IEnumerable<AntrianMapDto> ListData(IAntrianMapHdrKey antrianMapKey)
    {
        const string sql = """
            SELECT 
            	aa.fs_kd_dokter, aa.fs_kd_layanan, aa.fd_tgl_jadwal, aa.fs_jam_jadwal,
            	aa.fn_no_antrian, aa.fs_flag, aa.fs_mr, aa.fs_nm_pasien, aa.fs_kd_trs_gen,
            	ISNULL(bb.fs_nm_peg,'') AS fs_nm_dokter,
            	ISNULL(cc.fs_nm_layanan,'') AS fs_nm_layanan
            FROM 
            	ta_no_antrian_map aa
            	LEFT JOIN td_peg bb ON aa.fs_kd_dokter = bb.fs_kd_peg
            	LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan
            WHERE 
            	aa.fd_tgl_jadwal = @tglJadwal
            	AND aa.fs_kd_dokter = @dokterId
            	AND aa.fs_kd_layanan = @layananId
            	AND aa.fs_jam_jadwal = @jamJadwal
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@tglJadwal", antrianMapKey.TglJadwal.ToString("yyyy-MM-dd") , SqlDbType.VarChar);
        dp.AddParam("@dokterId", antrianMapKey.DokterId , SqlDbType.VarChar);
        dp.AddParam("@layananId", antrianMapKey.LayananId, SqlDbType.VarChar);
        dp.AddParam("@jamJadwal", antrianMapKey.JamJadwal.ToString("HH:mm") , SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianMapDto>(sql, dp);
    }

    public IEnumerable<AntrianMapDto> ListData(DateTime date)
    {
        const string sql = """
            SELECT 
            	aa.fs_kd_dokter, aa.fs_kd_layanan, aa.fd_tgl_jadwal, aa.fs_jam_jadwal,
            	aa.fn_no_antrian, aa.fs_flag, aa.fs_mr, aa.fs_nm_pasien, aa.fs_kd_trs_gen,
            	ISNULL(bb.fs_nm_peg,'') AS fs_nm_dokter,
            	ISNULL(cc.fs_nm_layanan,'') AS fs_nm_layanan
            FROM 
            	ta_no_antrian_map aa
            	LEFT JOIN td_peg bb ON aa.fs_kd_dokter = bb.fs_kd_peg
            	LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan
            WHERE 
            	aa.fd_tgl_jadwal BETWEEN @tgl1 AND @tgl2
            """;

        var tgl2 = date.AddMonths(3);
        var dp = new DynamicParameters();

        dp.AddParam("@tgl1", date.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@@tgl2", tgl2.ToString("yyyy-MM-dd"), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianMapDto>(sql, dp);
    }
}
