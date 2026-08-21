using Bilreg.Domain.BedUsageContext.RoomChargeFeature;
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
        var rows = listModel.ToList();
        if (rows.Count == 0)
            return;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        
        var dataTable = rows.AsDataTable();
        var destTable = "ta_trs_roomcharge2";

        var dbColumns = SqlBulkHelper.GetTableColumns(conn, destTable);

        using var bcp = new SqlBulkCopy(conn)
        {
            BatchSize = rows.Count,
            DestinationTableName = destTable
        };

        SqlBulkHelper.MapColumnsCaseInsensitive(bcp, dataTable, dbColumns);
        bcp.WriteToServer(dataTable);
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
