using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class KasirSupervisorAuthDal : IKasirSupervisorAuthDal
{
    private readonly DatabaseOptions _opt;

    public KasirSupervisorAuthDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public bool ValidateSupervisor(string userId, string password)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
            return false;

        const string sql = """
            SELECT TOP 1 1
            FROM TZ_USER
            WHERE fs_kd_user = @UserId
              AND fs_pwd = @Password
              AND (fb_sys_admin = 1 OR fb_verifikator = 1)
            """;

        var dp = new DynamicParameters();
        dp.Add("@UserId", userId.Trim(), DbType.String);
        dp.Add("@Password", password.Trim(), DbType.String);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ExecuteScalar<int?>(sql, dp);
        return result.HasValue && result.Value == 1;
    }
}
