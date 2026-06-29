using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;

public interface IBilrgTataRekPasienBalanceDal :
    IInsert<BilrgTataRekPasienBalanceDto>,
    IGetData<BilrgTataRekPasienBalanceDto, IPasienKey>
{
    int UpdateConditional(BilrgTataRekPasienBalanceDto dto, int expectedVersion);
}

public class BilrgTataRekPasienBalanceDal : IBilrgTataRekPasienBalanceDal
{
    private readonly DatabaseOptions _opt;

    public BilrgTataRekPasienBalanceDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(BilrgTataRekPasienBalanceDto model)
    {
        const string sql = """
            INSERT INTO BILRG_TataRekPasienBalance (
                PasienId, Version,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @PasienId, @Version,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, BuildParams(model));
    }

    public int UpdateConditional(BilrgTataRekPasienBalanceDto dto, int expectedVersion)
    {
        const string sql = """
            UPDATE BILRG_TataRekPasienBalance
            SET UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                Version = Version + 1
            WHERE PasienId = @PasienId
              AND Version = @ExpectedVersion
            """;

        var dp = BuildParams(dto);
        dp.AddParam("@ExpectedVersion", expectedVersion, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Execute(sql, dp);
    }

    public BilrgTataRekPasienBalanceDto GetData(IPasienKey key)
    {
        const string sql = """
            SELECT
                PasienId, Version,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_TataRekPasienBalance
            WHERE PasienId = @PasienId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BilrgTataRekPasienBalanceDto>(sql, dp);
    }

    private static DynamicParameters BuildParams(BilrgTataRekPasienBalanceDto model)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", model.PasienId, SqlDbType.VarChar);
        dp.AddParam("@Version", model.Version, SqlDbType.Int);
        dp.AddParam("@CrtUser", model.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", model.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", model.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", model.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", model.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", model.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
