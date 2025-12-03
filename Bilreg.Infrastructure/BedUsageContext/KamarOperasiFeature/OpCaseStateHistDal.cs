using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IOpCaseStateHistDal :
    IInsertBulk<OpCaseStateHistDto>,
    IDelete<IOrderOpKey>,
    IListData<OpCaseStateHistDto, IOrderOpKey>
{
}

public class OpCaseStateHistDal : IOpCaseStateHistDal
{
    private readonly DatabaseOptions _opt;

    public OpCaseStateHistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<OpCaseStateHistDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("OrderOpId", "OrderOpId");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("OpCaseState", "OpCaseState");
        bcp.AddMap("StateTimestamp", "StateTimestamp");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_OpCaseStateHist";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IOrderOpKey key)
    {
        const string sql = """
            DELETE FROM
                BILRG_OpCaseStateHist
            WHERE
                OrderOpId = @OrderOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    
    }

    public IEnumerable<OpCaseStateHistDto> ListData(IOrderOpKey filter)
    {
        const string sql = """
            SELECT 
                OrderOpId, NoUrut, OpCaseState, StateTimestamp
            FROM 
                BILRG_OpCaseStateHist
            WHERE
                OrderOpId = @OrderOpId
            ORDER BY NoUrut
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", filter.OrderOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OpCaseStateHistDto>(sql, dp);
    }
}

