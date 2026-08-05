using Dapper;
using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public interface IKfaReplacementDal :
    IGetData<KfaReplacementDto, IKfaKey>,
    IListData<KfaReplacementDto>
{
}

public class KfaReplacementDal : IKfaReplacementDal
{
    private readonly DatabaseOptions _opt;

    public KfaReplacementDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public KfaReplacementDto GetData(IKfaKey key)
    {
        const string sql = """
            SELECT
                KfaId, KfaNewId, KfaNewName, Reason
            FROM 
                FARPU_KfaReplacement
            WHERE 
                KfaId = @KfaId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@KfaId", key.KfaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<KfaReplacementDto>(sql, dp);
    }

    public IEnumerable<KfaReplacementDto> ListData()
    {
        const string sql = """
            SELECT
                KfaId, KfaNewId, KfaNewName, Reason
            FROM 
                FARPU_KfaReplacement
            WHERE 
                KfaId = @KfaId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KfaReplacementDto>(sql);
    }
}
