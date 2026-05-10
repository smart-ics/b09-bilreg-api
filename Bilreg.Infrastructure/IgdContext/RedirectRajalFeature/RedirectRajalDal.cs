using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.RedirectRajalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.IgdContext.RedirectRajalFeature;

public interface IRedirectRajalDal :
    IInsert<RedirectRajalDto>,
    IUpdate<RedirectRajalDto>,
    IDelete<IRedirectRajalKey>,
    IGetData<RedirectRajalDto, IRedirectRajalKey>,
    IListData<RedirectRajalDto, IIgdVisitKey>
{
}

public class RedirectRajalDal : IRedirectRajalDal
{
    private readonly DatabaseOptions _opt;

    public RedirectRajalDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(RedirectRajalDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_RedirectRajal (
                RedirectRajalId, IgdVisitId, VisitorName,
                RedirectDateTime, Reason, RedirectUserId)
            VALUES (
                @RedirectRajalId, @IgdVisitId, @VisitorName,
                @RedirectDateTime, @Reason, @RedirectUserId)
            """;
        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RedirectRajalDto dto)
    {
        const string sql = """
            UPDATE BILRG_RedirectRajal
            SET IgdVisitId = @IgdVisitId,
                VisitorName = @VisitorName,
                RedirectDateTime = @RedirectDateTime,
                Reason = @Reason,
                RedirectUserId = @RedirectUserId
            WHERE RedirectRajalId = @RedirectRajalId
            """;
        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRedirectRajalKey key)
    {
        const string sql = "DELETE BILRG_RedirectRajal WHERE RedirectRajalId = @RedirectRajalId";
        var dp = new DynamicParameters();
        dp.AddParam("@RedirectRajalId", key.RedirectRajalId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public RedirectRajalDto GetData(IRedirectRajalKey key)
    {
        const string sql = """
            SELECT RedirectRajalId, IgdVisitId, VisitorName,
                RedirectDateTime, Reason, RedirectUserId
            FROM BILRG_RedirectRajal
            WHERE RedirectRajalId = @RedirectRajalId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RedirectRajalId", key.RedirectRajalId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RedirectRajalDto>(sql, dp);
    }

    public IEnumerable<RedirectRajalDto> ListData(IIgdVisitKey filter)
    {
        const string sql = """
            SELECT RedirectRajalId, IgdVisitId, VisitorName,
                RedirectDateTime, Reason, RedirectUserId
            FROM BILRG_RedirectRajal
            WHERE IgdVisitId = @IgdVisitId
            ORDER BY RedirectDateTime
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", filter.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RedirectRajalDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(RedirectRajalDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@RedirectRajalId", dto.RedirectRajalId, SqlDbType.VarChar);
        dp.AddParam("@IgdVisitId", dto.IgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@VisitorName", dto.VisitorName, SqlDbType.VarChar);
        dp.AddParam("@RedirectDateTime", dto.RedirectDateTime, SqlDbType.DateTime);
        dp.AddParam("@Reason", dto.Reason, SqlDbType.VarChar);
        dp.AddParam("@RedirectUserId", dto.RedirectUserId, SqlDbType.VarChar);
        return dp;
    }
}
