using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature;

public class TrsKasirItemTypeTest
{
    [Fact]
    public void Create_WithValidParameters_InitializesCorrectly()
    {
        // Act
        var sut = TrsKasirItemType.Create(
            billId: "BIL-202611-001",
            regId: "REG-001",
            layananId: "LAY01",
            keterangan: "Pemeriksaan Dokter Spesialis",
            nilaiBill: 150_000m,
            nilaiBayar: 150_000m,
            noUrut: 1,
            trsKasirId: "DX0353231A");

        // Assert
        sut.KasirItemId.Should().Be("DX0353231A-01");
        sut.TrsKasirId.Should().Be("DX0353231A");
        sut.NoUrut.Should().Be(1);
        sut.BillId.Should().Be("BIL-202611-001");
        sut.RegId.Should().Be("REG-001");
        sut.LayananId.Should().Be("LAY01");
        sut.Keterangan.Should().Be("Pemeriksaan Dokter Spesialis");
        sut.NilaiBill.Should().Be(150_000m);
        sut.NilaiBayar.Should().Be(150_000m);
    }

    [Fact]
    public void Create_WithoutTrsKasirId_GeneratesFallbackSequentialId()
    {
        // Act
        var sut = TrsKasirItemType.Create(
            billId: "BIL-202611-002",
            regId: "REG-002",
            layananId: "LAY02",
            keterangan: "Obat Farmasi Rawat Jalan",
            nilaiBill: 75_000m,
            nilaiBayar: 50_000m,
            noUrut: 2);

        // Assert
        sut.KasirItemId.Should().Be("02");
        sut.TrsKasirId.Should().BeEmpty();
        sut.NoUrut.Should().Be(2);
        sut.NilaiBill.Should().Be(75_000m);
        sut.NilaiBayar.Should().Be(50_000m);
    }

    [Fact]
    public void WithTransaction_UpdatesTrsKasirIdAndDeterministicKasirItemId()
    {
        // Arrange
        var initial = TrsKasirItemType.Create(
            billId: "BIL-003",
            regId: "REG-003",
            layananId: "LAY03",
            keterangan: "Laboratorium Darah Lengkap",
            nilaiBill: 200_000m,
            nilaiBayar: 200_000m);

        // Act
        var sut = initial.WithTransaction("DX12345678", 5);

        // Assert
        sut.KasirItemId.Should().Be("DX12345678-05");
        sut.TrsKasirId.Should().Be("DX12345678");
        sut.NoUrut.Should().Be(5);
        sut.BillId.Should().Be("BIL-003");
        sut.NilaiBill.Should().Be(200_000m);
        sut.NilaiBayar.Should().Be(200_000m);
    }

    [Fact]
    public void Constructor_WithCustomKasirItemId_PreservesGivenId()
    {
        // Act
        var sut = new TrsKasirItemType(
            kasirItemId: "CUSTOM-ITEM-99",
            trsKasirId: "DX00000001",
            noUrut: 1,
            billId: "BIL-001",
            regId: "REG-001",
            layananId: "LAY01",
            keterangan: "Item test",
            nilaiBill: 100_000m,
            nilaiBayar: 100_000m);

        // Assert
        sut.KasirItemId.Should().Be("CUSTOM-ITEM-99");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenBillIdIsEmpty_ShouldThrow(string? invalidBillId)
    {
        // Act
        var act = () => TrsKasirItemType.Create(
            billId: invalidBillId!,
            regId: "REG-001",
            layananId: "LAY01",
            keterangan: "Test",
            nilaiBill: 100_000m,
            nilaiBayar: 100_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*BillId wajib diisi*");
    }

    [Fact]
    public void Validate_WhenNilaiBillIsNegative_ShouldThrow()
    {
        // Act
        var act = () => TrsKasirItemType.Create(
            billId: "BIL-001",
            regId: "REG-001",
            layananId: "LAY01",
            keterangan: "Test",
            nilaiBill: -10_000m,
            nilaiBayar: 0m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nilai bill tidak boleh negatif*");
    }

    [Fact]
    public void Validate_WhenNilaiBayarIsNegative_ShouldThrow()
    {
        // Act
        var act = () => TrsKasirItemType.Create(
            billId: "BIL-001",
            regId: "REG-001",
            layananId: "LAY01",
            keterangan: "Test",
            nilaiBill: 10_000m,
            nilaiBayar: -5_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nilai bayar item tidak boleh negatif*");
    }

    [Fact]
    public void Validate_WhenNilaiBayarExceedsNilaiBill_ShouldThrow()
    {
        // Act
        var act = () => TrsKasirItemType.Create(
            billId: "BIL-001",
            regId: "REG-001",
            layananId: "LAY01",
            keterangan: "Test",
            nilaiBill: 50_000m,
            nilaiBayar: 75_000m);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nilai bayar item tidak boleh melebihi nilai bill*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenNoUrutIsZeroOrNegative_ShouldThrow(int invalidNoUrut)
    {
        // Act
        var act = () => TrsKasirItemType.Create(
            billId: "BIL-001",
            regId: "REG-001",
            layananId: "LAY01",
            keterangan: "Test",
            nilaiBill: 50_000m,
            nilaiBayar: 50_000m,
            noUrut: invalidNoUrut);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nomor urut item kasir harus lebih besar dari nol*");
    }
}
