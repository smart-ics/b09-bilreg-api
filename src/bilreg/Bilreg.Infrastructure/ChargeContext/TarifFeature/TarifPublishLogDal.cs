using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface ITarifPublishLogDal :
    IInsert<TarifPublishLogDto>,
    IGetData<TarifPublishLogDto, ITarifPublishLogKey>,
    IListData<TarifPublishLogDto, ITarifPolicyKey>
{
    TarifLastPublishRow? GetLastPublish();
}

public class TarifPublishLogDal : ITarifPublishLogDal
{
    private readonly DatabaseOptions _opt;

    public TarifPublishLogDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(TarifPublishLogDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_TarifPublishLog (
                PublishLogId, TarifPolicyId, PublishedBy, PublishedDate, VariantCount, Note)
            VALUES (
                @PublishLogId, @TarifPolicyId, @PublishedBy, @PublishedDate, @VariantCount, @Note)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public TarifPublishLogDto GetData(ITarifPublishLogKey key)
    {
        const string sql = """
            SELECT
                PublishLogId, TarifPolicyId, PublishedBy, PublishedDate, VariantCount, Note
            FROM BILRG_TarifPublishLog
            WHERE PublishLogId = @PublishLogId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PublishLogId", key.PublishLogId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TarifPublishLogDto>(sql, dp);
    }

    public IEnumerable<TarifPublishLogDto> ListData(ITarifPolicyKey filter)
    {
        const string sql = """
            SELECT
                PublishLogId, TarifPolicyId, PublishedBy, PublishedDate, VariantCount, Note
            FROM BILRG_TarifPublishLog
            WHERE TarifPolicyId = @TarifPolicyId
            ORDER BY PublishedDate DESC, PublishLogId DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TarifPolicyId", filter.TarifPolicyId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TarifPublishLogDto>(sql, dp);
    }

    public TarifLastPublishRow? GetLastPublish()
    {
        const string sql = """
            SELECT TOP 1
                PublishedDate, TarifPolicyId, PublishLogId
            FROM BILRG_TarifPublishLog
            ORDER BY PublishedDate DESC, PublishLogId DESC
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TarifLastPublishRow>(sql)?.FirstOrDefault();
    }

    private static DynamicParameters MapParams(TarifPublishLogDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@PublishLogId", dto.PublishLogId, SqlDbType.VarChar);
        dp.AddParam("@TarifPolicyId", dto.TarifPolicyId, SqlDbType.VarChar);
        dp.AddParam("@PublishedBy", dto.PublishedBy, SqlDbType.VarChar);
        dp.AddParam("@PublishedDate", dto.PublishedDate, SqlDbType.DateTime);
        dp.AddParam("@VariantCount", dto.VariantCount, SqlDbType.Int);
        dp.AddParam("@Note", dto.Note, SqlDbType.VarChar);
        return dp;
    }
}
