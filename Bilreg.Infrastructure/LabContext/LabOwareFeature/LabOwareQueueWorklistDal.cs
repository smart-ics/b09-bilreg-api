using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabOwareFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.LabContext.LabOwareFeature;

public class LabOwareQueueWorklistDal : ILabOwareQueueWorklistDal
{
    private readonly DatabaseOptions _opt;

    public LabOwareQueueWorklistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<LabOwareQueueWorklistView> List(LabOwareQueueWorklistFilter filter)
    {
        var sql = """
            SELECT
                aa.QueueId,
                bb.OrderNo,
                aa.QueueStatus,
                aa.RetryCount,
                aa.LastError,
                aa.CrtDate,
                aa.ProcessedDate
            FROM BILRG_LabOwareOutboundQueue aa
            INNER JOIN BILRG_LabOrder bb ON bb.OrderId = aa.OrderId
            WHERE 1 = 1
            """;

        if (filter.QueueStatus.HasValue)
            sql += " AND aa.QueueStatus = @QueueStatus";

        if (filter.Date1.HasValue && filter.Date2.HasValue)
            sql += " AND aa.CrtDate >= @Date1 AND aa.CrtDate < @Date2End";

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            sql += " AND bb.OrderNo LIKE @SearchTerm";

        sql += " ORDER BY aa.CrtDate DESC";

        var dp = new DynamicParameters();

        if (filter.QueueStatus.HasValue)
            dp.AddParam("@QueueStatus", filter.QueueStatus.Value, SqlDbType.Int);

        if (filter.Date1.HasValue && filter.Date2.HasValue)
        {
            dp.AddParam("@Date1", filter.Date1.Value, SqlDbType.DateTime);
            dp.AddParam("@Date2End", filter.Date2.Value.Date.AddDays(1), SqlDbType.DateTime);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            dp.AddParam("@SearchTerm", $"%{filter.SearchTerm.Trim()}%", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<LabOwareQueueWorklistView>(sql, dp).ToList();
    }
}
