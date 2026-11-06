using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

/// <summary>
/// Pemeriksaan transaksi hilir pada billing registrasi:
/// - alokasi pembayaran yang telah disetujui (alokasi ke penjamin / pihak ketiga),
/// - transaksi kasir yang telah diproses.
/// </summary>
public class DownstreamSettlementInspector : IDownstreamSettlementInspector
{
    private readonly DatabaseOptions _opt;

    public DownstreamSettlementInspector(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public bool HasApprovedAllocation(string regId)
    {
        const string sql = """
            SELECT
                COUNT(1)
            FROM
                ta_trs_billing2 aa
                INNER JOIN ta_trs_billing bill ON aa.fs_kd_trs = bill.fs_kd_trs
            WHERE
                bill.fs_kd_reg = @fs_kd_reg
                AND aa.fs_kd_trs_bayar <> aa.fs_kd_trs
                AND LEFT(aa.fs_kd_trs_bayar, 2) = 'RO'
                AND aa.fs_kd_jenis_bayar NOT IN ('KAS', 'HUT')
            """;

        return Exists(sql, regId);
    }

    public bool HasCashierSettlement(string regId)
    {
        const string sql = """
            SELECT
                COUNT(1)
            FROM
                ta_trs_billing2 aa
                INNER JOIN ta_trs_billing bill ON aa.fs_kd_trs = bill.fs_kd_trs
            WHERE
                bill.fs_kd_reg = @fs_kd_reg
                AND aa.fs_kd_trs_bayar <> aa.fs_kd_trs
                AND LEFT(aa.fs_kd_trs_bayar, 2) <> 'RO'
            """;

        return Exists(sql, regId);
    }

    private bool Exists(string sql, string regId)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ExecuteScalar<int>(sql, dp) > 0;
    }
}
