using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public interface IAntrianMapDal : 
    IInsertBulk<AntrianMapDto>,
    IDelete<IAntrianMapKey>,
    IListData<AntrianMapDto, IAntrianMapKey>
{
}
    
public class AntrianMapDal : IAntrianMapDal
{
    private readonly DatabaseOptions _opt;

    public AntrianMapDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<AntrianMapDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("fs_kd_dokter", "fs_kd_dokter");
        bcp.AddMap("fs_kd_layanan", "fs_kd_layanan");
        bcp.AddMap("fd_tgl_jadwal", "fd_tgl_jadwal");
        bcp.AddMap("fs_jam_jadwal", "fs_jam_jadwal");
        bcp.AddMap("fn_no_antrian", "fn_no_antrian");
        bcp.AddMap("fs_flag", "fs_flag");
        bcp.AddMap("fs_mr", "fs_mr");
        bcp.AddMap("fs_nm_pasien", "fs_nm_pasien");
        bcp.AddMap("fs_kd_trs_gen", "fs_kd_trs_gen");


        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_no_antrian_map";
        bcp.WriteToServer(fetched.AsDataTable());
    }
    public void Delete(IAntrianMapKey key)
    {
        const string sql = """
            DELETE FROM
                ta_no_antrian_map
            WHERE 
            	aa.fd_tgl_jadwal = @tglJadwal
            	AND aa.fs_kd_dokter = @dokterId
            	AND aa.fs_kd_layanan = @layananId
            	AND aa.fs_jam_jadwal = @jamJadwal
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@tglJadwal", key.TglPraktek.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@dokterId", key.PpaId, SqlDbType.VarChar);
        dp.AddParam("@layananId", key.LayananId, SqlDbType.VarChar);
        dp.AddParam("@jamJadwal", key.JamPraktek.ToString("HH:mm"), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    public IEnumerable<AntrianMapDto> ListData(IAntrianMapKey antrianMapKey)
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
        dp.AddParam("@tglJadwal", antrianMapKey.TglPraktek.ToString("yyyy-MM-dd") , SqlDbType.VarChar);
        dp.AddParam("@dokterId", antrianMapKey.PpaId , SqlDbType.VarChar);
        dp.AddParam("@layananId", antrianMapKey.LayananId, SqlDbType.VarChar);
        dp.AddParam("@jamJadwal", antrianMapKey.JamPraktek.ToString("HH:mm") , SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianMapDto>(sql, dp);
    }

    
}
