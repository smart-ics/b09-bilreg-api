using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public interface IAntrianMapHdrDal :
    IInsert<AntrianMapDto>,
    IUpdate<AntrianMapDto>,
    IGetData<AntrianMapDto, IAntrianMapKey>
{
    IEnumerable<AntrianMapDto> ListData(ILayananKey lynKey, IPpaKey ppaKey , DateOnly tglJadwal);
}
public class AntrianMapHdrDal : IAntrianMapHdrDal
{
    private readonly DatabaseOptions _opt;

    public AntrianMapHdrDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AntrianMapDto dto)
    {
        const string sql = """
             INSERT INTO ta_no_antrian_map_hdr(
                 fs_kd_antrian_map, fs_kd_jadwal, fs_kd_dokter, fs_kd_layanan, 
                 fd_tgl_jadwal, fs_jam_jadwal, fs_jam_praktek,
                 fs_pattern, fn_max)
             VALUES(
                 @fs_kd_antrian_map, @fs_kd_jadwal, @fs_kd_dokter, @fs_kd_layanan, 
                 @fd_tgl_jadwal, @fs_jam_jadwal, @fs_jam_praktek,
                 @fs_pattern, @fn_max)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_antrian_map", dto.fs_kd_antrian_map, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jadwal", dto.fs_kd_jadwal, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_dokter", dto.fs_kd_dokter, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_jadwal", dto.fd_tgl_jadwal, SqlDbType.DateTime);
        dp.AddParam("@fs_jam_jadwal", dto.fs_jam_jadwal, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_praktek", dto.fs_jam_praktek, SqlDbType.VarChar);
        dp.AddParam("@fs_pattern", dto.fs_pattern, SqlDbType.VarChar);
        dp.AddParam("@fn_max", dto.fn_max, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(AntrianMapDto dto)
    {
        const string sql = """
            UPDATE
                ta_no_antrian_map_hdr
            SET
                fs_jam_praktek = @fs_jam_praktek,
                fs_kd_jadwal = @fs_kd_jadwal, 
                fs_kd_dokter = @fs_kd_dokter, 
                fs_kd_layanan = @fs_kd_layanan, 
                fd_tgl_jadwal = @fd_tgl_jadwal, 
                fs_jam_jadwal = @fs_jam_jadwal, 
                fs_jam_praktek = @fs_jam_praktek,
                fs_pattern = @fs_pattern, 
                fn_max = @fn_max
            WHERE
                fs_kd_antrian_map = @fs_kd_antrian_map
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_antrian_map", dto.fs_kd_antrian_map, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jadwal", dto.fs_kd_jadwal, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_dokter", dto.fs_kd_dokter, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_jadwal", dto.fd_tgl_jadwal, SqlDbType.DateTime);
        dp.AddParam("@fs_jam_jadwal", dto.fs_jam_jadwal, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_praktek", dto.fs_jam_praktek, SqlDbType.VarChar);
        dp.AddParam("@fs_pattern", dto.fs_pattern, SqlDbType.VarChar);
        dp.AddParam("@fn_max", dto.fn_max, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public AntrianMapDto GetData(IAntrianMapKey key)
    {
        const string sql = """
            SELECT 
               	aa.fs_kd_antrian_map, aa.fs_kd_jadwal, aa.fs_kd_dokter, aa.fs_kd_layanan, 
                aa.fd_tgl_jadwal, aa.fs_jam_jadwal, aa.fs_jam_praktek, aa.fs_patterin, aa.fn_max,
            	ISNULL(bb.fs_nm_peg,'') AS fs_nm_dokter,
            	ISNULL(cc.fs_nm_layanan,'') AS fs_nm_layanan
            FROM 
               	ta_no_antrian_map_hdr aa
            	LEFT JOIN td_peg bb ON aa.fs_kd_dokter = fs_kd_peg
            	LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan
            WHERE 
            	aa.fs_kd_antrian_map = @fs_kd_antrian_map
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_antrian_map", key.AntrianMapId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<AntrianMapDto>(sql, dp);
    }

    public IEnumerable<AntrianMapDto> ListData(ILayananKey lynKey, IPpaKey ppaKey, DateOnly tglJadwal)
    {
        const string sql = """
            SELECT 
               	aa.fs_kd_antrian_map, aa.fs_kd_jadwal, aa.fs_kd_dokter, aa.fs_kd_layanan, 
                aa.fd_tgl_jadwal, aa.fs_jam_jadwal, aa.fs_jam_praktek, aa.fs_patterin, aa.fn_max,
            	ISNULL(bb.fs_nm_peg,'') AS fs_nm_dokter,
            	ISNULL(cc.fs_nm_layanan,'') AS fs_nm_layanan
            FROM 
               	ta_no_antrian_map_hdr aa
            	LEFT JOIN td_peg bb ON aa.fs_kd_dokter = fs_kd_peg
            	LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan
            WHERE 
            	aa.fs_kd_layanan = @fs_kd_layanan
                AND aa.fs_kd_dokter = @fs_kd_dokter
            	AND aa.fd_tgl_jadwal = @fd_tgl_jadwal
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan", lynKey.LayananId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_dokter", ppaKey.PpaId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_jadwal", tglJadwal.ToDateTime(TimeOnly.MinValue), SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianMapDto>(sql, dp);
    }
}



