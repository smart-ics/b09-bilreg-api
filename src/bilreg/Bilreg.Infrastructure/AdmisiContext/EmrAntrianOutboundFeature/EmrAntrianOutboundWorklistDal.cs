using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.EmrAntrianOutboundFeature;

public class EmrAntrianOutboundWorklistDal : IEmrAntrianOutboundWorklistDal
{
    private readonly DatabaseOptions _opt;

    public EmrAntrianOutboundWorklistDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<EmrAntrianOutboundWorklistView> List(EmrAntrianOutboundWorklistFilter filter)
    {
        var sql = """
            SELECT
                aa.QueueId,
                aa.SourceId,
                aa.MessageType,
                aa.QueueStatus,
                aa.RetryCount,
                aa.LastError,
                aa.CrtDate,
                aa.ProcessedDate
            FROM BILRG_EmrAntrianOutboundQueue aa
            WHERE 1 = 1
            """;

        if (filter.QueueStatus.HasValue)
            sql += " AND aa.QueueStatus = @QueueStatus";

        if (filter.Date1.HasValue && filter.Date2.HasValue)
            sql += " AND aa.CrtDate >= @Date1 AND aa.CrtDate < @Date2End";

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            sql += " AND (aa.SourceId LIKE @SearchTerm OR aa.MessageType LIKE @SearchTerm)";

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
        return conn.Query<EmrAntrianOutboundWorklistView>(sql, dp).ToList();
    }
}
