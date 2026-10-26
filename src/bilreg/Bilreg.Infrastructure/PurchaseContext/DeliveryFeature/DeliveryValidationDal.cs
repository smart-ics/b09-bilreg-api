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

    public DeliveryDuplicateInfoDto? CheckDuplicateSuratJalan(string supplierId, string suratJalanNo, string? excludeDoId = null)
    {
        const string sql = """
            SELECT TOP 1
                aa.fs_kd_trs AS ExistingDoId,
                aa.fd_tgl_trs AS ExistingDate,
                ISNULL(aa.fs_kd_petugas, '') AS ExistingPetugasName
            FROM TB_TRS_DO aa
            WHERE aa.fs_kd_iii = @SupplierId
              AND aa.fs_kd_do_asli = @SuratJalanNo
              AND aa.fd_tgl_void = '3000-01-01'
              AND (@ExcludeDoId IS NULL OR @ExcludeDoId = '' OR aa.fs_kd_trs <> @ExcludeDoId)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@SupplierId", supplierId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@SuratJalanNo", suratJalanNo ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@ExcludeDoId", excludeDoId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<DeliveryDuplicateInfoDto>(sql, dp);
    }
}
