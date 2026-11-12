using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.BrgFeature.UseCases;
using Bilreg.Application.BrgContext.KlasifikasiFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrgContext.BrgFeature.UseCases;

public class BrgRegisterCommandTest
{
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<IGroupRekRepo> _groupRekRepoMock = new();
    private readonly Mock<IPabrikRepo> _pabrikRepoMock = new();
    private readonly Mock<ISatuanRepo> _satuanRepoMock = new();

    private BrgRegisterHandler CreateSut() =>
        new(_brgRepoMock.Object,
            _groupRekRepoMock.Object,
            _pabrikRepoMock.Object,
            _satuanRepoMock.Object);

    private readonly GroupRekType _fakeGroupRek = new(
        "G01", "Grup Obat Bebas", new GroupRekDkType("OBT", "Obat"));
    private readonly PabrikType _fakePabrik = new("P01", "Kalbe Farma");
    private readonly SatuanType _fakeSatuanKecil = new("TAB", "Tablet");
    private readonly SatuanType _fakeSatuanBesar = new("STR", "Strip");

    public BrgRegisterCommandTest()
    {
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe<IBrg>.None);
        _groupRekRepoMock.Setup(x => x.LoadEntity(It.IsAny<IGroupRekKey>()))
            .Returns(MayBe.From(_fakeGroupRek));
        _pabrikRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPabrikKey>()))
            .Returns(MayBe.From(_fakePabrik));
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "TAB")))
            .Returns(MayBe.From(_fakeSatuanKecil));
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "STR")))
            .Returns(MayBe.From(_fakeSatuanBesar));
    }

    [Fact]
    public async Task Handle_WhenValidSingleSatuan_SavesBarangWithIsAktifTrue_AndReturnsResponse()
    {
        // Arrange
        var cmd = new BrgRegisterCommand(
            BrgId: "BRG0001",
            BrgName: "Paracetamol 500mg",
            KetBarang: "Obat Penurun Panas",
            GroupRekId: "G01",
            PabrikId: "P01",
            SatuanKecilId: "TAB",
            SatuanBesarId: "TAB",
            Konversi: 1
        );
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BrgId.Should().Be("BRG0001");

        _brgRepoMock.Verify(x => x.SaveChanges(It.Is<BarangModel>(b =>
            b.BrgId == "BRG0001" &&
            b.BrgName == "Paracetamol 500mg" &&
            b.IsAktif == true &&
            b.KetBarang == "Obat Penurun Panas" &&
            b.GroupRek.GroupRekId == "G01" &&
            b.GroupRekDk.GroupRekDkId == "OBT" &&
            b.Pabrik.PabrikId == "P01" &&
            b.SatuanKecil.SatuanId == "TAB" &&
            b.SatuanBesar.SatuanId == "TAB" &&
            b.Konversi == 1
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValidTwoTierUom_SavesBarangWithTwoTierUom_AndReturnsResponse()
    {
        // Arrange
        var cmd = new BrgRegisterCommand(
            BrgId: "BRG0002",
            BrgName: "Amoxicillin 500mg",
            KetBarang: "Antibiotik",
            GroupRekId: "G01",
            PabrikId: "P01",
            SatuanKecilId: "TAB",
            SatuanBesarId: "STR",
            Konversi: 10
        );
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BrgId.Should().Be("BRG0002");

        _brgRepoMock.Verify(x => x.SaveChanges(It.Is<BarangModel>(b =>
            b.BrgId == "BRG0002" &&
            b.IsAktif == true &&
            b.SatuanKecil.SatuanId == "TAB" &&
            b.SatuanBesar.SatuanId == "STR" &&
            b.Konversi == 10
        )), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenBrgIdIsNullOrEmpty_ThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var cmd = new BrgRegisterCommand(
            invalidId, "Test Barang", "Ket", "G01", "P01", "TAB", "TAB", 1);
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenBrgIdExceeds13Chars_ThrowsArgumentException()
    {
        // Arrange (Max 13 chars constraint)
        var cmd = new BrgRegisterCommand(
            "12345678901234", "Test Barang", "Ket", "G01", "P01", "TAB", "TAB", 1);
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*13*");
    }

    [Fact]
    public async Task Handle_WhenBrgIdLengthIsExactly13Chars_Succeeds()
    {
        // Arrange
        var cmd = new BrgRegisterCommand(
            "1234567890123", "Test Barang", "Ket", "G01", "P01", "TAB", "TAB", 1);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.BrgId.Should().Be("1234567890123");
    }

    [Fact]
    public async Task Handle_WhenBrgAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = new Mock<IBrg>();
        existing.Setup(x => x.BrgId).Returns("BRG_EXIST");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG_EXIST")))
            .Returns(MayBe.From(existing.Object));

        var cmd = new BrgRegisterCommand(
            "BRG_EXIST", "Barang Duplikat", "Ket", "G01", "P01", "TAB", "TAB", 1);
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah terdaftar*");
    }

    [Fact]
    public async Task Handle_WhenGroupRekNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _groupRekRepoMock.Setup(x => x.LoadEntity(It.IsAny<IGroupRekKey>()))
            .Returns(MayBe<GroupRekType>.None);

        var cmd = new BrgRegisterCommand(
            "BRG0001", "Barang Test", "Ket", "G99", "P01", "TAB", "TAB", 1);
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*GroupRek*");
    }

    [Fact]
    public async Task Handle_WhenPabrikNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _pabrikRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPabrikKey>()))
            .Returns(MayBe<PabrikType>.None);

        var cmd = new BrgRegisterCommand(
            "BRG0001", "Barang Test", "Ket", "G01", "P99", "TAB", "TAB", 1);
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Pabrik*");
    }

    [Fact]
    public async Task Handle_WhenSatuanKecilNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "TAB")))
            .Returns(MayBe<SatuanType>.None);

        var cmd = new BrgRegisterCommand(
            "BRG0001", "Barang Test", "Ket", "G01", "P01", "TAB", "TAB", 1);
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SatuanKecil*");
    }

    [Fact]
    public async Task Handle_WhenSatuanBesarNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "BOX_NOT_FOUND")))
            .Returns(MayBe<SatuanType>.None);

        var cmd = new BrgRegisterCommand(
            "BRG0001", "Barang Test", "Ket", "G01", "P01", "TAB", "BOX_NOT_FOUND", 100);
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SatuanBesar*");
    }

    [Fact]
    public async Task Handle_WhenKonversiInvalidForTwoTier_ThrowsArgumentException()
    {
        // Arrange (different satuan but konversi <= 1)
        var cmd = new BrgRegisterCommand(
            "BRG0001", "Barang Test", "Ket", "G01", "P01", "TAB", "STR", 1);
        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*konversi harus > 1*");
    }
}
