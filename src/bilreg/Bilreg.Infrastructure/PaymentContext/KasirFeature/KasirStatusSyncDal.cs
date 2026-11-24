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
        var dp = new DynamicParameters();
        dp.AddParam("@OrderRef", orderRef ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@KasirId", kasirId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TglTrs", tglTrs ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@JamTrs", jamTrs ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TrsKasirId", trsKasirId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TotalNilai", totalNilai, SqlDbType.Decimal);
        dp.AddParam("@RegId", regId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        if (!string.IsNullOrWhiteSpace(orderRef))
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
            conn.Execute(sqlDeposit, dp);
        }

        if (!string.IsNullOrWhiteSpace(regId))
        {
            const string sqlReg3 = """
                UPDATE ta_registrasi3
                SET fs_kd_trs_deposit_khusus = @TrsKasirId
                WHERE fs_kd_reg = @RegId AND fs_kd_bayar = 'BYKAS';
                """;
            conn.Execute(sqlReg3, dp);
        }
        else if (!string.IsNullOrWhiteSpace(orderRef))
        {
            const string sqlReg3Order = """
                UPDATE ta_registrasi3
                SET fs_kd_trs_deposit_khusus = @TrsKasirId
                WHERE fs_kd_reg = @OrderRef AND fs_kd_bayar = 'BYKAS';
                """;
            conn.Execute(sqlReg3Order, dp);
        }
    }

    public void SyncKeluar(string orderRef, string kasirId, string tglTrs, string jamTrs, string trsKasirId, decimal totalNilai, string regId = "")
    {
        var dp = new DynamicParameters();
        dp.AddParam("@OrderRef", orderRef ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@KasirId", kasirId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TglTrs", tglTrs ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@JamTrs", jamTrs ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TrsKasirId", trsKasirId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@RegId", regId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        if (!string.IsNullOrWhiteSpace(orderRef))
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
            conn.Execute(sqlDeposit, dp);
        }

        if (!string.IsNullOrWhiteSpace(regId))
        {
            const string sqlReg3 = """
                UPDATE ta_registrasi3
                SET fs_kd_trs_deposit_khusus = @TrsKasirId
                WHERE fs_kd_reg = @RegId AND fs_kd_bayar = 'BYKAS';
                """;
            conn.Execute(sqlReg3, dp);
        }
        else if (!string.IsNullOrWhiteSpace(orderRef))
        {
            const string sqlReg3Order = """
                UPDATE ta_registrasi3
                SET fs_kd_trs_deposit_khusus = @TrsKasirId
                WHERE fs_kd_reg = @OrderRef AND fs_kd_bayar = 'BYKAS';
                """;
            conn.Execute(sqlReg3Order, dp);
        }
    }

    public void Reversal(string orderRef, string trsKasirId, string regId = "")
    {
        var dp = new DynamicParameters();
        dp.AddParam("@OrderRef", orderRef ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@TrsKasirId", trsKasirId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@RegId", regId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        const string sqlDeposit = """
            UPDATE ta_trs_deposit
            SET fs_kd_petugas_kasir = '',
                fs_kd_trs_deposit_khusus = '',
                fn_total_bayar = 0
            WHERE (@OrderRef <> '' AND fs_kd_trs = @OrderRef) OR fs_kd_trs_deposit_khusus = @TrsKasirId;
            """;
        conn.Execute(sqlDeposit, dp);

        const string sqlReg3 = """
            UPDATE ta_registrasi3
            SET fs_kd_trs_deposit_khusus = ''
            WHERE fs_kd_trs_deposit_khusus = @TrsKasirId
               OR (fs_kd_bayar = 'BYKAS' AND (@RegId <> '' AND fs_kd_reg = @RegId));
            """;
        conn.Execute(sqlReg3, dp);
    }
}
