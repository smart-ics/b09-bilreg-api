using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public interface IAntrianMapDal : 
    IInsert<AntrianMapDetilDto>,
    IDelete<IAntrianMapKey>,
    IListData<AntrianMapDetilDto, IAntrianMapKey>,
    IListData<AntrianMapDetilDto, DateTime>
{
}
    
public class AntrianMapDetilDal : IAntrianMapDal
{
    private readonly DatabaseOptions _opt;

    public AntrianMapDetilDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AntrianMapDetilDto detilDto)
    {
        const string sql = """
             INSERT INTO ta_no_antrian_map(
                 fs_kd_antrian_map, fs_kd_dokter, fs_kd_layanan, 
                 fd_tgl_jadwal, fs_jam_jadwal, fn_no_antrian,
                 fs_flag, fs_mr, fs_nm_pasien, fs_kd_trs_gen,
                 fb_terpakai)
             VALUES(
                 @fs_kd_antrian_map, @fs_kd_dokter, @fs_kd_layanan, 
                 @fd_tgl_jadwal, @fs_jam_jadwal, @fn_no_antrian,
                 @fs_flag, @fs_mr, @fs_nm_pasien, @fs_kd_trs_gen,
                 @fb_terpakai)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_antrian_map", detilDto.fs_kd_antrian_map, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_dokter", detilDto.fs_kd_dokter, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", detilDto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_jadwal", detilDto.fd_tgl_jadwal, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_jadwal", detilDto.fs_jam_jadwal, SqlDbType.VarChar);
        dp.AddParam("@fn_no_antrian", detilDto.fn_no_antrian, SqlDbType.Decimal);
        dp.AddParam("@fs_flag", detilDto.fs_flag, SqlDbType.VarChar);
        dp.AddParam("@fs_mr", detilDto.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", detilDto.fs_nm_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_trs_gen", detilDto.fs_kd_trs_gen, SqlDbType.VarChar);
        dp.AddParam("@fb_terpakai", detilDto.fb_terpakai, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);


    }
    public void Delete(IAntrianMapKey key)
    {
        const string sql = """
            DELETE FROM
                ta_no_antrian_map
            WHERE 
            	fs_kd_antrian_map = @fs_kd_antrian_map
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_antrian_map", key.AntrianMapId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    public IEnumerable<AntrianMapDetilDto> ListData(IAntrianMapKey antrianMapKey)
    {
        const string sql = """
            SELECT 
            	aa.fs_kd_antrian_map, aa.fs_kd_dokter, aa.fs_kd_layanan, aa.fd_tgl_jadwal, aa.fs_jam_jadwal,
            	aa.fn_no_antrian, aa.fs_flag, aa.fs_mr, aa.fs_nm_pasien, aa.fs_kd_trs_gen,
                aa.fb_terpakai,
            	ISNULL(bb.fs_nm_peg,'') AS fs_nm_dokter,
            	ISNULL(cc.fs_nm_layanan,'') AS fs_nm_layanan
            FROM 
            	ta_no_antrian_map aa
            	LEFT JOIN td_peg bb ON aa.fs_kd_dokter = bb.fs_kd_peg
            	LEFT JOIN ta_layanan cc ON aa.fs_kd_layanan = cc.fs_kd_layanan
            WHERE 
            	aa.fs_kd_antrian_map = @fs_kd_antrian_map
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_antrian_map", antrianMapKey.AntrianMapId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianMapDetilDto>(sql, dp);
    }

    public IEnumerable<AntrianMapDetilDto> ListData(DateTime date)
    {
        const string sql = """
            SELECT 
            	aa.fs_kd_antrian_map, aa.fs_kd_dokter, aa.fs_kd_layanan, aa.fd_tgl_jadwal, aa.fs_jam_jadwal,
            	aa.fn_no_antrian, aa.fs_flag, aa.fs_mr, aa.fs_nm_pasien, aa.fs_kd_trs_gen,
                aa.fb_terpakai,
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

        dp.AddParam("@tgl1", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), SqlDbType.VarChar);
        dp.AddParam("@@tgl2", tgl2.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianMapDetilDto>(sql, dp);
    }
}
