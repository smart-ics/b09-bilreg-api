using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using Bilreg.Application.PaymentContext.PasienBalanceFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;

public class LegacyOutstandingReceivableReader : IPasienBalanceLegacyReader
{
    private readonly DatabaseOptions _opt;

    public LegacyOutstandingReceivableReader(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<LegacyOutstandingReceivable> ListOutstanding(IPasienKey key, DateOnly businessDate)
    {
        const string sql = """
            SELECT
                aa.fs_kd_piutang,
                LEFT(aa.fs_keterangan, 10) AS RegId,
                aa.fn_nilai_jasa,
                aa.fn_nilai_obat,
                aa.fd_tgl_piutang
            FROM
                t_bp_piutang_hdr aa
            WHERE
                aa.fd_tgl_piutang <= @TglNow
                AND aa.fd_tgl_void = '3000-01-01'
                AND aa.fn_sisa > 0
                AND aa.fs_kd_mr = @PasienId
                AND aa.fs_kd_iii = 'JAMINAN000'
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TglNow", businessDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@PasienId", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Read<LegacyOutstandingReceivableRow>(sql, dp).ToList();

        return rows.Select(x => new LegacyOutstandingReceivable(
            key.PasienId,
            x.RegId,
            x.fn_nilai_jasa,
            x.fn_nilai_obat,
            ParsePiutangDate(x.fd_tgl_piutang),
            x.fs_kd_piutang));
    }

    private static DateTime ParsePiutangDate(string fdTglPiutang)
    {
        if (DateOnly.TryParseExact(fdTglPiutang, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.ToDateTime(TimeOnly.MinValue);

        return DateTime.ParseExact(fdTglPiutang, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private sealed record LegacyOutstandingReceivableRow(
        string fs_kd_piutang,
        string RegId,
        decimal fn_nilai_jasa,
        decimal fn_nilai_obat,
        string fd_tgl_piutang);
}
