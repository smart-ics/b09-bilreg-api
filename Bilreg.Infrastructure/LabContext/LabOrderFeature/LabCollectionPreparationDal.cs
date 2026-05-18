using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public class LabCollectionPreparationDal : ILabCollectionPreparationDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private readonly DatabaseOptions _opt;

    public LabCollectionPreparationDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public LabCollectionPreparationView? Get(string orderId)
    {
        const string headerSql = """
            SELECT
                aa.OrderId, aa.OrderNo, aa.PatientId, aa.PatientName, aa.LabOrderStatus
            FROM BILRG_LabOrder aa
            WHERE aa.OrderId = @OrderId AND aa.VodDate = @VodDate
            """;

        const string itemsSql = """
            SELECT
                bb.ItemNo, bb.TestId, bb.TestCode, bb.TestName,
                bb.TubeType, bb.SpecimenType, bb.RequiredTubeCount
            FROM BILRG_LabOrderItem bb
            WHERE bb.OrderId = @OrderId
            ORDER BY bb.ItemNo
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", orderId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var header = conn.QueryFirstOrDefault<LabCollectionHeaderRow>(headerSql, dp);
        if (header is null)
            return null;

        var itemDp = new DynamicParameters();
        itemDp.AddParam("@OrderId", orderId, SqlDbType.VarChar);
        var itemRows = conn.Read<LabCollectionItemRow>(itemsSql, itemDp).ToList();

        var tests = itemRows
            .Select(r => new LabCollectionPreparationTestItem(
                r.ItemNo,
                r.TestId,
                r.TestCode,
                r.TestName,
                r.TubeType,
                r.SpecimenType,
                r.RequiredTubeCount))
            .ToList();

        var groups = itemRows
            .GroupBy(r => (r.TubeType, r.SpecimenType))
            .Select(g => new LabCollectionPreparationVacutainerGroup(
                g.Key.TubeType,
                g.Key.SpecimenType,
                g.Max(x => x.RequiredTubeCount)))
            .OrderBy(g => g.TubeType)
            .ThenBy(g => g.SpecimenType, StringComparer.Ordinal)
            .ToList();

        return new LabCollectionPreparationView(
            header.OrderId,
            header.OrderNo,
            header.PatientId,
            header.PatientName,
            header.LabOrderStatus,
            tests,
            groups);
    }

    private sealed record LabCollectionHeaderRow(
        string OrderId,
        string OrderNo,
        string PatientId,
        string PatientName,
        int LabOrderStatus);

    private sealed record LabCollectionItemRow(
        int ItemNo,
        string TestId,
        string TestCode,
        string TestName,
        int TubeType,
        string SpecimenType,
        int RequiredTubeCount);
}
