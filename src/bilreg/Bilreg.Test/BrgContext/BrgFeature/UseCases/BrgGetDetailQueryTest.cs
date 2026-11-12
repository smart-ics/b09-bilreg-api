using Ardalis.GuardClauses;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.BrgFeature.UseCases;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrgContext.BrgFeature.UseCases;

public class BrgGetDetailQueryTest
{
    private readonly Mock<IBrgRepo> _brgRepoMock = new();

    private BrgGetDetailHandler CreateSut() => new(_brgRepoMock.Object);

    [Fact]
    public async Task Handle_WhenBarangExists_ReturnsCompleteDetailResponse()
    {
        // Arrange
        var barang = BarangModel.Create(
            brgId: "BRG001",
            brgName: "Paracetamol 500mg",
            isAktif: true,
            ketBarang: "Analgesik antipiretik",
            groupRek: new GroupRekReff("GR01", "Grup Obat"),
            groupRekDk: new GroupRekDkType("OBT", "Obat"),
            pabrik: new PabrikReff("PB01", "PT Kimia Farma"),
            satuanKecil: new SatuanType("TAB", "Tablet"),
            satuanBesar: new SatuanType("STRIP", "Strip"),
            konversi: 10
        );

        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG001")))
            .Returns(MayBe.From<IBrg>(barang));

        var query = new BrgGetDetailQuery("BRG001");
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BrgId.Should().Be("BRG001");
        result.BrgName.Should().Be("Paracetamol 500mg");
        result.IsAktif.Should().BeTrue();
        result.KetBarang.Should().Be("Analgesik antipiretik");
        result.GroupRekId.Should().Be("GR01");
        result.GroupRekName.Should().Be("Grup Obat");
        result.GroupRekDkId.Should().Be("OBT");
        result.GroupRekDkName.Should().Be("Obat");
        result.PabrikId.Should().Be("PB01");
        result.PabrikName.Should().Be("PT Kimia Farma");
        result.SatuanKecilId.Should().Be("TAB");
        result.SatuanKecilName.Should().Be("Tablet");
        result.SatuanBesarId.Should().Be("STRIP");
        result.SatuanBesarName.Should().Be("Strip");
        result.Konversi.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WhenSingleSatuan_ReturnsIdenticalSatuanKecilAndBesarWithKonversiOne()
    {
        // Arrange
        var barang = BarangModel.Create(
            brgId: "BHP001",
            brgName: "Kassa Steril",
            isAktif: true,
            ketBarang: "Kassa penutup luka",
            groupRek: new GroupRekReff("GR02", "Grup BHP"),
            groupRekDk: new GroupRekDkType("BHP", "Bahan Habis Pakai"),
            pabrik: new PabrikReff("PB02", "PT OneMed"),
            satuanKecil: new SatuanType("PCS", "Pieces"),
            satuanBesar: new SatuanType("PCS", "Pieces"),
            konversi: 1
        );

        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BHP001")))
            .Returns(MayBe.From<IBrg>(barang));

        var query = new BrgGetDetailQuery("BHP001");
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.SatuanKecilId.Should().Be("PCS");
        result.SatuanBesarId.Should().Be("PCS");
        result.Konversi.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenBarangIsInactive_ReturnsIsAktifFalse()
    {
        // Arrange
        var barang = BarangModel.Create(
            brgId: "ATK001",
            brgName: "Kertas A4",
            isAktif: false,
            ketBarang: "Alat tulis kantor nonaktif",
            groupRek: new GroupRekReff("GR03", "Grup ATK"),
            groupRekDk: new GroupRekDkType("ATK", "Alat Tulis Kantor"),
            pabrik: new PabrikReff("PB03", "PT Sinar Mas"),
            satuanKecil: new SatuanType("RIM", "Rim"),
            satuanBesar: new SatuanType("BOX", "Box"),
            konversi: 5
        );

        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "ATK001")))
            .Returns(MayBe.From<IBrg>(barang));

        var query = new BrgGetDetailQuery("ATK001");
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsAktif.Should().BeFalse();
        result.GroupRekDkId.Should().Be("ATK");
    }

    [Fact]
    public async Task Handle_WhenBarangNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe<IBrg>.None);

        var query = new BrgGetDetailQuery("NONEXIST");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Barang with Id: NONEXIST not found.*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenBrgIdIsNullOrWhitespace_ThrowsArgumentException(string? brgId)
    {
        // Arrange
        var query = new BrgGetDetailQuery(brgId!);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
