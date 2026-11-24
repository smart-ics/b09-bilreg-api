using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

public class TipeHargaModelTest
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Act
        var sut = TipeHargaModel.Create(
            "UM",
            "Pasien Mandiri / Umum",
            25m,
            1500m,
            3000m,
            11m,
            5m,
            true);

        // Assert
        sut.TipeHargaId.Should().Be("UM");
        sut.TipeHargaName.Should().Be("Pasien Mandiri / Umum");
        sut.DefaultMarginPersen.Should().Be(25m);
        sut.BiayaPerObat_r.Should().Be(1500m);
        sut.BiayaPerResep_R.Should().Be(3000m);
        sut.TaxPersen.Should().Be(11m);
        sut.DiskonPersen.Should().Be(5m);
        sut.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldMapParametersDirectly()
    {
        // Act
        var sut = new TipeHargaModel("JM", "Jaminan", 10m, 100m, 200m, 0m, 0m, false);

        // Assert
        sut.TipeHargaId.Should().Be("JM");
        sut.TipeHargaName.Should().Be("Jaminan");
        sut.DefaultMarginPersen.Should().Be(10m);
        sut.BiayaPerObat_r.Should().Be(100m);
        sut.BiayaPerResep_R.Should().Be(200m);
        sut.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Create_WithNullOrWhiteSpaceId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TipeHargaModel.Create("  ", "Umum", 0m, 0m, 0m, 0m, 0m, true);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("tipeHargaId");
    }

    [Fact]
    public void Create_WithNullOrWhiteSpaceName_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TipeHargaModel.Create("UM", "  ", 0m, 0m, 0m, 0m, 0m, true);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("tipeHargaName");
    }

    [Fact]
    public void Default_ShouldReturnInactiveZeroedModel()
    {
        // Act
        var sut = TipeHargaModel.Default;

        // Assert
        sut.TipeHargaId.Should().Be("-");
        sut.TipeHargaName.Should().Be("-");
        sut.DefaultMarginPersen.Should().Be(0m);
        sut.BiayaPerObat_r.Should().Be(0m);
        sut.BiayaPerResep_R.Should().Be(0m);
        sut.TaxPersen.Should().Be(0m);
        sut.DiskonPersen.Should().Be(0m);
        sut.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Key_ShouldReturnDefaultWithGivenId()
    {
        // Act
        var sut = TipeHargaModel.Key("BPJ");

        // Assert
        sut.TipeHargaId.Should().Be("BPJ");
        sut.Should().BeOfType<TipeHargaModel>();
    }
}
