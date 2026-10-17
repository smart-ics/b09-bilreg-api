using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public class DeliveryValidationDal : IDeliveryValidationDal
{
    private readonly DatabaseOptions _opt;

    public DeliveryValidationDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public bool IsSuratJalanUnique(string supplierId, string suratJalanNo, string? excludeDoId = null)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM TB_TRS_DO
            WHERE fs_kd_iii = @SupplierId
              AND fs_kd_do_asli = @SuratJalanNo
              AND fd_tgl_void = '3000-01-01'
              AND (@ExcludeDoId IS NULL OR @ExcludeDoId = '' OR fs_kd_trs <> @ExcludeDoId)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@SupplierId", supplierId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@SuratJalanNo", suratJalanNo ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@ExcludeDoId", excludeDoId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var count = conn.ExecuteScalar<int>(sql, dp);
        return count == 0;
    }
}
