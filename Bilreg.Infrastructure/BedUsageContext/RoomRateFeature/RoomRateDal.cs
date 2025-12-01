using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.RoomRateFeature;

public interface IRoomRateKomponenDal:
    IInsertBulk<RoomRateDto>,
    IDelete<IKamarKey>,
    IListData<RoomRateDto, IKamarKey>
{
}

public class RoomRateKomponenDal : IRoomRateKomponenDal
{
    private readonly DatabaseOptions _opt;

    public RoomRateKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<RoomRateDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("fs_kd_kamar", "fs_kd_kamar");
        bcp.AddMap("fs_kd_detil_tarif", "fs_kd_detil_tarif");
        bcp.AddMap("fs_kd_tipe_kamar", "fs_kd_tipe_kamar");
        bcp.AddMap("fn_tarif", "fn_tarif");
        bcp.AddMap("fn_no_urut", "fn_no_urut");
        bcp.AddMap("fs_kd_kelas", "fs_kd_kelas");
        bcp.AddMap("fn_harike", "fn_harike");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_kamar2";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IKamarKey key)
    {
        const string sql = """
            DELETE FROM
                ta_kamar2
            WHERE
                fs_kd_kamar = @fs_kd_kamar
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar", key.KamarId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    
    }

    public IEnumerable<RoomRateDto> ListData(IKamarKey filter)
    {
        const string sql = """
            SELECT 
                aa.fs_kd_kamar, aa.fs_kd_detil_tarif, aa.fs_kd_tipe_kamar, aa.fn_tarif, aa.fn_no_urut,
                aa.fs_kd_kelas, aa.fn_harike,
                ISNULL(bb.fs_nm_kamar, '') AS fs_nm_kamar,
                ISNULL(cc.fs_nm_detil_tarif, '') AS fs_nm_detil_tarif,
                ISNULL(dd.fs_nm_kamar_tipe, '') AS fs_nm_tipe_kamar,
                ISNULL(ee.fs_nm_kelas, '') AS fs_nm_kelas
            FROM 
                ta_kamar2 aa
                LEFT JOIN ta_kamar bb ON aa.fs_kd_kamar = bb.fs_kd_kamar
                LEFT JOIN ta_detil_tarif cc ON aa.fs_kd_detil_tarif = cc.fs_kd_detil_tarif
                LEFT JOIN ta_kamar_tipe dd ON aa.fs_kd_tipe_kamar = dd.fs_kd_kamar_tipe
                LEFT JOIN ta_kelas ee ON aa.fs_kd_kelas = ee.fs_kd_kelas
            WHERE
                aa.fs_kd_kamar = @fs_kd_kamar
            ORDER BY aa.fn_no_urut
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kamar", filter.KamarId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomRateDto>(sql, dp);
    }
}