using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Services;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.InventoryContext.OpnameFeature;

public class OpnameItemInfoProviderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetHpp_WhenBrgIdIsNullOrWhitespace_ReturnsZero(string? brgId)
    {
        // Arrange
        var provider = new OpnameItemInfoProvider(
            (b, _) => 5000m,
            (b, _) => "TAB");

        // Act
        var result = provider.GetHpp(brgId!);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public void GetHpp_WhenBrgIdIsValid_ReturnsExecutedHpp()
    {
        // Arrange
        const string brgId = "BRG-001";
        const decimal expectedHpp = 12500.50m;
        string? capturedBrgId = null;

        var provider = new OpnameItemInfoProvider(
            (b, _) =>
            {
                capturedBrgId = b;
                return expectedHpp;
            },
            (b, _) => "TAB");

        // Act
        var result = provider.GetHpp(brgId);

        // Assert
        result.Should().Be(expectedHpp);
        capturedBrgId.Should().Be(brgId);
    }

    [Fact]
    public void GetHpp_WhenExecutorReturnsNull_ReturnsZero()
    {
        // Arrange
        var provider = new OpnameItemInfoProvider(
            (b, _) => null,
            (b, _) => "TAB");

        // Act
        var result = provider.GetHpp("BRG-UNKNOWN");

        // Assert
        result.Should().Be(0m);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetBaseSatuan_WhenBrgIdIsNullOrWhitespace_ReturnsEmptyString(string? brgId)
    {
        // Arrange
        var provider = new OpnameItemInfoProvider(
            (b, _) => 5000m,
            (b, _) => "TAB");

        // Act
        var result = provider.GetBaseSatuan(brgId!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetBaseSatuan_WhenBrgIdIsValid_ReturnsExecutedSatuan()
    {
        // Arrange
        const string brgId = "BRG-002";
        const string expectedSatuan = "BOT";
        string? capturedBrgId = null;

        var provider = new OpnameItemInfoProvider(
            (b, _) => 1000m,
            (b, _) =>
            {
                capturedBrgId = b;
                return expectedSatuan;
            });

        // Act
        var result = provider.GetBaseSatuan(brgId);

        // Assert
        result.Should().Be(expectedSatuan);
        capturedBrgId.Should().Be(brgId);
    }

    [Fact]
    public void GetBaseSatuan_WhenExecutorReturnsNull_ReturnsEmptyString()
    {
        // Arrange
        var provider = new OpnameItemInfoProvider(
            (b, _) => 1000m,
            (b, _) => null);

        // Act
        var result = provider.GetBaseSatuan("BRG-UNKNOWN");

        // Assert
        result.Should().BeEmpty();
    }
}
