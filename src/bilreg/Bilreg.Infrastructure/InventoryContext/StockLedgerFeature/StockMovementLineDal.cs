using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;

public interface IStockMovementLineDal :
    IInsertBulk<StockMovementLineDto>,
    IListData<StockMovementLineDto, IStockMovementKey>
{
}

public class StockMovementLineDal : IStockMovementLineDal
{
    private readonly DatabaseOptions _opt;

    public StockMovementLineDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<StockMovementLineDto> listModel)
    {
        var list = listModel.ToList();
        if (list.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();

        bcp.AddMap("StockMovementId", "StockMovementId");
        bcp.AddMap("LineNo", "LineNo");
        bcp.AddMap("BrgId", "BrgId");
        bcp.AddMap("ReceiptSourceId", "ReceiptSourceId");
        bcp.AddMap("LayananId", "LayananId");
        bcp.AddMap("Direction", "Direction");
        bcp.AddMap("Quantity", "Quantity");
        bcp.AddMap("AmountPerUnit", "AmountPerUnit");
        bcp.AddMap("Origin", "Origin");
        bcp.AddMap("StockLayerId", "StockLayerId");
        bcp.AddMap("CrtUser", "CrtUser");
        bcp.AddMap("CrtDate", "CrtDate");
        bcp.AddMap("UpdUser", "UpdUser");
        bcp.AddMap("UpdDate", "UpdDate");
        bcp.AddMap("VodUser", "VodUser");
        bcp.AddMap("VodDate", "VodDate");

        bcp.BatchSize = list.Count;
        bcp.DestinationTableName = "BILRG_StokMovementLine";
        bcp.WriteToServer(list.AsDataTable());
    }

    public IEnumerable<StockMovementLineDto> ListData(IStockMovementKey filter)
    {
        const string sql = """
            SELECT
                aa.StockMovementId,
                aa.[LineNo] AS [LineNo],
                aa.BrgId, aa.ReceiptSourceId, aa.LayananId,
                aa.Direction, aa.Quantity, aa.AmountPerUnit,
                aa.Origin, aa.StockLayerId,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_StokMovementLine aa
            WHERE aa.StockMovementId = @StockMovementId
            ORDER BY aa.[LineNo]
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@StockMovementId", filter.StockMovementId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<StockMovementLineDto>(sql, dp) ?? [];
    }
}
