using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.BrgFeature.UseCases;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrgContext.BrgFeature.UseCases;

public class BrgSetAktifCommandTest
{
    private readonly Mock<IBrgRepo> _brgRepoMock = new();

    private BrgSetAktifHandler CreateSut() => new(_brgRepoMock.Object);

    private static BarangModel CreateFakeBarangModel(string id, bool isAktif)
    {
        return BarangModel.Create(
            brgId: id,
            brgName: "Test Barang",
            isAktif: isAktif,
            ketBarang: "Keterangan",
            groupRek: new GroupRekReff("G01", "Grup Obat"),
            groupRekDk: new GroupRekDkType("OBT", "Obat"),
            pabrik: new PabrikReff("P01", "Pabrik Test"),
            satuanKecil: new SatuanType("TAB", "Tablet"),
            satuanBesar: new SatuanType("TAB", "Tablet"),
            konversi: 1
        );
    }

    [Fact]
    public async Task Handle_WhenBarangExists_UpdatesIsAktifToFalse_AndSavesChanges()
    {
        // Arrange
        var existing = CreateFakeBarangModel("BRG0001", isAktif: true);
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG0001")))
            .Returns(MayBe.From<IBrg>(existing));

        var sut = CreateSut();
        var cmd = new BrgSetAktifCommand("BRG0001", IsAktif: false);

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _brgRepoMock.Verify(x => x.SaveChanges(It.Is<IBrg>(b =>
            b.BrgId == "BRG0001" &&
            b.IsAktif == false
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBarangExists_UpdatesIsAktifToTrue_AndSavesChanges()
    {
        // Arrange
        var existing = CreateFakeBarangModel("BRG0001", isAktif: false);
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG0001")))
            .Returns(MayBe.From<IBrg>(existing));

        var sut = CreateSut();
        var cmd = new BrgSetAktifCommand("BRG0001", IsAktif: true);

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _brgRepoMock.Verify(x => x.SaveChanges(It.Is<IBrg>(b =>
            b.BrgId == "BRG0001" &&
            b.IsAktif == true
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBarangNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe<IBrg>.None);

        var sut = CreateSut();
        var cmd = new BrgSetAktifCommand("BRG_NOT_FOUND", IsAktif: false);

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Barang 'BRG_NOT_FOUND' tidak ditemukan*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenBrgIdIsNullOrEmpty_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var sut = CreateSut();
        var cmd = new BrgSetAktifCommand(invalidId, IsAktif: false);

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenLoadedEntityIsMockIBrg_RecreatesModelAndSavesChanges()
    {
        // Arrange
        var mockBrg = new Mock<IBrg>();
        mockBrg.Setup(x => x.BrgId).Returns("BRG0002");
        mockBrg.Setup(x => x.BrgName).Returns("Mocked Barang");
        mockBrg.Setup(x => x.IsAktif).Returns(true);
        mockBrg.Setup(x => x.KetBarang).Returns("Ket");
        mockBrg.Setup(x => x.GroupRek).Returns(new GroupRekReff("G01", "Grup"));
        mockBrg.Setup(x => x.GroupRekDk).Returns(new GroupRekDkType("OBT", "Obat"));
        mockBrg.Setup(x => x.Pabrik).Returns(new PabrikReff("P01", "Pabrik"));
        mockBrg.Setup(x => x.SatuanKecil).Returns(new SatuanType("TAB", "Tab"));
        mockBrg.Setup(x => x.SatuanBesar).Returns(new SatuanType("TAB", "Tab"));
        mockBrg.Setup(x => x.Konversi).Returns(1);

        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG0002")))
            .Returns(MayBe.From(mockBrg.Object));

        var sut = CreateSut();
        var cmd = new BrgSetAktifCommand("BRG0002", IsAktif: false);

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _brgRepoMock.Verify(x => x.SaveChanges(It.Is<IBrg>(b =>
            b.BrgId == "BRG0002" &&
            b.IsAktif == false
        )), Times.Once);
    }
}
