using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature;

public class KasirShiftModelTest
{
    private static readonly DateTime OpenTime = new(2026, 11, 7, 7, 0, 0);

    [Fact]
    public void OpenShift_WithValidParameters_ShouldInitializeOpenShiftCorrectly()
    {
        // Act
        var sut = KasirShiftModel.OpenShift(
            kodeShift: "1",
            kasirId: "KASIR-01",
            loket: "Loket 01",
            saldoAwal: 500_000m,
            openTime: OpenTime);

        // Assert
        sut.ShiftId.Should().StartWith("SH");
        sut.ShiftId.Length.Should().Be(10);
        sut.KodeShift.Should().Be("1");
        sut.KasirId.Should().Be("KASIR-01");
        sut.Loket.Should().Be("Loket 01");
        sut.SaldoAwal.Should().Be(500_000m);
        sut.TglShift.Should().Be("2026-11-07");
        sut.TglJamBuka.Should().Be("2026-11-07 07:00:00");
        sut.TglJamTutup.Should().Be("3000-01-01 00:00:00");
        sut.StatusShift.Should().Be(KasirConst.StatusShiftBuka);
        sut.IsOpen.Should().BeTrue();
        sut.IsClosed.Should().BeFalse();
        sut.SaldoAkhirFisik.Should().Be(0m);
        sut.TotalTerimaSistem.Should().Be(0m);
        sut.TotalKeluarSistem.Should().Be(0m);
        sut.SaldoAkhirSistem.Should().Be(500_000m);
        sut.Audit.Created.UserId.Should().Be("KASIR-01");
    }

    [Fact]
    public void OpenShift_WithInvalidInputs_ShouldThrowBusinessValidationException()
    {
        var actNoKode = () => KasirShiftModel.OpenShift("", "KASIR-01", "Loket 01", 100_000m, OpenTime);
        actNoKode.Should().Throw<BusinessValidationException>().WithMessage("*Kode shift wajib diisi*");

        var actNoKasir = () => KasirShiftModel.OpenShift("1", "", "Loket 01", 100_000m, OpenTime);
        actNoKasir.Should().Throw<BusinessValidationException>().WithMessage("*KasirId wajib diisi*");

        var actNegSaldo = () => KasirShiftModel.OpenShift("1", "KASIR-01", "Loket 01", -50_000m, OpenTime);
        actNegSaldo.Should().Throw<BusinessValidationException>().WithMessage("*Saldo awal kasir tidak boleh negatif*");
    }

    [Fact]
    public void CloseShift_WhenShiftIsOpen_ShouldCalculateBalancesAndCloseSuccessfully()
    {
        // Arrange
        var sut = KasirShiftModel.OpenShift(
            kodeShift: "1",
            kasirId: "KASIR-01",
            loket: "Loket 01",
            saldoAwal: 500_000m,
            openTime: OpenTime);

        var closeTime = new DateTime(2026, 11, 7, 14, 0, 0);

        // Act: Saldo awal 500k, Terima sistem 2.500k, Keluar sistem 200k => Saldo sistem = 2.800k. Fisik dihitung 2.800k (selisih 0)
        sut.CloseShift(
            saldoAkhirFisik: 2_800_000m,
            totalTerima: 2_500_000m,
            totalKeluar: 200_000m,
            closeTime: closeTime);

        // Assert
        sut.StatusShift.Should().Be(KasirConst.StatusShiftTutup);
        sut.IsOpen.Should().BeFalse();
        sut.IsClosed.Should().BeTrue();
        sut.TglJamTutup.Should().Be("2026-11-07 14:00:00");
        sut.SaldoAkhirFisik.Should().Be(2_800_000m);
        sut.TotalTerimaSistem.Should().Be(2_500_000m);
        sut.TotalKeluarSistem.Should().Be(200_000m);
        sut.SaldoAkhirSistem.Should().Be(2_800_000m);
        sut.SelisihKas.Should().Be(0m);
        sut.Audit.Modified.UserId.Should().Be("KASIR-01");
    }

    [Fact]
    public void CloseShift_WithDiscrepancy_ShouldReflectSelisihKas()
    {
        // Arrange: Saldo sistem = 500k + 1.000k - 100k = 1.400k. Fisik = 1.350k => Selisih -50k
        var sut = KasirShiftModel.OpenShift("2", "KASIR-02", "Loket 02", 500_000m, OpenTime);

        // Act
        sut.CloseShift(
            saldoAkhirFisik: 1_350_000m,
            totalTerima: 1_000_000m,
            totalKeluar: 100_000m,
            closeTime: OpenTime.AddHours(7));

        // Assert
        sut.SaldoAkhirSistem.Should().Be(1_400_000m);
        sut.SaldoAkhirFisik.Should().Be(1_350_000m);
        sut.SelisihKas.Should().Be(-50_000m);
    }

    [Fact]
    public void CloseShift_WhenAlreadyClosed_ShouldThrowBusinessValidationException()
    {
        // Arrange
        var sut = KasirShiftModel.OpenShift("1", "KASIR-01", "Loket 01", 200_000m, OpenTime);
        sut.CloseShift(200_000m, 0m, 0m, OpenTime.AddHours(7));

        // Act
        var act = () => sut.CloseShift(200_000m, 0m, 0m, OpenTime.AddHours(8));

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*sudah berstatus TUTUP*");
    }

    [Fact]
    public void UpdateRekapSistem_WhenShiftIsOpen_ShouldUpdateTotals()
    {
        // Arrange
        var sut = KasirShiftModel.OpenShift("1", "KASIR-01", "Loket 01", 300_000m, OpenTime);

        // Act
        sut.UpdateRekapSistem(1_500_000m, 100_000m);

        // Assert
        sut.TotalTerimaSistem.Should().Be(1_500_000m);
        sut.TotalKeluarSistem.Should().Be(100_000m);
        sut.SaldoAkhirSistem.Should().Be(1_700_000m);
    }

    [Fact]
    public void UpdateRekapSistem_WhenShiftIsClosed_ShouldThrowBusinessValidationException()
    {
        // Arrange
        var sut = KasirShiftModel.OpenShift("1", "KASIR-01", "Loket 01", 300_000m, OpenTime);
        sut.CloseShift(300_000m, 0m, 0m, OpenTime.AddHours(7));

        // Act
        var act = () => sut.UpdateRekapSistem(500_000m, 50_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*sudah TUTUP*");
    }
}
