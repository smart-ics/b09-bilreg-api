using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.Shared.AuditLogFeature;

public interface IAuditLogDal : IInsert<AuditLogDto>
{
}

public class AuditLogDal : IAuditLogDal
{
    private readonly DatabaseOptions _opt;

    public AuditLogDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AuditLogDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AuditLog (
                AuditId, EventTime, UserId, ActionType, EntityName, EntityId,
                Reason, OriginalDataJson, CorrelationId, ClientIpAddress, UserAgent)
            VALUES (
                @AuditId, @EventTime, @UserId, @ActionType, @EntityName, @EntityId,
                @Reason, @OriginalDataJson, @CorrelationId, @ClientIpAddress, @UserAgent)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AuditId", dto.AuditId, SqlDbType.VarChar);
        dp.AddParam("@EventTime", dto.EventTime, SqlDbType.DateTime);
        dp.AddParam("@UserId", dto.UserId, SqlDbType.VarChar);
        dp.AddParam("@ActionType", dto.ActionType, SqlDbType.VarChar);
        dp.AddParam("@EntityName", dto.EntityName, SqlDbType.VarChar);
        dp.AddParam("@EntityId", dto.EntityId, SqlDbType.VarChar);
        dp.AddParam("@Reason", dto.Reason, SqlDbType.NVarChar);
        dp.AddParam("@OriginalDataJson", dto.OriginalDataJson, SqlDbType.NVarChar);
        dp.AddParam("@CorrelationId", dto.CorrelationId, SqlDbType.VarChar);
        dp.AddParam("@ClientIpAddress", dto.ClientIpAddress, SqlDbType.VarChar);
        dp.AddParam("@UserAgent", dto.UserAgent, SqlDbType.NVarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
