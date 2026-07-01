using Dapper;
using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public interface IKfaPackagingDal :
    IListData<KfaPackagingDto, IKfaKey>
{
}

public class KfaPackagingDal : IKfaPackagingDal
{
    private readonly DatabaseOptions _opt;

    public KfaPackagingDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<KfaPackagingDto> ListData(IKfaKey filter)
    {
        const string sql = """
            SELECT
                KfaId, KfaPackagingId, KfaPackagingName,
                PackPrice, UomId, Qty, Generate
            FROM 
                FARPU_KfaPackaging
            WHERE 
                KfaId = @KfaId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@KfaId", filter.KfaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KfaPackagingDto>(sql, dp);
    }
}
