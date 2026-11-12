using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using FluentAssertions;

namespace Bilreg.Test.BrgContext.BrgFeature;

public class BarangModelTest
{
    private static GroupRekReff CreateTestGroupRek() => new("GR01", "Grup Obat");
    private static PabrikReff CreateTestPabrik() => new("PB01", "Kimia Farma");
    private static SatuanType CreateSatuanTablet() => SatuanType.Create("TAB", "Tablet");
    private static SatuanType CreateSatuanStrip() => SatuanType.Create("STR", "Strip");
    private static SatuanType CreateSatuanBox() => SatuanType.Create("BOX", "Box");

    [Fact]
    public void Create_WithValidSingleTierUOM_Success()
    {
        // Arrange
        var satKecil = CreateSatuanTablet();
        var satBesar = CreateSatuanTablet();
        var groupRek = CreateTestGroupRek();
        var groupRekDk = GroupRekDkType.Obat;
        var pabrik = CreateTestPabrik();

        // Act
        var actual = BarangModel.Create(
            "BRG001",
            "Paracetamol 500mg",
            true,
            "Obat Penurun Panas",
            groupRek,
            groupRekDk,
            pabrik,
            satKecil,
            satBesar,
            1);

        // Assert
        actual.BrgId.Should().Be("BRG001");
        actual.BrgName.Should().Be("Paracetamol 500mg");
        actual.IsAktif.Should().BeTrue();
        actual.KetBarang.Should().Be("Obat Penurun Panas");
        actual.GroupRek.Should().Be(groupRek);
        actual.GroupRekDk.Should().Be(groupRekDk);
        actual.Pabrik.Should().Be(pabrik);
        actual.SatuanKecil.Should().Be(satKecil);
        actual.SatuanBesar.Should().Be(satBesar);
        actual.Konversi.Should().Be(1);

        actual.ListSatuan.Should().HaveCount(1);
        actual.ListSatuan.First().Satuan.Should().Be(satKecil);
        actual.ListSatuan.First().Konversi.Should().Be(1);
    }

    [Fact]
    public void Create_WithValidTwoTierUOM_Success()
    {
        // Arrange
        var satKecil = CreateSatuanTablet();
        var satBesar = CreateSatuanBox();
        var groupRek = CreateTestGroupRek();
        var groupRekDk = GroupRekDkType.Obat;
        var pabrik = CreateTestPabrik();

        // Act
        var actual = BarangModel.Create(
            "BRG002",
            "Amoxicillin 500mg",
            true,
            "Antibiotik",
            groupRek,
            groupRekDk,
            pabrik,
            satKecil,
            satBesar,
            100);

        // Assert
        actual.BrgId.Should().Be("BRG002");
        actual.Konversi.Should().Be(100);

        var listSatuan = actual.ListSatuan.ToList();
        listSatuan.Should().HaveCount(2);
        listSatuan[0].Satuan.Should().Be(satKecil);
        listSatuan[0].Konversi.Should().Be(1);
        listSatuan[1].Satuan.Should().Be(satBesar);
        listSatuan[1].Konversi.Should().Be(100);
    }

    [Theory]
    [InlineData("OBT", "Obat")]
    [InlineData("BHP", "Alkes dan Bhp")]
    [InlineData("ATK", "Alat Tulis Kantor")]
    [InlineData("GIZ", "Gizi")]
    public void Create_SupportsAllStandardClassifications(string groupRekDkId, string groupRekDkName)
    {
        // Arrange
        var groupRekDk = GroupRekDkType.Create(groupRekDkId, groupRekDkName);
        var sat = CreateSatuanTablet();

        // Act
        var actual = BarangModel.Create(
            "BRG003",
            "Item Komoditas",
            true,
            "Keterangan",
            CreateTestGroupRek(),
            groupRekDk,
            CreateTestPabrik(),
            sat,
            sat,
            1);

        // Assert
        actual.GroupRekDk.GroupRekDkId.Should().Be(groupRekDkId);
        actual.GroupRekDk.GroupRekDkName.Should().Be(groupRekDkName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WhenKonversiLessThanOne_ThrowsArgumentException(int invalidKonversi)
    {
        // Arrange
        var sat = CreateSatuanTablet();

        // Act
        var act = () => BarangModel.Create(
            "BRG001",
            "Test Barang",
            true,
            "Ket",
            CreateTestGroupRek(),
            GroupRekDkType.Obat,
            CreateTestPabrik(),
            sat,
            sat,
            invalidKonversi);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Konversi harus >= 1*");
    }

    [Fact]
    public void Create_WhenSatuanBesarSameAsSatuanKecilAndKonversiGreaterThanOne_ThrowsArgumentException()
    {
        // Arrange
        var satKecil = CreateSatuanTablet();
        var satBesar = CreateSatuanTablet();

        // Act
        var act = () => BarangModel.Create(
            "BRG001",
            "Test Barang",
            true,
            "Ket",
            CreateTestGroupRek(),
            GroupRekDkType.Obat,
            CreateTestPabrik(),
            satKecil,
            satBesar,
            10);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Jika SatuanBesar sama dengan SatuanKecil, konversi harus 1*");
    }

    [Fact]
    public void Create_WhenSatuanBesarDifferentFromSatuanKecilAndKonversiEqualsOne_ThrowsArgumentException()
    {
        // Arrange
        var satKecil = CreateSatuanTablet();
        var satBesar = CreateSatuanStrip();

        // Act
        var act = () => BarangModel.Create(
            "BRG001",
            "Test Barang",
            true,
            "Ket",
            CreateTestGroupRek(),
            GroupRekDkType.Obat,
            CreateTestPabrik(),
            satKecil,
            satBesar,
            1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Jika SatuanBesar berbeda dari SatuanKecil, konversi harus > 1*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenBrgIdIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidBrgId)
    {
        // Arrange
        var sat = CreateSatuanTablet();

        // Act
        var act = () => BarangModel.Create(
            invalidBrgId!,
            "Test Barang",
            true,
            "Ket",
            CreateTestGroupRek(),
            GroupRekDkType.Obat,
            CreateTestPabrik(),
            sat,
            sat,
            1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenBrgNameIsNullOrWhiteSpace_ThrowsArgumentException(string? invalidBrgName)
    {
        // Arrange
        var sat = CreateSatuanTablet();

        // Act
        var act = () => BarangModel.Create(
            "BRG001",
            invalidBrgName!,
            true,
            "Ket",
            CreateTestGroupRek(),
            GroupRekDkType.Obat,
            CreateTestPabrik(),
            sat,
            sat,
            1);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhenRequiredObjectsAreNull_ThrowsArgumentNullException()
    {
        var sat = CreateSatuanTablet();
        var grp = CreateTestGroupRek();
        var grpDk = GroupRekDkType.Obat;
        var pbrk = CreateTestPabrik();

        FluentActions.Invoking(() => BarangModel.Create("B1", "N1", true, "", null!, grpDk, pbrk, sat, sat, 1))
            .Should().Throw<ArgumentNullException>();

        FluentActions.Invoking(() => BarangModel.Create("B1", "N1", true, "", grp, null!, pbrk, sat, sat, 1))
            .Should().Throw<ArgumentNullException>();

        FluentActions.Invoking(() => BarangModel.Create("B1", "N1", true, "", grp, grpDk, null!, sat, sat, 1))
            .Should().Throw<ArgumentNullException>();

        FluentActions.Invoking(() => BarangModel.Create("B1", "N1", true, "", grp, grpDk, pbrk, null!, sat, 1))
            .Should().Throw<ArgumentNullException>();

        FluentActions.Invoking(() => BarangModel.Create("B1", "N1", true, "", grp, grpDk, pbrk, sat, null!, 1))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToReff_ReturnsCorrectBrgReff()
    {
        // Arrange
        var sat = CreateSatuanTablet();
        var model = BarangModel.Create(
            "BRG001",
            "Paracetamol 500mg",
            true,
            "",
            CreateTestGroupRek(),
            GroupRekDkType.Obat,
            CreateTestPabrik(),
            sat,
            sat,
            1);

        // Act
        var reff = model.ToReff();

        // Assert
        reff.BrgId.Should().Be("BRG001");
        reff.BrgName.Should().Be("Paracetamol 500mg");
    }

    [Fact]
    public void SetAktif_ReturnsUpdatedModel()
    {
        // Arrange
        var sat = CreateSatuanTablet();
        var model = BarangModel.Create(
            "BRG001",
            "Paracetamol 500mg",
            true,
            "",
            CreateTestGroupRek(),
            GroupRekDkType.Obat,
            CreateTestPabrik(),
            sat,
            sat,
            1);

        // Act
        var updated = model.SetAktif(false);

        // Assert
        updated.IsAktif.Should().BeFalse();
        model.IsAktif.Should().BeTrue();
    }

    [Fact]
    public void Key_ReturnsCorrectIBrgKey()
    {
        // Act
        var key = BarangModel.Key("BRG999");

        // Assert
        key.BrgId.Should().Be("BRG999");
    }

    [Fact]
    public void Default_ReturnsValidInstance()
    {
        // Act
        var def = BarangModel.Default;

        // Assert
        def.BrgId.Should().Be("-");
        def.BrgName.Should().Be("-");
        def.IsAktif.Should().BeFalse();
        def.Konversi.Should().Be(1);
        def.ListSatuan.Should().HaveCount(1);
    }
}
