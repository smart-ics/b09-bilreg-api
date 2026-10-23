using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OrderOpResourceModelTest
{
    [Fact]
    public void Create_ValidParameters_ShouldInstantiateModelCorrectly()
    {
        // Arrange
        var resourceId = "RES-001";
        var orderOpId = "ORD-001";
        var resourceType = OrderOpResourceTypeEnum.Darah;
        var itemName = "Packed Red Cells (PRC)";
        var jumlah = 2;
        var satuan = "Kantong";
        var keterangan = "Golongan darah O+";

        // Act
        var actual = OrderOpResourceModel.Create(
            resourceId, orderOpId, resourceType, itemName, jumlah, satuan, keterangan);

        // Assert
        actual.Should().NotBeNull();
        actual.OrderOpResourceId.Should().Be(resourceId);
        actual.OrderOpId.Should().Be(orderOpId);
        actual.ResourceType.Should().Be(resourceType);
        actual.ItemName.Should().Be(itemName);
        actual.Jumlah.Should().Be(jumlah);
        actual.Satuan.Should().Be(satuan);
        actual.Keterangan.Should().Be(keterangan);
    }

    [Fact]
    public void Create_EmptyResourceId_ShouldAutoGenerateUlid()
    {
        // Act
        var actual = OrderOpResourceModel.Create(
            "", "ORD-001", OrderOpResourceTypeEnum.Implan, "Plat Tulang 3.5mm", 1, "Pcs", "Titanium");

        // Assert
        actual.Should().NotBeNull();
        actual.OrderOpResourceId.Should().NotBeNullOrWhiteSpace();
        actual.OrderOpResourceId.Length.Should().Be(26);
    }

    [Fact]
    public void Create_NullKeterangan_ShouldDefaultToEmptyString()
    {
        // Act
        var actual = OrderOpResourceModel.Create(
            "RES-002", "ORD-001", OrderOpResourceTypeEnum.AlatKhusus, "C-Arm Fluoroscopy", 1, "Unit", null!);

        // Assert
        actual.Keterangan.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhiteSpaceItemName_ShouldThrowArgumentException(string? invalidItemName)
    {
        // Act
        var act = () => OrderOpResourceModel.Create(
            "RES-001", "ORD-001", OrderOpResourceTypeEnum.Darah, invalidItemName!, 1, "Kolf", "Catatan");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_InvalidJumlah_ShouldThrowArgumentException(int invalidJumlah)
    {
        // Act
        var act = () => OrderOpResourceModel.Create(
            "RES-001", "ORD-001", OrderOpResourceTypeEnum.Darah, "Darah Segar", invalidJumlah, "Kolf", "Catatan");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhiteSpaceSatuan_ShouldThrowArgumentException(string? invalidSatuan)
    {
        // Act
        var act = () => OrderOpResourceModel.Create(
            "RES-001", "ORD-001", OrderOpResourceTypeEnum.Implan, "Mesh Hernia", 1, invalidSatuan!, "Catatan");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldSetAllPropertiesDirectly()
    {
        // Arrange & Act
        var model = new OrderOpResourceModel(
            "ID1", "ORD1", OrderOpResourceTypeEnum.AlatKhusus, "Harmonic Scalpel", 1, "Set", "Bedah laparoskopi");

        // Assert
        model.OrderOpResourceId.Should().Be("ID1");
        model.OrderOpId.Should().Be("ORD1");
        model.ResourceType.Should().Be(OrderOpResourceTypeEnum.AlatKhusus);
        model.ItemName.Should().Be("Harmonic Scalpel");
        model.Jumlah.Should().Be(1);
        model.Satuan.Should().Be("Set");
        model.Keterangan.Should().Be("Bedah laparoskopi");
    }

    [Fact]
    public void AnestesiPlanEnum_ShouldHaveCorrectCanonicalValues()
    {
        ((int)AnestesiPlanEnum.General).Should().Be(1);
        ((int)AnestesiPlanEnum.RegionalSpinal).Should().Be(2);
        ((int)AnestesiPlanEnum.Lokal).Should().Be(3);
        ((int)AnestesiPlanEnum.Sedasi).Should().Be(4);
        ((int)AnestesiPlanEnum.TanpaAnestesi).Should().Be(5);
    }

    [Fact]
    public void PostOpCareUnitEnum_ShouldHaveCorrectCanonicalValues()
    {
        ((int)PostOpCareUnitEnum.Ward).Should().Be(1);
        ((int)PostOpCareUnitEnum.PACU).Should().Be(2);
        ((int)PostOpCareUnitEnum.ICU).Should().Be(3);
        ((int)PostOpCareUnitEnum.PICU).Should().Be(4);
        ((int)PostOpCareUnitEnum.NICU).Should().Be(5);
    }

    [Fact]
    public void KlasifikasiOperasiEnum_ShouldHaveCorrectCanonicalValues()
    {
        ((int)KlasifikasiOperasiEnum.Kecil).Should().Be(1);
        ((int)KlasifikasiOperasiEnum.Sedang).Should().Be(2);
        ((int)KlasifikasiOperasiEnum.Besar).Should().Be(3);
        ((int)KlasifikasiOperasiEnum.Khusus).Should().Be(4);
    }

    [Fact]
    public void OrderOpResourceTypeEnum_ShouldHaveCorrectCanonicalValues()
    {
        ((int)OrderOpResourceTypeEnum.Darah).Should().Be(1);
        ((int)OrderOpResourceTypeEnum.Implan).Should().Be(2);
        ((int)OrderOpResourceTypeEnum.AlatKhusus).Should().Be(3);
    }

    [Fact]
    public void UrgencyLevelEnum_ShouldHaveCorrectCanonicalBinaryValues()
    {
        ((int)UrgencyLevelEnum.Elective).Should().Be(1);
        ((int)UrgencyLevelEnum.Cito).Should().Be(2);
    }
}
