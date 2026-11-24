using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature;

public class LoketKasirModelTest
{
    [Fact]
    public void Create_WithValidParameters_InitializesCorrectly()
    {
        // Act
        var sut = LoketKasirModel.Create(
            loketId: "LOK-INAP",
            namaLoket: "Loket Kasir Rawat Inap",
            coaId: "1110201",
            namaCoa: "KAS KASIR RAWAT INAP",
            keterangan: "Loket Kasir Gedung Rawat Inap");

        // Assert
        sut.LoketId.Should().Be("LOK-INAP");
        sut.NamaLoket.Should().Be("Loket Kasir Rawat Inap");
        sut.CoaId.Should().Be("1110201");
        sut.NamaCoa.Should().Be("KAS KASIR RAWAT INAP");
        sut.IsAktif.Should().BeTrue();
        sut.Keterangan.Should().Be("Loket Kasir Gedung Rawat Inap");
    }

    [Fact]
    public void Constructor_WithAllParameters_InitializesCorrectly()
    {
        // Act
        var sut = new LoketKasirModel(
            loketId: "LOK-IGD",
            namaLoket: "Loket Kasir IGD",
            coaId: "1110202",
            namaCoa: "KAS KASIR IGD",
            isAktif: false,
            keterangan: "Sedang renovasi");

        // Assert
        sut.LoketId.Should().Be("LOK-IGD");
        sut.NamaLoket.Should().Be("Loket Kasir IGD");
        sut.CoaId.Should().Be("1110202");
        sut.NamaCoa.Should().Be("KAS KASIR IGD");
        sut.IsAktif.Should().BeFalse();
        sut.Keterangan.Should().Be("Sedang renovasi");
    }

    [Fact]
    public void Update_ModifiesPropertiesAndTrimsValues()
    {
        // Arrange
        var sut = LoketKasirModel.Create(
            loketId: "LOK-POLI",
            namaLoket: "Loket Kasir Poliklinik",
            coaId: "1110203",
            namaCoa: "KAS KASIR POLIKLINIK",
            keterangan: "Lama");

        // Act
        sut.Update("  Loket Kasir Eksekutif  ", "  1110204  ", "  KAS EKSEKUTIF  ", "  Gedung Baru  ");

        // Assert
        sut.NamaLoket.Should().Be("Loket Kasir Eksekutif");
        sut.CoaId.Should().Be("1110204");
        sut.NamaCoa.Should().Be("KAS EKSEKUTIF");
        sut.Keterangan.Should().Be("Gedung Baru");
    }

    [Fact]
    public void Aktifkan_And_NonAktifkan_TogglesIsAktifState()
    {
        // Arrange
        var sut = LoketKasirModel.Create("LOK-01", "Loket 1", "1110201");

        // Act & Assert
        sut.NonAktifkan();
        sut.IsAktif.Should().BeFalse();

        sut.Aktifkan();
        sut.IsAktif.Should().BeTrue();

        sut.SetAktif(false);
        sut.IsAktif.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenLoketIdIsEmpty_ShouldThrow(string? invalidLoketId)
    {
        // Act
        var act = () => LoketKasirModel.Create(invalidLoketId!, "Loket A", "1110201");

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*LoketId wajib diisi*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenNamaLoketIsEmpty_ShouldThrow(string? invalidNamaLoket)
    {
        // Act
        var act = () => LoketKasirModel.Create("LOK-01", invalidNamaLoket!, "1110201");

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nama loket wajib diisi*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WhenCoaIdIsEmpty_ShouldThrow(string? invalidCoaId)
    {
        // Act
        var act = () => LoketKasirModel.Create("LOK-01", "Loket A", invalidCoaId!);

        // Assert
        act.Should().Throw<BusinessValidationException>()
            .WithMessage("*CoaId wajib diisi*");
    }

    [Fact]
    public void Validate_WhenUpdateWithEmptyRequiredFields_ShouldThrow()
    {
        // Arrange
        var sut = LoketKasirModel.Create("LOK-01", "Loket A", "1110201");

        // Act & Assert
        var actEmptyNama = () => sut.Update("", "1110201", "KAS", "");
        actEmptyNama.Should().Throw<BusinessValidationException>()
            .WithMessage("*Nama loket wajib diisi*");

        var actEmptyCoa = () => sut.Update("Loket A", "", "KAS", "");
        actEmptyCoa.Should().Throw<BusinessValidationException>()
            .WithMessage("*CoaId wajib diisi*");
    }
}
