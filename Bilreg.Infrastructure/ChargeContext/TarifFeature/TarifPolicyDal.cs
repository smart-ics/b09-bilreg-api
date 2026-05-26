using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface ITarifPolicyDal :
    IInsert<TarifPolicyDto>,
    IUpdate<TarifPolicyDto>,
    IDelete<ITarifPolicyKey>,
    IGetData<TarifPolicyDto, ITarifPolicyKey>,
    IListData<TarifPolicyDto, TarifPolicyListFilter>
{
}

public class TarifPolicyDal : ITarifPolicyDal
{
    private readonly DatabaseOptions _opt;

    public TarifPolicyDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(TarifPolicyDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_TarifPolicy (
                TarifPolicyId, PolicyNo, PolicyName, EffectiveDateInfo, Description, PolicyStatus,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @TarifPolicyId, @PolicyNo, @PolicyName, @EffectiveDateInfo, @Description, @PolicyStatus,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public void Update(TarifPolicyDto dto)
    {
        const string sql = """
            UPDATE BILRG_TarifPolicy
            SET
                PolicyNo = @PolicyNo,
                PolicyName = @PolicyName,
                EffectiveDateInfo = @EffectiveDateInfo,
                Description = @Description,
                PolicyStatus = @PolicyStatus,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                TarifPolicyId = @TarifPolicyId
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));
    }

    public void Delete(ITarifPolicyKey key)
    {
        const string sql = """
            DELETE FROM BILRG_TarifPolicy
            WHERE TarifPolicyId = @TarifPolicyId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TarifPolicyId", key.TarifPolicyId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TarifPolicyDto GetData(ITarifPolicyKey key)
    {
        const string sql = """
            SELECT
                TarifPolicyId, PolicyNo, PolicyName, EffectiveDateInfo, Description, PolicyStatus,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_TarifPolicy
            WHERE TarifPolicyId = @TarifPolicyId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TarifPolicyId", key.TarifPolicyId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TarifPolicyDto>(sql, dp);
    }

    public IEnumerable<TarifPolicyDto> ListData(TarifPolicyListFilter filter)
    {
        const string sql = """
            SELECT
                aa.TarifPolicyId, aa.PolicyNo, aa.PolicyName, aa.EffectiveDateInfo,
                aa.Description, aa.PolicyStatus,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM BILRG_TarifPolicy aa
            WHERE
                (@PolicyStatus IS NULL OR aa.PolicyStatus = @PolicyStatus)
                AND (
                    @Keyword = ''
                    OR aa.PolicyNo LIKE @KeywordPattern
                    OR aa.PolicyName LIKE @KeywordPattern
                )
            ORDER BY aa.CrtDate DESC, aa.TarifPolicyId DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PolicyStatus",
            filter.PolicyStatus.HasValue ? (int)filter.PolicyStatus.Value : null,
            SqlDbType.Int);
        dp.AddParam("@Keyword", filter.Keyword ?? "", SqlDbType.VarChar);
        dp.AddParam("@KeywordPattern", $"%{filter.Keyword}%", SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TarifPolicyDto>(sql, dp);
    }

    private static DynamicParameters MapParams(TarifPolicyDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@TarifPolicyId", dto.TarifPolicyId, SqlDbType.VarChar);
        dp.AddParam("@PolicyNo", dto.PolicyNo, SqlDbType.VarChar);
        dp.AddParam("@PolicyName", dto.PolicyName, SqlDbType.VarChar);
        dp.AddParam("@EffectiveDateInfo", dto.EffectiveDateInfo, SqlDbType.DateTime);
        dp.AddParam("@Description", dto.Description, SqlDbType.VarChar);
        dp.AddParam("@PolicyStatus", dto.PolicyStatus, SqlDbType.Int);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
