using Dapper;
using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.StandardFeature;

public interface IKfaIngredientDal :
    IListData<KfaIngredientDto, IKfaKey>
{
}

public class KfaIngredientDal : IKfaIngredientDal
{
    private readonly DatabaseOptions _opt;

    public KfaIngredientDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<KfaIngredientDto> ListData(IKfaKey filter)
    {
        const string sql = """
            SELECT
                KfaId, KfaIngredientId, KfaIngredientName,
                Active, State, Strength
            FROM 
                FARPU_KfaIngredient
            WHERE 
                KfaId = @KfaId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@KfaId", filter.KfaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KfaIngredientDto>(sql, dp);
    }
}
