using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabTestDefinitionFeature;

public interface ILabTestComponentDal :
    IInsertBulk<LabTestComponentDto>,
    IDelete<ILabTestDefinitionKey>,
    IListData<LabTestComponentDto, ILabTestDefinitionKey>
{
}

public class LabTestComponentDal : ILabTestComponentDal
{
    private readonly DatabaseOptions _opt;

    public LabTestComponentDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<LabTestComponentDto> listModel)
    {
        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("TestDefinitionId", "TestDefinitionId");
        bcp.AddMap("SequenceNo", "SequenceNo");
        bcp.AddMap("ComponentId", "ComponentId");
        bcp.AddMap("ReferenceRangeOverride", "ReferenceRangeOverride");
        bcp.AddMap("RequiredFlagging", "RequiredFlagging");
        bcp.AddMap("IsMandatory", "IsMandatory");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_LabTestComponent";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(ILabTestDefinitionKey key)
    {
        const string sql = """
            DELETE FROM BILRG_LabTestComponent
            WHERE TestDefinitionId = @TestDefinitionId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TestDefinitionId", key.TestDefinitionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<LabTestComponentDto> ListData(ILabTestDefinitionKey filter)
    {
        const string sql = """
            SELECT
                TestDefinitionId,
                SequenceNo,
                ComponentId,
                ReferenceRangeOverride,
                RequiredFlagging,
                IsMandatory
            FROM BILRG_LabTestComponent
            WHERE TestDefinitionId = @TestDefinitionId
            ORDER BY SequenceNo
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TestDefinitionId", filter.TestDefinitionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabTestComponentDto>(sql, dp);
    }
}
