using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IOpCasePpaDal:
    IInsertBulk<OpCasePpaDto>,
    IDelete<IOrderOpKey>,
    IListData<OpCasePpaDto, IOrderOpKey>
{
}

public class OpCasePpaDal : IOpCasePpaDal
{
    private readonly DatabaseOptions _opt;

    public OpCasePpaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<OpCasePpaDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("OrderOpId", "OrderOpId");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("PpaId", "PpaId");
        bcp.AddMap("Role", "Role");
        bcp.AddMap("AssignDate", "AssignDate");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_OpCasePpa";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IOrderOpKey key)
    {
        const string sql = """
            DELETE FROM
                BILRG_OpCasePpa
            WHERE
                OrderOpId = @OrderOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    
    }

    public IEnumerable<OpCasePpaDto> ListData(IOrderOpKey filter)
    {
        const string sql = """
            SELECT 
                aa.OrderOpId, aa.NoUrut, aa.PpaId, aa.Role, aa.AssignDate,
                ISNULL(bb.fs_nm_peg, '') AS PpaName
            FROM 
                BILRG_OpCasePpa aa
                LEFT JOIN td_peg bb ON aa.PpaId = bb.fs_kd_peg
            WHERE
                aa.OrderOpId = @OrderOpId
            ORDER BY aa.NoUrut
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", filter.OrderOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OpCasePpaDto>(sql, dp);
    }
}