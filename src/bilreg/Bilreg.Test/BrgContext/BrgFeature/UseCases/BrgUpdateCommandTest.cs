using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.BrgFeature.UseCases;
using Bilreg.Application.BrgContext.KlasifikasiFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.BrgContext.BrgFeature.UseCases;

public class BrgUpdateCommandTest
{
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<IBrgStockMovementCheck> _stockMovementCheckMock = new();
    private readonly Mock<IGroupRekRepo> _groupRekRepoMock = new();
    private readonly Mock<IPabrikRepo> _pabrikRepoMock = new();
    private readonly Mock<ISatuanRepo> _satuanRepoMock = new();

    private BrgUpdateHandler CreateSut() =>
        new(
            _brgRepoMock.Object,
            _stockMovementCheckMock.Object,
            _groupRekRepoMock.Object,
            _pabrikRepoMock.Object,
            _satuanRepoMock.Object
        );

    private static BarangModel CreateSampleBarang(
        string id = "BRG001",
        string name = "Paracetamol 500mg",
        bool isAktif = true,
        string ket = "Tablet antipiretik",
        string groupRekId = "GR01",
        string pabrikId = "PAB01",
        string satKecilId = "SAT_TAB",
        string satBesarId = "SAT_BOX",
        int konversi = 100)
    {
        var groupRek = new GroupRekType(groupRekId, "Obat Oral", new GroupRekDkType("OBT", "Obat"));
        var pabrik = new PabrikType(pabrikId, "Kimia Farma");
        var satKecil = new SatuanType(satKecilId, "Tablet");
        var satBesar = satKecilId == satBesarId ? satKecil : new SatuanType(satBesarId, "Box");

        return BarangModel.Create(
            id,
            name,
            isAktif,
            ket,
            groupRek.ToReff(),
            groupRek.GroupRekDk,
            pabrik.ToReff(),
            satKecil,
            satBesar,
            konversi);
    }

    [Theory]
    [InlineData("", "Barang Name", "GR01", "PAB01", "S01", "S01", 1)]
    [InlineData("   ", "Barang Name", "GR01", "PAB01", "S01", "S01", 1)]
    [InlineData("B01", "", "GR01", "PAB01", "S01", "S01", 1)]
    [InlineData("B01", "   ", "GR01", "PAB01", "S01", "S01", 1)]
    [InlineData("B01", "Barang Name", "", "PAB01", "S01", "S01", 1)]
    [InlineData("B01", "Barang Name", "GR01", "", "S01", "S01", 1)]
    [InlineData("B01", "Barang Name", "GR01", "PAB01", "", "S01", 1)]
    [InlineData("B01", "Barang Name", "GR01", "PAB01", "S01", "", 1)]
    [InlineData("B01", "Barang Name", "GR01", "PAB01", "S01", "S01", 0)]
    [InlineData("B01", "Barang Name", "GR01", "PAB01", "S01", "S01", -1)]
    public async Task Handle_WhenGuardClausesTriggered_ThrowsArgumentException(
        string id, string name, string grId, string pabId, string sKecil, string sBesar, int konv)
    {
        // Arrange
        var sut = CreateSut();
        var command = new BrgUpdateCommand(id, name, "Ket", grId, pabId, sKecil, sBesar, konv);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenBarangNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe<IBrg>.None);

        var sut = CreateSut();
        var command = new BrgUpdateCommand("BRG999", "Nama", "Ket", "GR01", "PAB01", "S01", "S01", 1);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*BRG999*not found*");
    }

    [Fact]
    public async Task Handle_WhenSatuanKecilChangedAndHasStockMovement_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = CreateSampleBarang(id: "BRG001", satKecilId: "SAT_TAB", satBesarId: "SAT_BOX", konversi: 100);
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG001")))
            .Returns(MayBe.From<IBrg>(existing));

        _stockMovementCheckMock.Setup(x => x.HasStockMovementOrBalance("BRG001"))
            .Returns(true);

        var sut = CreateSut();
        var command = new BrgUpdateCommand(
            "BRG001", "Paracetamol", "Ket", "GR01", "PAB01", "SAT_KAP", "SAT_BOX", 100);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak dapat diubah karena barang sudah memiliki riwayat mutasi atau saldo stok*");
        _stockMovementCheckMock.Verify(x => x.HasStockMovementOrBalance("BRG001"), Times.Once);
        _brgRepoMock.Verify(x => x.SaveChanges(It.IsAny<IBrg>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenKonversiChangedAndHasStockMovement_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = CreateSampleBarang(id: "BRG001", satKecilId: "SAT_TAB", satBesarId: "SAT_BOX", konversi: 100);
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG001")))
            .Returns(MayBe.From<IBrg>(existing));

        _stockMovementCheckMock.Setup(x => x.HasStockMovementOrBalance("BRG001"))
            .Returns(true);

        var sut = CreateSut();
        var command = new BrgUpdateCommand(
            "BRG001", "Paracetamol", "Ket", "GR01", "PAB01", "SAT_TAB", "SAT_BOX", 50);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak dapat diubah karena barang sudah memiliki riwayat mutasi atau saldo stok*");
        _stockMovementCheckMock.Verify(x => x.HasStockMovementOrBalance("BRG001"), Times.Once);
        _brgRepoMock.Verify(x => x.SaveChanges(It.IsAny<IBrg>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBothSatuanKecilAndKonversiChangedAndHasStockMovement_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = CreateSampleBarang(id: "BRG001", satKecilId: "SAT_TAB", satBesarId: "SAT_BOX", konversi: 100);
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG001")))
            .Returns(MayBe.From<IBrg>(existing));

        _stockMovementCheckMock.Setup(x => x.HasStockMovementOrBalance("BRG001"))
            .Returns(true);

        var sut = CreateSut();
        var command = new BrgUpdateCommand(
            "BRG001", "Paracetamol", "Ket", "GR01", "PAB01", "SAT_KAP", "SAT_BOX", 50);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak dapat diubah karena barang sudah memiliki riwayat mutasi atau saldo stok*");
        _brgRepoMock.Verify(x => x.SaveChanges(It.IsAny<IBrg>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenGroupRekNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var existing = CreateSampleBarang(id: "BRG001", satKecilId: "SAT_TAB", satBesarId: "SAT_TAB", konversi: 1);
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe.From<IBrg>(existing));

        _groupRekRepoMock.Setup(x => x.LoadEntity(It.IsAny<IGroupRekKey>()))
            .Returns(MayBe<GroupRekType>.None);

        var sut = CreateSut();
        var command = new BrgUpdateCommand("BRG001", "Nama", "Ket", "GR_INVALID", "PAB01", "SAT_TAB", "SAT_TAB", 1);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*GroupRek 'GR_INVALID' not found*");
    }

    [Fact]
    public async Task Handle_WhenPabrikNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var existing = CreateSampleBarang(id: "BRG001", satKecilId: "SAT_TAB", satBesarId: "SAT_TAB", konversi: 1);
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe.From<IBrg>(existing));

        var groupRek = new GroupRekType("GR01", "Obat Oral", new GroupRekDkType("OBT", "Obat"));
        _groupRekRepoMock.Setup(x => x.LoadEntity(It.IsAny<IGroupRekKey>()))
            .Returns(MayBe.From(groupRek));

        _pabrikRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPabrikKey>()))
            .Returns(MayBe<PabrikType>.None);

        var sut = CreateSut();
        var command = new BrgUpdateCommand("BRG001", "Nama", "Ket", "GR01", "PAB_INVALID", "SAT_TAB", "SAT_TAB", 1);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Pabrik 'PAB_INVALID' not found*");
    }

    [Fact]
    public async Task Handle_WhenSatuanKecilNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var existing = CreateSampleBarang(id: "BRG001", satKecilId: "SAT_TAB", satBesarId: "SAT_TAB", konversi: 1);
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe.From<IBrg>(existing));

        var groupRek = new GroupRekType("GR01", "Obat Oral", new GroupRekDkType("OBT", "Obat"));
        _groupRekRepoMock.Setup(x => x.LoadEntity(It.IsAny<IGroupRekKey>()))
            .Returns(MayBe.From(groupRek));

        var pabrik = new PabrikType("PAB01", "Kimia Farma");
        _pabrikRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPabrikKey>()))
            .Returns(MayBe.From(pabrik));

        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "SAT_INVALID")))
            .Returns(MayBe<SatuanType>.None);

        var sut = CreateSut();
        var command = new BrgUpdateCommand("BRG001", "Nama", "Ket", "GR01", "PAB01", "SAT_INVALID", "SAT_INVALID", 1);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SatuanKecil 'SAT_INVALID' not found*");
    }

    [Fact]
    public async Task Handle_WhenSatuanBesarNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var existing = CreateSampleBarang(id: "BRG001", satKecilId: "SAT_TAB", satBesarId: "SAT_TAB", konversi: 1);
        _brgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns(MayBe.From<IBrg>(existing));

        var groupRek = new GroupRekType("GR01", "Obat Oral", new GroupRekDkType("OBT", "Obat"));
        _groupRekRepoMock.Setup(x => x.LoadEntity(It.IsAny<IGroupRekKey>()))
            .Returns(MayBe.From(groupRek));

        var pabrik = new PabrikType("PAB01", "Kimia Farma");
        _pabrikRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPabrikKey>()))
            .Returns(MayBe.From(pabrik));

        var satKecil = new SatuanType("SAT_TAB", "Tablet");
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "SAT_TAB")))
            .Returns(MayBe.From(satKecil));

        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "SAT_BESAR_INVALID")))
            .Returns(MayBe<SatuanType>.None);

        var sut = CreateSut();
        var command = new BrgUpdateCommand("BRG001", "Nama", "Ket", "GR01", "PAB01", "SAT_TAB", "SAT_BESAR_INVALID", 10);

        // Act
        var act = () => sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SatuanBesar 'SAT_BESAR_INVALID' not found*");
    }

    [Fact]
    public async Task Handle_WhenSatuanKecilAndKonversiChangedAndNoStockMovement_UpdatesSuccessfully()
    {
        // Arrange
        var existing = CreateSampleBarang(id: "BRG001", satKecilId: "SAT_TAB", satBesarId: "SAT_BOX", konversi: 100);
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG001")))
            .Returns(MayBe.From<IBrg>(existing));

        _stockMovementCheckMock.Setup(x => x.HasStockMovementOrBalance("BRG001"))
            .Returns(false);

        var groupRek = new GroupRekType("GR01", "Obat Oral", new GroupRekDkType("OBT", "Obat"));
        _groupRekRepoMock.Setup(x => x.LoadEntity(It.IsAny<IGroupRekKey>()))
            .Returns(MayBe.From(groupRek));

        var pabrik = new PabrikType("PAB01", "Kimia Farma");
        _pabrikRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPabrikKey>()))
            .Returns(MayBe.From(pabrik));

        var satKecilBaru = new SatuanType("SAT_KAP", "Kapsul");
        var satBesarBaru = new SatuanType("SAT_BTL", "Botol");
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "SAT_KAP")))
            .Returns(MayBe.From(satKecilBaru));
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "SAT_BTL")))
            .Returns(MayBe.From(satBesarBaru));

        BarangModel? savedBarang = null;
        _brgRepoMock.Setup(x => x.SaveChanges(It.IsAny<IBrg>()))
            .Callback<IBrg>(b => savedBarang = b as BarangModel);

        var sut = CreateSut();
        var command = new BrgUpdateCommand(
            "BRG001", "Paracetamol Kapsul", "Update UOM", "GR01", "PAB01", "SAT_KAP", "SAT_BTL", 50);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        savedBarang.Should().NotBeNull();
        savedBarang!.BrgId.Should().Be("BRG001");
        savedBarang.BrgName.Should().Be("Paracetamol Kapsul");
        savedBarang.SatuanKecil.SatuanId.Should().Be("SAT_KAP");
        savedBarang.SatuanBesar.SatuanId.Should().Be("SAT_BTL");
        savedBarang.Konversi.Should().Be(50);
        _brgRepoMock.Verify(x => x.SaveChanges(It.IsAny<IBrg>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOnlyDescriptiveAttributesChangedAndHasStockMovement_UpdatesSuccessfully()
    {
        // Arrange
        var existing = CreateSampleBarang(
            id: "BRG001",
            name: "Nama Lama",
            ket: "Ket Lama",
            groupRekId: "GR01",
            pabrikId: "PAB01",
            satKecilId: "SAT_TAB",
            satBesarId: "SAT_BOX",
            konversi: 100);

        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG001")))
            .Returns(MayBe.From<IBrg>(existing));

        // Note: Even if stock check returns true, handler should not throw because UOM didn't change!
        _stockMovementCheckMock.Setup(x => x.HasStockMovementOrBalance("BRG001"))
            .Returns(true);

        var newGroupRek = new GroupRekType("GR02", "Obat Topikal", new GroupRekDkType("OBT", "Obat"));
        _groupRekRepoMock.Setup(x => x.LoadEntity(It.Is<IGroupRekKey>(k => k.GroupRekId == "GR02")))
            .Returns(MayBe.From(newGroupRek));

        var newPabrik = new PabrikType("PAB02", "Kalbe Farma");
        _pabrikRepoMock.Setup(x => x.LoadEntity(It.Is<IPabrikKey>(k => k.PabrikId == "PAB02")))
            .Returns(MayBe.From(newPabrik));

        var satKecil = new SatuanType("SAT_TAB", "Tablet");
        var satBesar = new SatuanType("SAT_BOX", "Box");
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "SAT_TAB")))
            .Returns(MayBe.From(satKecil));
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "SAT_BOX")))
            .Returns(MayBe.From(satBesar));

        BarangModel? savedBarang = null;
        _brgRepoMock.Setup(x => x.SaveChanges(It.IsAny<IBrg>()))
            .Callback<IBrg>(b => savedBarang = b as BarangModel);

        var sut = CreateSut();
        var command = new BrgUpdateCommand(
            "BRG001", "Nama Baru", "Ket Baru", "GR02", "PAB02", "SAT_TAB", "SAT_BOX", 100);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        savedBarang.Should().NotBeNull();
        savedBarang!.BrgId.Should().Be("BRG001");
        savedBarang.BrgName.Should().Be("Nama Baru");
        savedBarang.KetBarang.Should().Be("Ket Baru");
        savedBarang.GroupRek.GroupRekId.Should().Be("GR02");
        savedBarang.GroupRek.GroupRekName.Should().Be("Obat Topikal");
        savedBarang.Pabrik.PabrikId.Should().Be("PAB02");
        savedBarang.Pabrik.PabrikName.Should().Be("Kalbe Farma");
        savedBarang.SatuanKecil.SatuanId.Should().Be("SAT_TAB");
        savedBarang.SatuanBesar.SatuanId.Should().Be("SAT_BOX");
        savedBarang.Konversi.Should().Be(100);
        savedBarang.IsAktif.Should().BeTrue();

        _stockMovementCheckMock.Verify(x => x.HasStockMovementOrBalance(It.IsAny<string>()), Times.Never);
        _brgRepoMock.Verify(x => x.SaveChanges(It.IsAny<IBrg>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSingleUomUpdated_PreservesIsAktifAndSavesCorrectly()
    {
        // Arrange
        var existing = CreateSampleBarang(
            id: "BRG002",
            isAktif: false,
            satKecilId: "SAT_PCS",
            satBesarId: "SAT_PCS",
            konversi: 1);

        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG002")))
            .Returns(MayBe.From<IBrg>(existing));

        var groupRek = new GroupRekType("GR_ATK", "Alat Tulis", new GroupRekDkType("ATK", "ATK"));
        _groupRekRepoMock.Setup(x => x.LoadEntity(It.IsAny<IGroupRekKey>()))
            .Returns(MayBe.From(groupRek));

        var pabrik = new PabrikType("PAB_SN", "Snowman");
        _pabrikRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPabrikKey>()))
            .Returns(MayBe.From(pabrik));

        var satPcs = new SatuanType("SAT_PCS", "Pieces");
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "SAT_PCS")))
            .Returns(MayBe.From(satPcs));

        BarangModel? savedBarang = null;
        _brgRepoMock.Setup(x => x.SaveChanges(It.IsAny<IBrg>()))
            .Callback<IBrg>(b => savedBarang = b as BarangModel);

        var sut = CreateSut();
        var command = new BrgUpdateCommand(
            "BRG002", "Spidol Whiteboard Hitam", "Alat Tulis Kantor", "GR_ATK", "PAB_SN", "SAT_PCS", "SAT_PCS", 1);

        // Act
        await sut.Handle(command, CancellationToken.None);

        // Assert
        savedBarang.Should().NotBeNull();
        savedBarang!.BrgId.Should().Be("BRG002");
        savedBarang.IsAktif.Should().BeFalse(); // Preserved existing inactive status
        savedBarang.GroupRekDk.GroupRekDkId.Should().Be("ATK");
        savedBarang.ListSatuan.Should().HaveCount(1);
        _brgRepoMock.Verify(x => x.SaveChanges(It.IsAny<IBrg>()), Times.Once);
    }
}
