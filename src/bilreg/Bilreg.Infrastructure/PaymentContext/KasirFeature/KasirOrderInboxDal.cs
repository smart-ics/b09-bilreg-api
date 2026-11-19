using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class KasirOrderInboxDal : IKasirOrderInboxDal
{
    private readonly DatabaseOptions _opt;

    public KasirOrderInboxDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<OrderKasirItemDto> ListOrderInbox(string? targetDate = null)
    {
        var tgl = string.IsNullOrWhiteSpace(targetDate)
            ? DateTime.Today.ToString("yyyy-MM-dd")
            : targetDate.Trim();

        const string sql = """
            WITH PendingBills AS (
                SELECT fs_kd_trs, fs_kd_reg, fn_total, fd_tgl_trs
                FROM ta_trs_billing
                WHERE fd_tgl_trs = @TargetDate AND fn_total > 0
                UNION
                SELECT b.fs_kd_trs, b.fs_kd_reg, b.fn_total, b.fd_tgl_trs
                FROM ta_trs_billing b
                INNER JOIN BILRG_RegAktif ra ON b.fs_kd_reg = ra.RegId
                WHERE b.fn_total > 0
            )
            -- 1. Order Tagihan Pasien Selesai Layanan (ta_registrasi3 alokasi BYKAS)
            SELECT
                reg3.fs_kd_reg AS OrderId,
                'PENERIMAAN' AS [Type],
                'Order Pembayaran Tagihan' AS Subtype,
                reg3.fs_kd_reg AS RegId,
                ISNULL(reg.fs_mr, '') AS NoRm,
                ISNULL(mr.fs_nm_pasien, '') AS PasienName,
                (reg3.fn_jasa + reg3.fn_obat) AS TotalNilai,
                'MENUNGGU_BAYAR' AS [Status],
                reg.fd_tgl_keluar AS TglOrder,
                ISNULL(lay.fs_nm_layanan, '-') AS UnitName,
                ISNULL(tj.fs_nm_tipe_jaminan, 'UMUM/MANDIRI') AS PenjaminName
            FROM ta_registrasi3 reg3
            INNER JOIN ta_registrasi reg ON reg3.fs_kd_reg = reg.fs_kd_reg
            LEFT JOIN tc_mr mr ON reg.fs_mr = mr.fs_mr
            LEFT JOIN ta_layanan lay ON reg.fs_kd_layanan = lay.fs_kd_layanan
            LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
            WHERE reg3.fs_kd_bayar = 'BYKAS'
              AND reg3.fs_kd_rek = (SELECT TOP 1 fs_value FROM tz_parameter_sistem WHERE fs_kd_parameter = 'BIL_REGOUT_REK_PIUT_KSR')
              AND (reg3.fs_kd_trs_deposit_khusus IS NULL OR reg3.fs_kd_trs_deposit_khusus = '')
              AND reg.fd_tgl_keluar IS NOT NULL
              AND reg.fd_tgl_keluar <> ''
              AND reg.fd_tgl_keluar <> '3000-01-01'
              AND reg.fd_tgl_keluar = @TargetDate
              AND (reg3.fn_jasa + reg3.fn_obat) > 0
              AND NOT EXISTS (
                  SELECT 1 FROM BILRG_TrsKasir k
                  WHERE (k.OrderRef = reg3.fs_kd_reg OR k.RegId = reg3.fs_kd_reg)
                    AND k.JenisTrs = 'TERIMA'
                    AND k.StatusTrs = 'BERHASIL'
              )

            UNION ALL

            -- 2. Order Setoran Deposit (Pasien Aktif di BILRG_RegAktif)
            SELECT
                dep.fs_kd_trs AS OrderId,
                'PENERIMAAN' AS [Type],
                'Order Pembayaran Deposit' AS Subtype,
                dep.fs_kd_reg AS RegId,
                ISNULL(reg.fs_mr, '') AS NoRm,
                ISNULL(mr.fs_nm_pasien, '') AS PasienName,
                dep.fn_nilai_deposit AS TotalNilai,
                'MENUNGGU_BAYAR' AS [Status],
                dep.fd_tgl_trs AS TglOrder,
                ISNULL(lay.fs_nm_layanan, '-') AS UnitName,
                ISNULL(tj.fs_nm_tipe_jaminan, 'UMUM/MANDIRI') AS PenjaminName
            FROM ta_trs_deposit dep
            INNER JOIN BILRG_RegAktif ra ON dep.fs_kd_reg = ra.RegId
            LEFT JOIN ta_registrasi reg ON dep.fs_kd_reg = reg.fs_kd_reg
            LEFT JOIN tc_mr mr ON reg.fs_mr = mr.fs_mr
            LEFT JOIN ta_layanan lay ON reg.fs_kd_layanan = lay.fs_kd_layanan
            LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
            WHERE dep.fd_tgl_void = '3000-01-01'
              AND (dep.fs_kd_trs_deposit_khusus IS NULL OR dep.fs_kd_trs_deposit_khusus = '')
              AND (dep.fs_kd_petugas_kasir = '' OR dep.fs_kd_petugas_kasir IS NULL)
              AND dep.fn_total_bayar = 0
              AND dep.fn_nilai_deposit > 0
              AND dep.fd_tgl_trs = @TargetDate
              AND NOT EXISTS (
                  SELECT 1 FROM BILRG_TrsKasir k
                  WHERE k.OrderRef = dep.fs_kd_trs
                    AND k.JenisTrs = 'TERIMA'
                    AND k.StatusTrs = 'BERHASIL'
              )

            UNION ALL

            -- 3. Pengeluaran Kas / Pengembalian Deposit (Refund)
            SELECT
                dep.fs_kd_trs AS OrderId,
                'PENGELUARAN' AS [Type],
                'Pengembalian Deposit / Refund' AS Subtype,
                dep.fs_kd_reg AS RegId,
                ISNULL(reg.fs_mr, '') AS NoRm,
                ISNULL(mr.fs_nm_pasien, '') AS PasienName,
                ABS(dep.fn_nilai_deposit) AS TotalNilai,
                'MENUNGGU_PENGELUARAN' AS [Status],
                dep.fd_tgl_trs AS TglOrder,
                ISNULL(lay.fs_nm_layanan, '-') AS UnitName,
                ISNULL(tj.fs_nm_tipe_jaminan, 'UMUM/MANDIRI') AS PenjaminName
            FROM ta_trs_deposit dep
            LEFT JOIN ta_registrasi reg ON dep.fs_kd_reg = reg.fs_kd_reg
            LEFT JOIN tc_mr mr ON reg.fs_mr = mr.fs_mr
            LEFT JOIN ta_layanan lay ON reg.fs_kd_layanan = lay.fs_kd_layanan
            LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
            WHERE dep.fd_tgl_void = '3000-01-01'
              AND (dep.fs_kd_trs_deposit_khusus IS NULL OR dep.fs_kd_trs_deposit_khusus = '')
              AND (dep.fs_kd_petugas_kasir = '' OR dep.fs_kd_petugas_kasir IS NULL)
              AND dep.fn_nilai_deposit < 0
              AND dep.fd_tgl_trs = @TargetDate
              AND NOT EXISTS (
                  SELECT 1 FROM BILRG_TrsKasir k
                  WHERE k.OrderRef = dep.fs_kd_trs
                    AND k.JenisTrs = 'KELUAR'
                    AND k.StatusTrs = 'BERHASIL'
              )

            UNION ALL

            -- 4. Tagihan Belum Lunas Rawat Jalan & IGD (ta_trs_billing)
            SELECT
                b.fs_kd_reg AS OrderId,
                'PENERIMAAN' AS [Type],
                'Tagihan Layanan RJ/IGD' AS Subtype,
                b.fs_kd_reg AS RegId,
                ISNULL(reg.fs_mr, '') AS NoRm,
                ISNULL(mr.fs_nm_pasien, '') AS PasienName,
                SUM(b.fn_total) AS TotalNilai,
                'MENUNGGU_BAYAR' AS [Status],
                MAX(b.fd_tgl_trs) AS TglOrder,
                ISNULL(lay.fs_nm_layanan, '-') AS UnitName,
                ISNULL(tj.fs_nm_tipe_jaminan, 'UMUM/MANDIRI') AS PenjaminName
            FROM PendingBills b
            LEFT JOIN ta_registrasi reg ON b.fs_kd_reg = reg.fs_kd_reg
            LEFT JOIN tc_mr mr ON reg.fs_mr = mr.fs_mr
            LEFT JOIN ta_layanan lay ON reg.fs_kd_layanan = lay.fs_kd_layanan
            LEFT JOIN ta_tipe_jaminan tj ON reg.fs_kd_tipe_jaminan = tj.fs_kd_tipe_jaminan
            WHERE (reg.fd_tgl_void = '3000-01-01' OR reg.fd_tgl_void IS NULL OR reg.fd_tgl_void = '')
              AND NOT EXISTS (
                  SELECT 1 FROM ta_registrasi3 reg3
                  WHERE reg3.fs_kd_reg = b.fs_kd_reg
                    AND reg3.fs_kd_bayar = 'BYKAS'
              )
              AND NOT EXISTS (
                  SELECT 1 FROM BILRG_TrsKasirItem ki
                  INNER JOIN BILRG_TrsKasir k ON ki.TrsKasirId = k.TrsKasirId
                  WHERE ki.BillId = b.fs_kd_trs
                    AND k.StatusTrs = 'BERHASIL'
              )
            GROUP BY
                b.fs_kd_reg,
                reg.fs_mr,
                mr.fs_nm_pasien,
                lay.fs_nm_layanan,
                tj.fs_nm_tipe_jaminan
            HAVING SUM(b.fn_total) > 0
            ORDER BY TglOrder DESC, OrderId DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TargetDate", tgl, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderKasirItemDto>(sql, dp);
    }
}
