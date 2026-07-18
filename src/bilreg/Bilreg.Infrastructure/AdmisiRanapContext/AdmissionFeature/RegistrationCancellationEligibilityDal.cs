using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;

public interface IRegistrationCancellationEligibilityDal
{
    bool HasBillingItems(string regId);
}

public sealed class RegistrationCancellationEligibilityDal : IRegistrationCancellationEligibilityDal
{
    internal const string HasBillingItemsSql = """
        SELECT
            CAST(CASE WHEN EXISTS (
                SELECT 1
                FROM ta_trs_billing aa
                WHERE aa.fs_kd_reg = @RegId
            ) THEN 1 ELSE 0 END AS bit)
        """;

    private readonly DatabaseOptions _opt;

    public RegistrationCancellationEligibilityDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public bool HasBillingItems(string regId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regId);

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<bool>(HasBillingItemsSql, dp);
    }
}
