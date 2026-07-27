using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.InventoryContext.StokFeature;

public interface IStokLayerDal:
    IInsertBulk<StokLayerDto>,
    IDelete<IStokKey>,
    IListData<StokLayerDto, IStokKey>
{
}

public class StokLayerDal : IStokLayerDal
{
    private readonly DatabaseOptions _opt;
    public StokLayerDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(IEnumerable<StokLayerDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();
        
        bcp.AddMap("StokLayerId", "StokLayerId");
        bcp.AddMap("BrgId", "BrgId");
        bcp.AddMap("LayananId", "LayananId");
        
        bcp.AddMap("PurchaseId", "PurchaseId");
        bcp.AddMap("ReceiveId", "ReceiveId");
        bcp.AddMap("ExpDate", "ExpDate");
        bcp.AddMap("BatchNo", "BatchNo");
        
        bcp.AddMap("QtyIn", "QtyIn");
        bcp.AddMap("QtySisa", "QtySisa");
        bcp.AddMap("Hpp", "Hpp");
        
        bcp.AddMap("TrsReffInId", "TrsReffInId");
        bcp.AddMap("TrsReffInDate", "TrsReffInDate");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "FARIN_StokLayer";
        bcp.WriteToServer(fetched.AsDataTable());
    }
    
    public void Delete(IStokKey key)
    {
        const string sql = """
            DELETE FROM
                FARIN_StokLayer
            WHERE
                BrgId = @BrgId AND LayananId = @LayananId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", key.BrgId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", key.LayananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    
    }
    
    public IEnumerable<StokLayerDto> ListData(IStokKey filter)
    {
        const string sql = """
            SELECT 
                aa.StokLayerId, aa.BrgId, aa.LayananId,
                aa.PurchaseId, aa.ReceiveId, aa.ExpDate, aa.BatchNo,
                aa.QtyIn, aa.QtySisa, aa.Hpp,
                aa.TrsReffInId, aa.TrsReffInDate,
                ISNULL(bb.fs_nm_barang, '') BrgName,
                ISNULL(cc.fs_nm_layanan, '') LayananName
            FROM 
                FARIN_StokLayer aa
                LEFT JOIN tb_barang bb ON aa.BrgId = bb.fs_kd_barang
                LEFT JOIN ta_layanan cc ON aa.LayananId = cc.fs_kd_layanan
            WHERE
                aa.BrgId = @BrgId 
                AND aa.LayananId = @LayananId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", filter.BrgId, SqlDbType.VarChar);
        dp.AddParam("@LayananId", filter.LayananId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StokLayerDto>(sql, dp);
    }
}