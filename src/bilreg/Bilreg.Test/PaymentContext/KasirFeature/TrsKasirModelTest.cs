using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature;

public class TrsKasirModelTest
{
    private static readonly DateTime Now = new(2026, 11, 7, 10, 30, 0);

    [Fact]
    public void CreateTerimaKas_WithValidSingleCashPayment_ShouldCreateAggregateSuccessfully()
    {
        // Arrange
        var payments = new[]
        {
            TrsKasirPaymentType.Create(
                metodeBayar: KasirConst.MetodeBayarTunai,
                nilaiBayar: 100_000m,
                nilaiKembalian: 20_000m)
        };

        // Act
        var sut = TrsKasirModel.CreateTerimaKas(
            shiftId: "SH00000001",
            kasirId: "KASIR-01",
            regId: "REG-001",
            pasienId: "RM-001",
            pasienName: "Tn. Budi Santoso",
            orderRef: "BILL-001",
            keterangan: "Pembayaran rawat jalan",
            totalNilai: 80_000m,
            payments: payments,
            trsTime: Now);

        // Assert
        sut.TrsKasirId.Should().StartWith("DX");
        sut.TrsKasirId.Length.Should().Be(10);
        sut.JenisTrs.Should().Be(KasirConst.JenisTrsTerima);
        sut.ShiftId.Should().Be("SH00000001");
        sut.KasirId.Should().Be("KASIR-01");
        sut.RegId.Should().Be("REG-001");
        sut.PasienId.Should().Be("RM-001");
        sut.PasienName.Should().Be("Tn. Budi Santoso");
        sut.OrderRef.Should().Be("BILL-001");
        sut.Keterangan.Should().Be("Pembayaran rawat jalan");
        sut.TotalNilai.Should().Be(80_000m);
        sut.StatusTrs.Should().Be(KasirConst.StatusTrsBerhasil);
        sut.IsBerhasil.Should().BeTrue();
        sut.IsVoid.Should().BeFalse();
        sut.TglTrs.Should().Be("2026-11-07");
        sut.JamTrs.Should().Be("10:30:00");
        sut.TglJamTrsStr.Should().Be("2026-11-07 10:30:00");

        sut.Payments.Should().HaveCount(1);
        var p = sut.Payments.First();
        p.PaymentRowId.Should().Be($"{sut.TrsKasirId}-01");
        p.TrsKasirId.Should().Be(sut.TrsKasirId);
        p.NoUrut.Should().Be(1);
        p.MetodeBayar.Should().Be(KasirConst.MetodeBayarTunai);
        p.NilaiBayar.Should().Be(100_000m);
        p.NilaiKembalian.Should().Be(20_000m);
        p.NilaiNetto.Should().Be(80_000m);

        sut.Audit.Created.UserId.Should().Be("KASIR-01");
        sut.Audit.IsVoided.Should().BeFalse();
    }

    [Fact]
    public void CreateTerimaKas_WithMultiPayment_ShouldCalculateNetAndMapSequenceCorrectly()
    {
        // Arrange: Total 150_000 = QRIS 50_000 + DEBIT 50_000 + TUNAI 70_000 (kembalian 20_000)
        var payments = new[]
        {
            TrsKasirPaymentType.Create(
                metodeBayar: KasirConst.MetodeBayarQris,
                nilaiBayar: 50_000m,
                nilaiKembalian: 0m,
                nomorReferensi: "QRIS-REF-999"),
            TrsKasirPaymentType.Create(
                metodeBayar: KasirConst.MetodeBayarDebit,
                nilaiBayar: 50_000m,
                nilaiKembalian: 0m,
                nomorKartu: "1234-5678-9012-3456",
                nomorReferensi: "APPR-1234",
                namaBank: "BCA"),
            TrsKasirPaymentType.Create(
                metodeBayar: KasirConst.MetodeBayarTunai,
                nilaiBayar: 70_000m,
                nilaiKembalian: 20_000m)
        };

        // Act
        var sut = TrsKasirModel.CreateTerimaKas(
            shiftId: "SH00000001",
            kasirId: "KASIR-01",
            regId: "REG-002",
            pasienId: "RM-002",
            pasienName: "Ny. Siti Aminah",
            orderRef: "BILL-002",
            keterangan: "Pembayaran multi-payment",
            totalNilai: 150_000m,
            payments: payments,
            trsTime: Now);

        // Assert
        sut.TotalNilai.Should().Be(150_000m);
        sut.Payments.Should().HaveCount(3);
        sut.Payments[0].PaymentRowId.Should().Be($"{sut.TrsKasirId}-01");
        sut.Payments[0].NoUrut.Should().Be(1);
        sut.Payments[0].TrsKasirId.Should().Be(sut.TrsKasirId);
        sut.Payments[1].PaymentRowId.Should().Be($"{sut.TrsKasirId}-02");
        sut.Payments[1].NoUrut.Should().Be(2);
        sut.Payments[1].TrsKasirId.Should().Be(sut.TrsKasirId);
        sut.Payments[2].PaymentRowId.Should().Be($"{sut.TrsKasirId}-03");
        sut.Payments[2].NoUrut.Should().Be(3);
        sut.Payments[2].TrsKasirId.Should().Be(sut.TrsKasirId);

        var totalNetto = sut.Payments.Sum(x => x.NilaiNetto);
        totalNetto.Should().Be(150_000m);
    }

    [Fact]
    public void CreateTerimaKas_WhenPaymentSumDoesNotMatchTotalNilai_ShouldThrowBusinessValidationException()
    {
        // Arrange: Total 100_000, tapi bayar hanya 90_000
        var payments = new[]
        {
            TrsKasirPaymentType.Create(
                metodeBayar: KasirConst.MetodeBayarTunai,
                nilaiBayar: 90_000m,
                nilaiKembalian: 0m)
        };

        // Act
        var act = () => TrsKasirModel.CreateTerimaKas(
            shiftId: "SH00000001",
            kasirId: "KASIR-01",
            regId: "REG-001",
            pasienId: "RM-001",
            pasienName: "Tn. Budi",
            orderRef: "BILL-001",
            keterangan: "Test underpaid",
            totalNilai: 100_000m,
            payments: payments,
            trsTime: Now);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*tidak sesuai dengan TotalNilai*");
    }

    [Fact]
    public void CreateTerimaKas_WhenNonCashPaymentHasKembalian_ShouldThrowBusinessValidationException()
    {
        // Act
        var act = () => TrsKasirPaymentType.Create(
            metodeBayar: KasirConst.MetodeBayarDebit,
            nilaiBayar: 100_000m,
            nilaiKembalian: 10_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nilai kembalian hanya diizinkan untuk metode pembayaran TUNAI*");
    }

    [Fact]
    public void CreateTerimaKas_WhenCashPaymentHasKembalianGreaterThanNilaiBayar_ShouldThrowBusinessValidationException()
    {
        // Act
        var act = () => TrsKasirPaymentType.Create(
            metodeBayar: KasirConst.MetodeBayarTunai,
            nilaiBayar: 50_000m,
            nilaiKembalian: 60_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nilai kembalian tidak boleh melebihi nilai bayar tunai*");
    }

    [Fact]
    public void CreateTerimaKas_WhenTotalNilaiIsZeroOrNegative_ShouldThrowBusinessValidationException()
    {
        var payments = new[]
        {
            TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 0m)
        };

        var act = () => TrsKasirModel.CreateTerimaKas(
            shiftId: "SH00000001",
            kasirId: "KASIR-01",
            regId: "REG-001",
            pasienId: "RM-001",
            pasienName: "Tn. Budi",
            orderRef: "BILL-001",
            keterangan: "Test zero",
            totalNilai: 0m,
            payments: payments,
            trsTime: Now);

        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Total nilai transaksi kasir harus lebih besar dari nol*");
    }

    [Fact]
    public void CreateKeluarKas_WithValidAmount_ShouldCreateKeluarKasTransaction()
    {
        // Act
        var sut = TrsKasirModel.CreateKeluarKas(
            shiftId: "SH00000001",
            kasirId: "KASIR-01",
            regId: "REG-001",
            pasienId: "RM-001",
            pasienName: "Tn. Budi",
            orderRef: "DEP-001",
            keterangan: "Pengembalian sisa deposit pasien pulang",
            totalNilai: 250_000m,
            metodeBayar: KasirConst.MetodeBayarTunai,
            trsTime: Now);

        // Assert
        sut.TrsKasirId.Should().StartWith("DX");
        sut.JenisTrs.Should().Be(KasirConst.JenisTrsKeluar);
        sut.TotalNilai.Should().Be(250_000m);
        sut.StatusTrs.Should().Be(KasirConst.StatusTrsBerhasil);
        sut.Payments.Should().HaveCount(1);
        sut.Payments.First().NilaiBayar.Should().Be(250_000m);
        sut.Payments.First().NilaiKembalian.Should().Be(0m);
    }

    [Fact]
    public void Void_WhenTransactionIsActive_ShouldUpdateStatusAndAuditTrail()
    {
        // Arrange
        var payments = new[]
        {
            TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 100_000m)
        };
        var sut = TrsKasirModel.CreateTerimaKas(
            shiftId: "SH00000001",
            kasirId: "KASIR-01",
            regId: "REG-001",
            pasienId: "RM-001",
            pasienName: "Tn. Budi",
            orderRef: "BILL-001",
            keterangan: "Test void",
            totalNilai: 100_000m,
            payments: payments,
            trsTime: Now);

        var voidTime = Now.AddMinutes(15);

        // Act
        sut.Void("SUPERVISOR-01", "Salah input order billing", voidTime);

        // Assert
        sut.StatusTrs.Should().Be(KasirConst.StatusTrsVoid);
        sut.IsVoid.Should().BeTrue();
        sut.IsBerhasil.Should().BeFalse();
        sut.PetugasVoid.Should().Be("SUPERVISOR-01");
        sut.AlasanVoid.Should().Be("Salah input order billing");
        sut.TglVoid.Should().Be("2026-11-07");
        sut.JamVoid.Should().Be("10:45:00");
        sut.Audit.IsVoided.Should().BeTrue();
        sut.Audit.Voided.UserId.Should().Be("SUPERVISOR-01");
    }

    [Fact]
    public void Void_WhenTransactionIsAlreadyVoided_ShouldThrowBusinessValidationException()
    {
        // Arrange
        var payments = new[]
        {
            TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 100_000m)
        };
        var sut = TrsKasirModel.CreateTerimaKas(
            shiftId: "SH00000001",
            kasirId: "KASIR-01",
            regId: "REG-001",
            pasienId: "RM-001",
            pasienName: "Tn. Budi",
            orderRef: "BILL-001",
            keterangan: "Test double void",
            totalNilai: 100_000m,
            payments: payments,
            trsTime: Now);

        sut.Void("SUPERVISOR-01", "Pembatalan pertama", Now.AddMinutes(10));

        // Act
        var act = () => sut.Void("SUPERVISOR-02", "Pembatalan kedua", Now.AddMinutes(20));

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*sudah berstatus VOID*");
    }

    [Fact]
    public void Void_WhenPetugasVoidOrAlasanIsEmpty_ShouldThrowBusinessValidationException()
    {
        // Arrange
        var payments = new[]
        {
            TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 100_000m)
        };
        var sut = TrsKasirModel.CreateTerimaKas(
            shiftId: "SH00000001",
            kasirId: "KASIR-01",
            regId: "REG-001",
            pasienId: "RM-001",
            pasienName: "Tn. Budi",
            orderRef: "BILL-001",
            keterangan: "Test invalid void params",
            totalNilai: 100_000m,
            payments: payments,
            trsTime: Now);

        // Act & Assert
        var actNoPetugas = () => sut.Void("", "Ada alasan", Now.AddMinutes(10));
        actNoPetugas.Should().Throw<BusinessValidationException>()
            .WithMessage("*Petugas void wajib diisi*");

        var actNoAlasan = () => sut.Void("SUPERVISOR-01", "", Now.AddMinutes(10));
        actNoAlasan.Should().Throw<BusinessValidationException>()
            .WithMessage("*Alasan void wajib diisi*");
    }
}
