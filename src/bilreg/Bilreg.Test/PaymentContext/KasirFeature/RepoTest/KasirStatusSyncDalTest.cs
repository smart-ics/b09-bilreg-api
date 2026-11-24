using System.Data.SqlClient;
using Bilreg.Infrastructure.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.RepoTest;

public class KasirStatusSyncDalTest
{
    private readonly KasirStatusSyncDal _sut;
    private readonly string _connString;

    public KasirStatusSyncDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _connString = ConnStringHelper.Get(opt.Value);
        _sut = new KasirStatusSyncDal(opt);
    }

    [Fact]
    public void SyncTerima_UpdatesDepositAndRegistrasi3_WithTrsKasirId()
    {
        var trsKasirId = "DX" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        var orderRef = "DP" + Guid.NewGuid().ToString("N")[..8].ToUpper();

        using (var conn = new SqlConnection(_connString))
        {
            conn.Execute("""
                INSERT INTO ta_trs_deposit (fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fs_kd_reg, fn_total_bayar, fs_kd_petugas_kasir, fs_kd_trs_deposit_khusus)
                VALUES (@OrderRef, '2026-11-11', '10:00:00', 'USR01', @RegId, 0, '', '');

                INSERT INTO ta_registrasi3 (fs_kd_reg, fs_kd_bayar, fs_kd_rek, fn_jasa, fn_obat, fs_kd_trs_deposit_khusus)
                VALUES (@RegId, 'BYKAS', '1110201', 50000, 25000, '');
                """, new { OrderRef = orderRef, RegId = regId });
        }

        try
        {
            // Act
            _sut.SyncTerima(
                orderRef: orderRef,
                kasirId: "KASIR01",
                tglTrs: "2026-11-11",
                jamTrs: "14:30:00",
                trsKasirId: trsKasirId,
                totalNilai: 75_000m,
                regId: regId);

            // Assert
            using (var conn = new SqlConnection(_connString))
            {
                var depKhusus = conn.ExecuteScalar<string>(
                    "SELECT fs_kd_trs_deposit_khusus FROM ta_trs_deposit WHERE fs_kd_trs = @OrderRef",
                    new { OrderRef = orderRef });
                depKhusus.Should().Be(trsKasirId);

                var reg3Khusus = conn.ExecuteScalar<string>(
                    "SELECT fs_kd_trs_deposit_khusus FROM ta_registrasi3 WHERE fs_kd_reg = @RegId AND fs_kd_bayar = 'BYKAS'",
                    new { RegId = regId });
                reg3Khusus.Should().Be(trsKasirId);
            }
        }
        finally
        {
            using var conn = new SqlConnection(_connString);
            conn.Execute("DELETE FROM ta_trs_deposit WHERE fs_kd_trs = @OrderRef", new { OrderRef = orderRef });
            conn.Execute("DELETE FROM ta_registrasi3 WHERE fs_kd_reg = @RegId", new { RegId = regId });
        }
    }

    [Fact]
    public void SyncKeluar_UpdatesDepositAndRegistrasi3_WithTrsKasirId()
    {
        var trsKasirId = "DX" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        var orderRef = "DP" + Guid.NewGuid().ToString("N")[..8].ToUpper();

        using (var conn = new SqlConnection(_connString))
        {
            conn.Execute("""
                INSERT INTO ta_trs_deposit (fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fs_kd_reg, fn_total_bayar, fs_kd_petugas_kasir, fs_kd_trs_deposit_khusus)
                VALUES (@OrderRef, '2026-11-11', '10:00:00', 'USR01', @RegId, 0, '', '');

                INSERT INTO ta_registrasi3 (fs_kd_reg, fs_kd_bayar, fs_kd_rek, fn_jasa, fn_obat, fs_kd_trs_deposit_khusus)
                VALUES (@RegId, 'BYKAS', '1110201', 30000, 0, '');
                """, new { OrderRef = orderRef, RegId = regId });
        }

        try
        {
            // Act
            _sut.SyncKeluar(
                orderRef: orderRef,
                kasirId: "KASIR01",
                tglTrs: "2026-11-11",
                jamTrs: "15:00:00",
                trsKasirId: trsKasirId,
                totalNilai: 30_000m,
                regId: regId);

            // Assert
            using (var conn = new SqlConnection(_connString))
            {
                var depKhusus = conn.ExecuteScalar<string>(
                    "SELECT fs_kd_trs_deposit_khusus FROM ta_trs_deposit WHERE fs_kd_trs = @OrderRef",
                    new { OrderRef = orderRef });
                depKhusus.Should().Be(trsKasirId);

                var reg3Khusus = conn.ExecuteScalar<string>(
                    "SELECT fs_kd_trs_deposit_khusus FROM ta_registrasi3 WHERE fs_kd_reg = @RegId AND fs_kd_bayar = 'BYKAS'",
                    new { RegId = regId });
                reg3Khusus.Should().Be(trsKasirId);
            }
        }
        finally
        {
            using var conn = new SqlConnection(_connString);
            conn.Execute("DELETE FROM ta_trs_deposit WHERE fs_kd_trs = @OrderRef", new { OrderRef = orderRef });
            conn.Execute("DELETE FROM ta_registrasi3 WHERE fs_kd_reg = @RegId", new { RegId = regId });
        }
    }

    [Fact]
    public void Reversal_ClearsTrsKasirDepositKhusus()
    {
        var trsKasirId = "DX" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        var regId = "RG" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        var orderRef = "DP" + Guid.NewGuid().ToString("N")[..8].ToUpper();

        using (var conn = new SqlConnection(_connString))
        {
            conn.Execute("""
                INSERT INTO ta_trs_deposit (fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fs_kd_reg, fn_total_bayar, fs_kd_petugas_kasir, fs_kd_trs_deposit_khusus)
                VALUES (@OrderRef, '2026-11-11', '10:00:00', 'USR01', @RegId, 50000, 'KASIR01', @TrsKasirId);

                INSERT INTO ta_registrasi3 (fs_kd_reg, fs_kd_bayar, fs_kd_rek, fn_jasa, fn_obat, fs_kd_trs_deposit_khusus)
                VALUES (@RegId, 'BYKAS', '1110201', 50000, 0, @TrsKasirId);
                """, new { OrderRef = orderRef, RegId = regId, TrsKasirId = trsKasirId });
        }

        try
        {
            // Act
            _sut.Reversal(orderRef, trsKasirId, regId);

            // Assert
            using (var conn = new SqlConnection(_connString))
            {
                var depKhusus = conn.ExecuteScalar<string>(
                    "SELECT fs_kd_trs_deposit_khusus FROM ta_trs_deposit WHERE fs_kd_trs = @OrderRef",
                    new { OrderRef = orderRef });
                depKhusus.Should().BeEmpty();

                var reg3Khusus = conn.ExecuteScalar<string>(
                    "SELECT fs_kd_trs_deposit_khusus FROM ta_registrasi3 WHERE fs_kd_reg = @RegId AND fs_kd_bayar = 'BYKAS'",
                    new { RegId = regId });
                reg3Khusus.Should().BeEmpty();
            }
        }
        finally
        {
            using var conn = new SqlConnection(_connString);
            conn.Execute("DELETE FROM ta_trs_deposit WHERE fs_kd_trs = @OrderRef", new { OrderRef = orderRef });
            conn.Execute("DELETE FROM ta_registrasi3 WHERE fs_kd_reg = @RegId", new { RegId = regId });
        }
    }
}
