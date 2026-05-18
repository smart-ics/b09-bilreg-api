using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

public interface ILabResultItemDal :
    IInsertBulk<LabResultItemDto>,
    IDelete<ILabResultDocumentKey>,
    IListData<LabResultItemDto, ILabResultDocumentKey>
{
}

public class LabResultItemDal : ILabResultItemDal
{
    private readonly DatabaseOptions _opt;

    public LabResultItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<LabResultItemDto> listModel)
    {
        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("ResultDocumentId", "ResultDocumentId");
        bcp.AddMap("ItemNo", "ItemNo");
        bcp.AddMap("TestId", "TestId");
        bcp.AddMap("TestName", "TestName");
        bcp.AddMap("ComponentCode", "ComponentCode");
        bcp.AddMap("ComponentName", "ComponentName");
        bcp.AddMap("ResultType", "ResultType");
        bcp.AddMap("NumericValue", "NumericValue");
        bcp.AddMap("TextValue", "TextValue");
        bcp.AddMap("OptionValue", "OptionValue");
        bcp.AddMap("NarrativeValue", "NarrativeValue");
        bcp.AddMap("Unit", "Unit");
        bcp.AddMap("ReferenceRangeText", "ReferenceRangeText");
        bcp.AddMap("FlagStatus", "FlagStatus");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_LabResultItem";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(ILabResultDocumentKey key)
    {
        const string sql = """
            DELETE FROM BILRG_LabResultItem
            WHERE ResultDocumentId = @ResultDocumentId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ResultDocumentId", key.ResultDocumentId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<LabResultItemDto> ListData(ILabResultDocumentKey filter)
    {
        const string sql = """
            SELECT
                aa.ResultDocumentId, aa.ItemNo,
                aa.TestId, aa.TestName,
                aa.ComponentCode, aa.ComponentName,
                aa.ResultType, aa.NumericValue,
                aa.TextValue, aa.OptionValue, aa.NarrativeValue,
                aa.Unit, aa.ReferenceRangeText, aa.FlagStatus
            FROM BILRG_LabResultItem aa
            WHERE aa.ResultDocumentId = @ResultDocumentId
            ORDER BY aa.ItemNo
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ResultDocumentId", filter.ResultDocumentId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabResultItemDto>(sql, dp);
    }
}
