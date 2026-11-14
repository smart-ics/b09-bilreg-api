using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabResultFeature;

public class LabResultVerificationWorklistDal : ILabResultVerificationWorklistDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private readonly DatabaseOptions _opt;

    public LabResultVerificationWorklistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<LabResultVerificationWorklistView> List(LabResultVerificationWorklistFilter filter)
    {
        const int orderStatusRecorded = (int)LabOrderStatusEnum.Recorded;
        const int resultStatusRecorded = (int)LabResultStatusEnum.Recorded;
        const int flagCritical = (int)LabResultFlagEnum.Critical;

        var sql = """
            SELECT
                o.OrderId,
                o.OrderNo,
                o.PatientId,
                o.PatientName,
                d.RecordedDate,
                d.ResultStatus,
                d.RecordedUserId,
                CAST(CASE WHEN o.Urgency = 1 THEN 1 ELSE 0 END AS BIT) AS IsCito,
                ISNULL(o.Gender, '') AS Gender,
                ISNULL(o.AgeAtOrder, 0) AS AgeAtOrder,
                ISNULL(o.LayananPerujukName, '') AS LayananPerujukName,
                ISNULL(o.DokterPerujukName, '') AS DokterPerujukName,
                ISNULL(STUFF((
                    SELECT ', ' + bb.LabTestName
                    FROM BILRG_LabOrderItem bb
                    WHERE bb.OrderId = o.OrderId
                    ORDER BY bb.ItemNo
                    FOR XML PATH(''), TYPE
                ).value('.', 'NVARCHAR(MAX)'), 1, 2, ''), '') AS TestNames,
                CAST(CASE WHEN EXISTS (
                    SELECT 1
                    FROM BILRG_LabResultItem i
                    WHERE i.ResultDocumentId = d.ResultDocumentId
                      AND i.FlagStatus = @FlagCritical
                ) THEN 1 ELSE 0 END AS BIT) AS HasCriticalValue,
                d.VersionNo,
                d.ResultSource
            FROM BILRG_LabOrder o
            INNER JOIN BILRG_LabResultDocument d ON d.OrderId = o.OrderId
            WHERE o.VodDate = @VodDate
              AND d.VodDate = @VodDate
              AND d.IsCurrentVersion = 1
              AND o.LabOrderStatus = @OrderStatusRecorded
              AND d.ResultStatus = @ResultStatusRecorded
            """;

        if (filter.Date1.HasValue && filter.Date2.HasValue)
            sql += " AND d.RecordedDate >= @Date1 AND d.RecordedDate < @Date2End";

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            sql += """
                 AND (
                     o.OrderNo LIKE @SearchTerm
                     OR o.PatientName LIKE @SearchTerm
                     OR o.PatientId LIKE @SearchTerm
                 )
                """;

        sql += " ORDER BY o.Urgency DESC, d.RecordedDate DESC";

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@OrderStatusRecorded", orderStatusRecorded, SqlDbType.Int);
        dp.AddParam("@ResultStatusRecorded", resultStatusRecorded, SqlDbType.Int);
        dp.AddParam("@FlagCritical", flagCritical, SqlDbType.Int);

        if (filter.Date1.HasValue && filter.Date2.HasValue)
        {
            dp.AddParam("@Date1", filter.Date1.Value.Date, SqlDbType.DateTime);
            dp.AddParam("@Date2End", filter.Date2.Value.Date.AddDays(1), SqlDbType.DateTime);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            dp.AddParam("@SearchTerm", $"%{filter.SearchTerm.Trim()}%", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabResultVerificationWorklistView>(sql, dp) ?? Enumerable.Empty<LabResultVerificationWorklistView>();
    }
}
