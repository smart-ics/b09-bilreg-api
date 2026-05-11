using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitFeature;

public interface IIgdVisitEventDal :
    IInsert<IgdVisitEventDto>,
    IDelete<IIgdVisitKey>,
    IListData<IgdVisitEventDto, IIgdVisitKey>
{
}

public class IgdVisitEventDal : IIgdVisitEventDal
{
    private readonly DatabaseOptions _opt;

    public IgdVisitEventDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IgdVisitEventDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_IgdVisitEvent (
                IgdVisitId, NoEvent, EventKind, EventDateTime, UserId, Notes)
            VALUES (
                @IgdVisitId, @NoEvent, @EventKind, @EventDateTime, @UserId, @Notes)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", dto.IgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@NoEvent", dto.NoEvent, SqlDbType.Int);
        dp.AddParam("@EventKind", dto.EventKind, SqlDbType.VarChar);
        dp.AddParam("@EventDateTime", dto.EventDateTime, SqlDbType.DateTime);
        dp.AddParam("@UserId", dto.UserId, SqlDbType.VarChar);
        dp.AddParam("@Notes", dto.Notes, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IIgdVisitKey key)
    {
        const string sql = """
            DELETE BILRG_IgdVisitEvent WHERE IgdVisitId = @IgdVisitId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", key.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<IgdVisitEventDto> ListData(IIgdVisitKey key)
    {
        const string sql = """
            SELECT IgdVisitId, NoEvent, EventKind, EventDateTime, UserId, Notes
            FROM BILRG_IgdVisitEvent
            WHERE IgdVisitId = @IgdVisitId
            ORDER BY NoEvent
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", key.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<IgdVisitEventDto>(sql, dp);
    }
}
