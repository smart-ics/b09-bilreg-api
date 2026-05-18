using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public class LabOrderReleaseWorklistDal : ILabOrderReleaseWorklistDal
{
    private static readonly DateTime VoidSentinel = new(3000, 1, 1);

    private readonly DatabaseOptions _opt;

    public LabOrderReleaseWorklistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<LabReleaseView> List(LabOrderReleaseWorklistFilter filter)
    {
        const int orderStatusVerified = (int)LabOrderStatusEnum.Verified;
        const int clearanceApproved = (int)FinancialClearanceEnum.Approved;

        var sql = """
            SELECT
                o.OrderId,
                o.OrderNo,
                o.PatientName,
                d.VerifiedDate,
                o.FinancialClearance,
                o.ReleasedDate,
                o.LabOrderStatus
            FROM BILRG_LabOrder o
            INNER JOIN BILRG_LabResultDocument d ON d.OrderId = o.OrderId
            WHERE o.VodDate = @VodDate
              AND d.VodDate = @VodDate
              AND d.IsCurrentVersion = 1
              AND o.LabOrderStatus = @OrderStatusVerified
              AND o.FinancialClearance = @ClearanceApproved
            """;

        if (filter.Date1.HasValue && filter.Date2.HasValue)
            sql += " AND d.VerifiedDate >= @Date1 AND d.VerifiedDate < @Date2End";

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            sql += """
                 AND (
                     o.OrderNo LIKE @SearchTerm
                     OR o.PatientName LIKE @SearchTerm
                     OR o.PatientId LIKE @SearchTerm
                 )
                """;

        sql += " ORDER BY d.VerifiedDate DESC";

        var dp = new DynamicParameters();
        dp.AddParam("@VodDate", VoidSentinel, SqlDbType.DateTime);
        dp.AddParam("@OrderStatusVerified", orderStatusVerified, SqlDbType.Int);
        dp.AddParam("@ClearanceApproved", clearanceApproved, SqlDbType.Int);

        if (filter.Date1.HasValue && filter.Date2.HasValue)
        {
            dp.AddParam("@Date1", filter.Date1.Value.Date, SqlDbType.DateTime);
            dp.AddParam("@Date2End", filter.Date2.Value.Date.AddDays(1), SqlDbType.DateTime);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            dp.AddParam("@SearchTerm", $"%{filter.SearchTerm.Trim()}%", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<LabReleaseView>(sql, dp);
    }
}
