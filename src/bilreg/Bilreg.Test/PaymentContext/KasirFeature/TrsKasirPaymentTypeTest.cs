using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature;

public class TrsKasirPaymentTypeTest
{
    [Fact]
    public void Create_WithTrsKasirIdAndNoUrut_GeneratesDeterministicPaymentRowId()
    {
        // Act
        var sut = TrsKasirPaymentType.Create(
            metodeBayar: KasirConst.MetodeBayarTunai,
            nilaiBayar: 150_000m,
            nilaiKembalian: 20_000m,
            noUrut: 1,
            trsKasirId: "DX0353231A",
            kodeJenisPayment: "01",
            namaMetodeBayar: "TUNAI",
            coaId: "1110201");

        // Assert
        sut.PaymentRowId.Should().Be("DX0353231A-01");
        sut.TrsKasirId.Should().Be("DX0353231A");
        sut.NoUrut.Should().Be(1);
        sut.MetodeBayar.Should().Be(KasirConst.MetodeBayarTunai);
        sut.NilaiBayar.Should().Be(150_000m);
        sut.NilaiKembalian.Should().Be(20_000m);
        sut.NilaiNetto.Should().Be(130_000m);
        sut.KodeJenisPayment.Should().Be("01");
        sut.NamaMetodeBayar.Should().Be("TUNAI");
        sut.CoaId.Should().Be("1110201");
    }

    [Fact]
    public void Create_WithoutTrsKasirId_GeneratesSequentialFallbackRowId()
    {
        // Act
        var sut = TrsKasirPaymentType.Create(
            metodeBayar: KasirConst.MetodeBayarDebit,
            nilaiBayar: 200_000m,
            noUrut: 2,
            nomorKartu: "1234-5678-9012-3456",
            nomorReferensi: "REF-001",
            namaBank: "BCA",
            kodeJenisPayment: "02",
            namaMetodeBayar: "BCA DEBIT",
            coaId: "1110301");

        // Assert
        sut.PaymentRowId.Should().Be("02");
        sut.TrsKasirId.Should().BeEmpty();
        sut.NoUrut.Should().Be(2);
        sut.NilaiNetto.Should().Be(200_000m);
        sut.KodeJenisPayment.Should().Be("02");
        sut.NamaMetodeBayar.Should().Be("BCA DEBIT");
        sut.CoaId.Should().Be("1110301");
    }

    [Fact]
    public void WithTransaction_UpdatesTrsKasirIdAndProducesDeterministicRowId()
    {
        // Arrange
        var initial = TrsKasirPaymentType.Create(
            metodeBayar: KasirConst.MetodeBayarQris,
            nilaiBayar: 75_000m,
            nomorReferensi: "QRIS-999",
            kodeJenisPayment: "05",
            namaMetodeBayar: "QRIS STATIS",
            coaId: "1110501");

        // Act
        var sut = initial.WithTransaction("DX99999999", 3);

        // Assert
        sut.PaymentRowId.Should().Be("DX99999999-03");
        sut.TrsKasirId.Should().Be("DX99999999");
        sut.NoUrut.Should().Be(3);
        sut.MetodeBayar.Should().Be(KasirConst.MetodeBayarQris);
        sut.NilaiBayar.Should().Be(75_000m);
        sut.NilaiNetto.Should().Be(75_000m);
        sut.KodeJenisPayment.Should().Be("05");
        sut.NamaMetodeBayar.Should().Be("QRIS STATIS");
        sut.CoaId.Should().Be("1110501");
        sut.NomorReferensi.Should().Be("QRIS-999");
    }

    [Fact]
    public void Constructor_WithExplicitPaymentRowId_PreservesGivenId()
    {
        // Act
        var sut = new TrsKasirPaymentType(
            paymentRowId: "CUSTOM-ROW-01",
            trsKasirId: "DX00000001",
            noUrut: 1,
            metodeBayar: KasirConst.MetodeBayarTunai,
            nilaiBayar: 50_000m);

        // Assert
        sut.PaymentRowId.Should().Be("CUSTOM-ROW-01");
        sut.TrsKasirId.Should().Be("DX0353231A" != "DX00000001" ? "DX00000001" : sut.TrsKasirId);
    }

    [Fact]
    public void NilaiNetto_ShouldAlwaysEqualNilaiBayarMinusNilaiKembalian()
    {
        // Act
        var sut = TrsKasirPaymentType.Create(
            metodeBayar: KasirConst.MetodeBayarTunai,
            nilaiBayar: 100_000m,
            nilaiKembalian: 35_000m);

        // Assert
        sut.NilaiNetto.Should().Be(65_000m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenMetodeBayarIsEmpty_ShouldThrow(string? invalidMetode)
    {
        // Act
        var act = () => TrsKasirPaymentType.Create(
            metodeBayar: invalidMetode!,
            nilaiBayar: 10_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Metode bayar wajib diisi*");
    }

    [Fact]
    public void Validate_WhenMetodeBayarIsUnknown_ShouldThrow()
    {
        // Act
        var act = () => TrsKasirPaymentType.Create(
            metodeBayar: "CRYPTO",
            nilaiBayar: 10_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Metode bayar 'CRYPTO' tidak valid*");
    }

    [Fact]
    public void Validate_WhenNilaiBayarIsNegative_ShouldThrow()
    {
        // Act
        var act = () => TrsKasirPaymentType.Create(
            metodeBayar: KasirConst.MetodeBayarTunai,
            nilaiBayar: -5_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nilai bayar tidak boleh negatif*");
    }

    [Fact]
    public void Validate_WhenNilaiKembalianIsNegative_ShouldThrow()
    {
        // Act
        var act = () => TrsKasirPaymentType.Create(
            metodeBayar: KasirConst.MetodeBayarTunai,
            nilaiBayar: 10_000m,
            nilaiKembalian: -1_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nilai kembalian tidak boleh negatif*");
    }

    [Fact]
    public void Validate_WhenNonCashHasKembalian_ShouldThrow()
    {
        // Act
        var act = () => TrsKasirPaymentType.Create(
            metodeBayar: KasirConst.MetodeBayarDebit,
            nilaiBayar: 100_000m,
            nilaiKembalian: 5_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nilai kembalian hanya diizinkan untuk metode pembayaran TUNAI*");
    }

    [Fact]
    public void Validate_WhenCashKembalianExceedsNilaiBayar_ShouldThrow()
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
}
