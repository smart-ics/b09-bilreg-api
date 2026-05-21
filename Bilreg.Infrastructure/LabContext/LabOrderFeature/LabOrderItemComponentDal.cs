using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public interface ILabOrderItemComponentDal :
    IInsertBulk<LabOrderItemComponentDto>,
    IDelete<ILabOrderKey>,
    IListData<LabOrderItemComponentDto, ILabOrderKey>
{
}

public class LabOrderItemComponentDal : ILabOrderItemComponentDal
{
    private readonly DatabaseOptions _opt;

    public LabOrderItemComponentDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<LabOrderItemComponentDto> listModel)
    {
        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("OrderId", "OrderId");
        bcp.AddMap("ItemNo", "ItemNo");
        bcp.AddMap("ComponentNo", "ComponentNo");
        bcp.AddMap("ComponentId", "ComponentId");
        bcp.AddMap("ComponentCode", "ComponentCode");
        bcp.AddMap("ComponentName", "ComponentName");
        bcp.AddMap("ResultType", "ResultType");
        bcp.AddMap("Unit", "Unit");
        bcp.AddMap("ReferenceRangeText", "ReferenceRangeText");
        bcp.AddMap("SequenceNo", "SequenceNo");
        bcp.AddMap("IsMandatory", "IsMandatory");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_LabOrderItemComponent";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(ILabOrderKey key)
    {
        const string sql = """
            DELETE FROM BILRG_LabOrderItemComponent
            WHERE OrderId = @OrderId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", key.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<LabOrderItemComponentDto> ListData(ILabOrderKey filter)
    {
        const string sql = """
            SELECT
                aa.OrderId, aa.ItemNo, aa.ComponentNo,
                aa.ComponentId, aa.ComponentCode, aa.ComponentName,
                aa.ResultType, aa.Unit, aa.ReferenceRangeText,
                aa.SequenceNo, aa.IsMandatory
            FROM BILRG_LabOrderItemComponent aa
            WHERE aa.OrderId = @OrderId
            ORDER BY aa.ItemNo, aa.ComponentNo
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", filter.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabOrderItemComponentDto>(sql, dp);
    }
}
