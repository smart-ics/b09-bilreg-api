using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class KasirStatusSyncDal : IKasirStatusSyncDal
{
    private readonly DatabaseOptions _opt;

    public KasirStatusSyncDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void SyncTerima(string orderRef, string kasirId, string tglTrs, string jamTrs, string trsKasirId, decimal totalNilai, string regId = "")
    {
        const string sqlDeposit = """
            UPDATE ta_trs_deposit
            SET fs_kd_petugas_kasir = @KasirId,
                fn_total_bayar = @TotalNilai,
                fs_kd_trs_deposit_khusus = @TrsKasirId,
                updtgl = @TglTrs,
                updjam = @JamTrs,
                updusr = @KasirId
            WHERE fs_kd_trs = @OrderRef;
            """;

        const string sqlBilling2 = """
            UPDATE ta_trs_billing2
            SET fs_kd_petugas_kasir = @KasirId,
                fd_tgl_bayar = @TglTrs,
                fs_jam_bayar = @JamTrs,
                fs_kd_trs_bayar = @TrsKasirId
            WHERE fs_kd_jenis_bayar = 'BYKAS'
              AND (fs_kd_trs = @OrderRef
                   OR (@RegId <> '' AND fs_kd_trs IN (SELECT fs_kd_trs FROM ta_trs_billing WHERE fs_kd_reg = @RegId)));
            """;

        const string sqlReg3 = """
            UPDATE ta_registrasi3
            SET fs_kd_trs_deposit_khusus = @TrsKasirId
            WHERE fs_kd_bayar = 'BYKAS'
              AND (fs_kd_reg = @RegId OR (@OrderRef <> '' AND fs_kd_reg = @OrderRef));
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderRef", orderRef ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@KasirId", kasirId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TglTrs", tglTrs ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@JamTrs", jamTrs ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TrsKasirId", trsKasirId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TotalNilai", totalNilai, SqlDbType.Decimal);
        dp.AddParam("@RegId", regId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sqlDeposit, dp);
        conn.Execute(sqlBilling2, dp);
        conn.Execute(sqlReg3, dp);
    }

    public void SyncKeluar(string orderRef, string kasirId, string tglTrs, string jamTrs, string trsKasirId, decimal totalNilai, string regId = "")
    {
        const string sqlDeposit = """
            UPDATE ta_trs_deposit
            SET fs_kd_petugas_kasir = @KasirId,
                fs_kd_trs_deposit_khusus = @TrsKasirId,
                updtgl = @TglTrs,
                updjam = @JamTrs,
                updusr = @KasirId
            WHERE fs_kd_trs = @OrderRef;
            """;

        const string sqlBilling2 = """
            UPDATE ta_trs_billing2
            SET fs_kd_petugas_kasir = @KasirId,
                fd_tgl_bayar = @TglTrs,
                fs_jam_bayar = @JamTrs,
                fs_kd_trs_bayar = @TrsKasirId
            WHERE fs_kd_jenis_bayar = 'BYKAS'
              AND (fs_kd_trs = @OrderRef
                   OR (@RegId <> '' AND fs_kd_trs IN (SELECT fs_kd_trs FROM ta_trs_billing WHERE fs_kd_reg = @RegId)));
            """;

        const string sqlReg3 = """
            UPDATE ta_registrasi3
            SET fs_kd_trs_deposit_khusus = @TrsKasirId
            WHERE fs_kd_bayar = 'BYKAS'
              AND (fs_kd_reg = @RegId OR (@OrderRef <> '' AND fs_kd_reg = @OrderRef));
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderRef", orderRef ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@KasirId", kasirId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TglTrs", tglTrs ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@JamTrs", jamTrs ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TrsKasirId", trsKasirId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@RegId", regId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sqlDeposit, dp);
        conn.Execute(sqlBilling2, dp);
        conn.Execute(sqlReg3, dp);
    }

    public void Reversal(string orderRef, string trsKasirId, string regId = "")
    {
        const string sqlDeposit = """
            UPDATE ta_trs_deposit
            SET fs_kd_petugas_kasir = '',
                fs_kd_trs_deposit_khusus = '',
                fn_total_bayar = 0
            WHERE fs_kd_trs = @OrderRef OR fs_kd_trs_deposit_khusus = @TrsKasirId;
            """;

        const string sqlBilling2 = """
            UPDATE ta_trs_billing2
            SET fs_kd_petugas_kasir = '',
                fd_tgl_bayar = '3000-01-01',
                fs_jam_bayar = '00:00:00',
                fs_kd_trs_bayar = ''
            WHERE fs_kd_trs_bayar = @TrsKasirId
               OR (fs_kd_jenis_bayar = 'BYKAS' AND (fs_kd_trs = @OrderRef OR (@RegId <> '' AND fs_kd_trs IN (SELECT fs_kd_trs FROM ta_trs_billing WHERE fs_kd_reg = @RegId))));
            """;

        const string sqlReg3 = """
            UPDATE ta_registrasi3
            SET fs_kd_trs_deposit_khusus = ''
            WHERE fs_kd_trs_deposit_khusus = @TrsKasirId
               OR (fs_kd_bayar = 'BYKAS' AND (fs_kd_reg = @RegId OR (@OrderRef <> '' AND fs_kd_reg = @OrderRef)));
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderRef", orderRef ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TrsKasirId", trsKasirId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@RegId", regId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sqlDeposit, dp);
        conn.Execute(sqlBilling2, dp);
        conn.Execute(sqlReg3, dp);
    }
}
