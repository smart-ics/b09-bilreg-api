using Bilreg.Infrastructure.PaymentContext.DepositFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.DepositFeature;

public class DepositDalTest
{
    [Fact]
    public void GetSaldoSql_ContainsRequiredFiltersForPaidKasirDeposit()
    {
        // Act
        var sql = DepositDal.GetSaldoSql;

        // Assert - Filter kriteria kasir terbayar (fn_total_bayar > 0 dan ISNULL(fs_kd_petugas_kasir, '') <> '')
        sql.Should().Contain("fn_total_bayar > 0");
        sql.Should().Contain("ISNULL(fs_kd_petugas_kasir, '') <> ''");
        sql.Should().Contain("fd_tgl_void = '3000-01-01'");
    }

    [Fact]
    public void GetSaldoSql_ContainsCrossJoinWithRegistrasi3ForUsageAggregation()
    {
        // Act
        var sql = DepositDal.GetSaldoSql;

        // Assert
        sql.Should().Contain("ta_trs_deposit");
        sql.Should().Contain("ta_registrasi3");
        sql.Should().Contain("fs_kd_bayar IN ('BYDPU', 'BYDPK')");
        sql.Should().Contain("(setoran.TotalSetoran - terpakai.TotalTerpakai) AS SaldoAktif");
    }
}
