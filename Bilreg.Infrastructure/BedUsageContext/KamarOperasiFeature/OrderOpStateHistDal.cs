using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IOrderOpStateHistDal :
    IInsertBulk<OrderOpStateHistDto>,
    IDelete<IOrderOpKey>,
    IListData<OrderOpStateHistDto, IOrderOpKey>
{
}
public class OrderOpStateHistDal : IOrderOpStateHistDal
{
    private readonly DatabaseOptions _opt;

    public OrderOpStateHistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<OrderOpStateHistDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("OrderOpId", "OrderOpId");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("OrderOpState", "OrderOpState");
        bcp.AddMap("StateTimestamp", "StateTimestamp");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_OrderOpStateHist";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IOrderOpKey key)
    {
        const string sql = """
            DELETE FROM BILRG_OrderOpStateHist
            WHERE OrderOpId = @OrderOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, System.Data.SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<OrderOpStateHistDto> ListData(IOrderOpKey filter)
    {
        const string sql = """
            SELECT 
                OrderOpId,
                NoUrut,
                OrderOpState,
                StateTimestamp
            FROM BILRG_OrderOpStateHist
            WHERE OrderOpId = @OrderOpId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", filter.OrderOpId, System.Data.SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderOpStateHistDto>(sql, dp);
    }
}
