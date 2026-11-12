using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

public class RoundingPolicyModelTest
{
    [Fact]
    public void Default_ShouldBeIncrement100AndKeAtas()
    {
        // Act
        var sut = RoundingPolicyModel.Default;

        // Assert
        sut.NilaiPembulatan.Should().Be(100);
        sut.JenisPembulatan.Should().Be(RoundingModeEnum.KeAtas);
    }

    [Fact]
    public void Constructor_WithZeroIncrement_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new RoundingPolicyModel(0, RoundingModeEnum.Normal);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("nilaiPembulatan");
    }

    [Fact]
    public void Constructor_WithNegativeIncrement_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new RoundingPolicyModel(-100, RoundingModeEnum.Normal);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(1200, 1200)]
    [InlineData(1234, 1300)]
    [InlineData(1200.01, 1300)]
    [InlineData(1299.99, 1300)]
    [InlineData(1300, 1300)]
    public void HitungPembulatan_KeAtas_Increment100_ShouldRoundUpAnyFraction(decimal totalBefore, decimal expectedTotal)
    {
        // Arrange
        var sut = new RoundingPolicyModel(100, RoundingModeEnum.KeAtas);

        // Act
        var result = sut.HitungPembulatan(totalBefore);

        // Assert
        result.TotalSebelum.Should().Be(totalBefore);
        result.TotalAkhir.Should().Be(expectedTotal);
        result.SelisihPembulatan.Should().Be(expectedTotal - totalBefore);
    }

    [Theory]
    [InlineData(1234, 1200)]     // pecahan 34 < 50 -> bawah
    [InlineData(1249.99, 1200)]  // pecahan 49.99 < 50 -> bawah
    [InlineData(1250, 1300)]     // pecahan 50 >= 50 -> atas
    [InlineData(1250.01, 1300)]
    [InlineData(1299.99, 1300)]
    [InlineData(1200, 1200)]     // tepat kelipatan -> tetap
    public void HitungPembulatan_Normal_Increment100_ShouldRoundHalfUp(decimal totalBefore, decimal expectedTotal)
    {
        // Arrange
        var sut = new RoundingPolicyModel(100, RoundingModeEnum.Normal);

        // Act
        var result = sut.HitungPembulatan(totalBefore);

        // Assert
        result.TotalAkhir.Should().Be(expectedTotal);
        result.SelisihPembulatan.Should().Be(expectedTotal - totalBefore);
    }

    [Theory]
    [InlineData(1001, 1500)]
    [InlineData(1500, 1500)]
    [InlineData(1500.01, 2000)]
    public void HitungPembulatan_KeAtas_Increment500_ShouldUseIncrement(decimal totalBefore, decimal expectedTotal)
    {
        // Arrange
        var sut = new RoundingPolicyModel(500, RoundingModeEnum.KeAtas);

        // Act
        var result = sut.HitungPembulatan(totalBefore);

        // Assert
        result.TotalAkhir.Should().Be(expectedTotal);
    }

    [Theory]
    [InlineData(1249, 1000)]
    [InlineData(1250, 1500)]
    [InlineData(1749, 1500)]
    [InlineData(1750, 2000)]
    public void HitungPembulatan_Normal_Increment500_ShouldUseIncrement(decimal totalBefore, decimal expectedTotal)
    {
        // Arrange
        var sut = new RoundingPolicyModel(500, RoundingModeEnum.Normal);

        // Act
        var result = sut.HitungPembulatan(totalBefore);

        // Assert
        result.TotalAkhir.Should().Be(expectedTotal);
    }

    [Theory]
    [InlineData(1499, 1000)]
    [InlineData(1500, 2000)]
    [InlineData(1999, 2000)]
    public void HitungPembulatan_Normal_Increment1000_ShouldUseIncrement(decimal totalBefore, decimal expectedTotal)
    {
        // Arrange
        var sut = new RoundingPolicyModel(1000, RoundingModeEnum.Normal);

        // Act
        var result = sut.HitungPembulatan(totalBefore);

        // Assert
        result.TotalAkhir.Should().Be(expectedTotal);
    }

    [Theory]
    [InlineData(1000, 1000)]
    [InlineData(1000.01, 2000)]
    [InlineData(1234, 2000)]
    public void HitungPembulatan_KeAtas_Increment1000_ShouldUseIncrement(decimal totalBefore, decimal expectedTotal)
    {
        // Arrange
        var sut = new RoundingPolicyModel(1000, RoundingModeEnum.KeAtas);

        // Act
        var result = sut.HitungPembulatan(totalBefore);

        // Assert
        result.TotalAkhir.Should().Be(expectedTotal);
    }

    [Fact]
    public void HitungPembulatan_ShouldComputeSelisihPembulatanExactly()
    {
        // Arrange
        var keAtas = new RoundingPolicyModel(100, RoundingModeEnum.KeAtas);
        var normal = new RoundingPolicyModel(100, RoundingModeEnum.Normal);

        // Act
        var hasilKeAtas = keAtas.HitungPembulatan(1234m);
        var hasilNormal = normal.HitungPembulatan(1234m);

        // Assert
        hasilKeAtas.TotalAkhir.Should().Be(1300m);
        hasilKeAtas.SelisihPembulatan.Should().Be(66m);

        hasilNormal.TotalAkhir.Should().Be(1200m);
        hasilNormal.SelisihPembulatan.Should().Be(-34m);
    }
}
