using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public interface IRoundingPolicyDal
{
    /// <summary>
    /// Membaca parameter pembulatan dari <c>tz_parameter_sistem</c> dengan fallback default
    /// <c>100</c> / <c>Ke Atas</c> bila parameter tidak ditemukan atau tidak valid (TD-07).
    /// </summary>
    RoundingPolicyDto GetPolicy();

    /// <summary>
    /// Menyimpan (upsert) parameter <c>FAR_DBUMUM_PEMBULATAN</c> dan
    /// <c>FAR_DBUMUM_PEMBULATAN_ATAS</c> pada <c>tz_parameter_sistem</c>.
    /// </summary>
    void SavePolicy(RoundingPolicyDto dto);
}

/// <summary>
/// Data access parameter global pembulatan kasir pada tabel legacy <c>tz_parameter_sistem</c>
/// (<c>FAR_DBUMUM_PEMBULATAN</c> dan <c>FAR_DBUMUM_PEMBULATAN_ATAS</c>).
/// </summary>
public class RoundingPolicyDal : IRoundingPolicyDal
{
    /// <summary>Key parameter kelipatan pembulatan (mis. <c>100</c>, <c>500</c>, <c>1000</c>).</summary>
    public const string PembulatanKey = "FAR_DBUMUM_PEMBULATAN";

    /// <summary>Key parameter arah pembulatan (<c>1</c> = Ke Atas, <c>0</c> = Normal).</summary>
    public const string PembulatanAtasKey = "FAR_DBUMUM_PEMBULATAN_ATAS";

    private const int DefaultIncrement = 100;

    private readonly DatabaseOptions _opt;

    public RoundingPolicyDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public RoundingPolicyDto GetPolicy()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        var increment = ParseIncrement(ReadValue(conn, PembulatanKey));
        var mode = ParseMode(ReadValue(conn, PembulatanAtasKey));

        return new RoundingPolicyDto(increment, mode);
    }

    /// <summary>
    /// Membaca satu nilai parameter; mengembalikan <c>null</c> bila parameter tidak ditemukan.
    /// </summary>
    private static string? ReadValue(SqlConnection conn, string key)
    {
        const string sql = """
            SELECT fs_value
            FROM tz_parameter_sistem
            WHERE fs_kd_parameter = @fs_kd_parameter
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", key, SqlDbType.VarChar);

        return conn.Read<string>(sql, dp)?.FirstOrDefault();
    }

    public void SavePolicy(RoundingPolicyDto dto)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        Upsert(conn, PembulatanKey, "Pembulatan Nilai Tagihan",
            dto.NilaiPembulatan.ToString(CultureInfo.InvariantCulture));
        Upsert(conn, PembulatanAtasKey, "Pembulatan Ke Atas",
            dto.JenisPembulatan == RoundingModeEnum.KeAtas ? "1" : "0");
    }

    private static void Upsert(SqlConnection conn, string key, string name, string value)
    {
        const string sql = """
            IF EXISTS (SELECT 1 FROM tz_parameter_sistem WHERE fs_kd_parameter = @fs_kd_parameter)
            BEGIN
                UPDATE tz_parameter_sistem
                SET fs_value = @fs_value
                WHERE fs_kd_parameter = @fs_kd_parameter
            END
            ELSE
            BEGIN
                INSERT INTO tz_parameter_sistem (fs_kd_parameter, fs_nm_parameter, fs_value)
                VALUES (@fs_kd_parameter, @fs_nm_parameter, @fs_value)
            END
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", key, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_parameter", name, SqlDbType.VarChar);
        dp.AddParam("@fs_value", value, SqlDbType.VarChar);

        conn.Execute(sql, dp);
    }

    private static int ParseIncrement(string? value) =>
        int.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : DefaultIncrement;

    private static RoundingModeEnum ParseMode(string? value) =>
        string.Equals(value?.Trim(), "0", StringComparison.Ordinal)
            ? RoundingModeEnum.Normal
            : RoundingModeEnum.KeAtas;
}
