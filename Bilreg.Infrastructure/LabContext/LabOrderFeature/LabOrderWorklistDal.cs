using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public class LabOrderWorklistDal : ILabOrderWorklistDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private readonly DatabaseOptions _opt;

    public LabOrderWorklistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<LabOrderWorklistView> List(LabOrderWorklistFilter filter)
    {
        var sql = """
            SELECT
                aa.OrderId, aa.OrderNo, aa.LabOrderStatus, aa.OrderSource,
                aa.PatientId, aa.PatientName, aa.Gender, aa.AgeAtOrder,
                aa.FinancialClearance, aa.OwareStatus, aa.CrtDate, aa.BillingTindakanId,
                (SELECT COUNT(*) FROM BILRG_LabOrderItem bb WHERE bb.OrderId = aa.OrderId) AS ItemCount,
                (
                    SELECT STRING_AGG(bb.TestName, ', ') WITHIN GROUP (ORDER BY bb.ItemNo)
                    FROM BILRG_LabOrderItem bb
                    WHERE bb.OrderId = aa.OrderId
                ) AS TestNamesCsv
            FROM BILRG_LabOrder aa
            WHERE aa.VodDate = @VodDate
            """;

        if (filter.LabOrderStatus.HasValue)
            sql += " AND aa.LabOrderStatus = @LabOrderStatus";

        if (filter.Date1.HasValue && filter.Date2.HasValue)
            sql += " AND aa.CrtDate >= @Date1 AND aa.CrtDate < @Date2End";

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            sql += """
                 AND (
                     aa.OrderNo LIKE @SearchTerm
                     OR aa.PatientName LIKE @SearchTerm
                     OR aa.PatientId LIKE @SearchTerm
                 )
                """;

        sql += " ORDER BY aa.CrtDate DESC";

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);

        if (filter.LabOrderStatus.HasValue)
            dp.AddParam("@LabOrderStatus", filter.LabOrderStatus.Value, SqlDbType.Int);

        if (filter.Date1.HasValue && filter.Date2.HasValue)
        {
            dp.AddParam("@Date1", filter.Date1.Value.Date, SqlDbType.DateTime);
            dp.AddParam("@Date2End", filter.Date2.Value.Date.AddDays(1), SqlDbType.DateTime);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            dp.AddParam("@SearchTerm", $"%{filter.SearchTerm.Trim()}%", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<LabOrderWorklistRowDto>(sql, dp);
        return rows?.Select(ToView) ?? [];
    }

    private static LabOrderWorklistView ToView(LabOrderWorklistRowDto row)
    {
        IReadOnlyList<string>? testNames = null;
        if (!string.IsNullOrWhiteSpace(row.TestNamesCsv))
            testNames = row.TestNamesCsv.Split(", ", StringSplitOptions.RemoveEmptyEntries);

        return new LabOrderWorklistView(
            row.OrderId,
            row.OrderNo,
            row.LabOrderStatus,
            row.OrderSource,
            row.PatientId,
            row.PatientName,
            row.Gender,
            row.AgeAtOrder,
            row.ItemCount,
            row.FinancialClearance,
            row.OwareStatus,
            row.CrtDate,
            row.BillingTindakanId,
            testNames);
    }

    private sealed record LabOrderWorklistRowDto(
        string OrderId,
        string OrderNo,
        int LabOrderStatus,
        int OrderSource,
        string PatientId,
        string PatientName,
        string Gender,
        int AgeAtOrder,
        int FinancialClearance,
        int OwareStatus,
        DateTime CrtDate,
        string BillingTindakanId,
        int ItemCount,
        string? TestNamesCsv)
    {
        public static LabOrderWorklistRowDto Default() => new LabOrderWorklistRowDto(
            string.Empty, string.Empty, 0, 0, string.Empty, string.Empty, string.Empty, 
            0, 0, 0, VoidSentinel, string.Empty, 0, string.Empty);
    };
}
