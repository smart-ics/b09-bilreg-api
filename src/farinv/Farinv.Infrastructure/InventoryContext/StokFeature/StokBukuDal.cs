using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.InventoryContext.StokFeature;

public interface IStokBukuDal :
    IInsertBulk<StokBukuDto>,
    IDelete<IStokLayerKey>,
    IListData<StokBukuDto, IStokLayerKey>
{
}

public class StokBukuDal : IStokBukuDal
{
    private readonly DatabaseOptions _opt;
    public StokBukuDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(IEnumerable<StokBukuDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();
        bcp.AddMap("StokBukuId", "StokBukuId");
        bcp.AddMap("StokLayerId", "StokLayerId");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("BrgId", "BrgId");
        bcp.AddMap("LayananId", "LayananId");
        bcp.AddMap("PurchaseId", "PurchaseId");
        bcp.AddMap("ReceiveId", "ReceiveId");
        bcp.AddMap("ExpDate", "ExpDate");
        bcp.AddMap("BatchNo", "BatchNo");
        bcp.AddMap("QtyIn", "QtyIn");
        bcp.AddMap("QtyOut", "QtyOut");
        bcp.AddMap("Hpp", "Hpp");
        bcp.AddMap("TrsReffId", "TrsReffId");
        bcp.AddMap("TrsReffDate", "TrsReffDate");
        bcp.AddMap("UseCase", "UseCase");
        bcp.AddMap("EntryDate", "EntryDate");
        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "FARIN_StokBuku";
        bcp.WriteToServer(fetched.AsDataTable());
    }
    
    public void Delete(IStokLayerKey key)
    {
        const string sql = """
            DELETE FROM
                FARIN_StokBuku
            WHERE
                StokLayerId = @StokLayerId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@StokLayerId", key.StokLayerId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    
    }
    
    public IEnumerable<StokBukuDto> ListData(IStokLayerKey filter)
    {
        const string sql = """
            SELECT 
                StokBukuId, StokLayerId, NoUrut,
                BrgId, LayananId,
                PurchaseId, ReceiveId, ExpDate, BatchNo,
                QtyIn, QtyOut, Hpp,
                TrsReffId, TrsReffDate, UseCase,
                EntryDate
            FROM 
                FARIN_StokBuku
            WHERE
                StokLayerId = @StokLayerId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@StokLayerId", filter.StokLayerId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StokBukuDto>(sql, dp);
    }
}