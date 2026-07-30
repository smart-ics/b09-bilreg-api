using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public sealed class AdmissionConfigurationAuditReader : IAdmissionConfigurationAuditReader
{
    private readonly DatabaseOptions _opt;
    public AdmissionConfigurationAuditReader(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public IReadOnlyList<AdmissionConfigurationAuditItem> List(
        int page,
        int pageSize,
        out int totalCount)
    {
        const string where = """
            WHERE EntityName IN (
                'AdmissionWorkstationModel',
                'AdmissionQueueDisplayModel',
                'AdmissionQueueKioskModel')
            """;
        var offset = (page - 1) * pageSize;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        totalCount = conn.ExecuteScalar<int>($"SELECT COUNT(1) FROM BILRG_AuditLog {where}");
        const string select = """
            SELECT AuditId, EventTime, UserId, ActionType, EntityName, EntityId,
                   Reason, OriginalDataJson, CorrelationId
            FROM BILRG_AuditLog
            WHERE EntityName IN (
                'AdmissionWorkstationModel',
                'AdmissionQueueDisplayModel',
                'AdmissionQueueKioskModel')
            ORDER BY EventTime DESC, AuditId DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;
        return conn.Query<AdmissionConfigurationAuditItem>(
            select,
            new { Offset = offset, PageSize = pageSize }).ToList();
    }
}
