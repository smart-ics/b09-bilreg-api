using Bilreg.Domain.BedUsageContext.RoomChargeFeatue;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.BedUsageContext.RoomChargeFeature;

public interface IRoomChargeKomponenDal :
    IInsertBulk<RoomChargeKomponenDto>,
    IDelete<IRoomChargeKey>,
    IListData<RoomChargeKomponenDto, IRoomChargeKey>
{
}

public class RoomChargeKomponenDal : IRoomChargeKomponenDal
{
    private readonly DatabaseOptions _opt;
    public RoomChargeKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(IEnumerable<RoomChargeKomponenDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("fs_kd_trs", "fs_kd_trs");
        bcp.AddMap("fs_kd_detil_tarif", "fs_kd_detil_tarif");
        bcp.AddMap("fn_tarif", "fn_tarif");
        bcp.AddMap("fn_diskon", "fn_diskon");
        bcp.AddMap("fn_total", "fn_total");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "ta_trs_roomcharge2";
        bcp.WriteToServer(fetched.AsDataTable());
    }
    
    public void Delete(IRoomChargeKey key)
    {
        const string sql = """
           DELETE FROM
               ta_trs_roomcharge2
           WHERE
               fs_kd_trs = @fs_kd_trs
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.RoomChargeId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<RoomChargeKomponenDto> ListData(IRoomChargeKey filter)
    {
        const string sql = """
           SELECT  
           	aa.fs_kd_trs, aa.fs_kd_detil_tarif, aa.fn_tarif, aa.fn_diskon, aa.fn_total,
           	ISNULL(bb.fs_nm_detil_tarif,'-') AS fs_nm_detil_tarif
           FROM 
           	ta_trs_roomcharge2 aa 
           	LEFT JOIN ta_detil_tarif bb ON aa.fs_kd_detil_tarif = bb.fs_kd_detil_tarif
           WHERE aa.fs_kd_trs = @fs_kd_trs
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", filter.RoomChargeId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomChargeKomponenDto>(sql, dp);
    }

    
}
