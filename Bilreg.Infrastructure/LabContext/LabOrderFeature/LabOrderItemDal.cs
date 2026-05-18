using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public interface ILabOrderItemDal :
    IInsertBulk<LabOrderItemDto>,
    IDelete<ILabOrderKey>,
    IListData<LabOrderItemDto, ILabOrderKey>
{
}

public class LabOrderItemDal : ILabOrderItemDal
{
    private readonly DatabaseOptions _opt;

    public LabOrderItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<LabOrderItemDto> listModel)
    {
        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("OrderId", "OrderId");
        bcp.AddMap("ItemNo", "ItemNo");
        bcp.AddMap("TestId", "TestId");
        bcp.AddMap("TestCode", "TestCode");
        bcp.AddMap("TestName", "TestName");
        bcp.AddMap("TarifId", "TarifId");
        bcp.AddMap("TarifCode", "TarifCode");
        bcp.AddMap("TarifName", "TarifName");
        bcp.AddMap("TubeType", "TubeType");
        bcp.AddMap("SpecimenType", "SpecimenType");
        bcp.AddMap("RequiredTubeCount", "RequiredTubeCount");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_LabOrderItem";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(ILabOrderKey key)
    {
        const string sql = """
            DELETE FROM BILRG_LabOrderItem
            WHERE OrderId = @OrderId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", key.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<LabOrderItemDto> ListData(ILabOrderKey filter)
    {
        const string sql = """
            SELECT
                aa.OrderId, aa.ItemNo,
                aa.TestId, aa.TestCode, aa.TestName,
                aa.TarifId, aa.TarifCode, aa.TarifName,
                aa.TubeType, aa.SpecimenType, aa.RequiredTubeCount
            FROM BILRG_LabOrderItem aa
            WHERE aa.OrderId = @OrderId
            ORDER BY aa.ItemNo
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", filter.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabOrderItemDto>(sql, dp);
    }
}
