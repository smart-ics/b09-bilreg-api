using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using FluentAssertions;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryOrderItemModelTest
{
    private static readonly DateTime ExpiryDate = new(2028, 12, 31);

    [Fact]
    public void Constructor_MapsValuesAndInitializesReceiptState()
    {
        var sut = new DeliveryOrderItemModel(
            7, "BRG001", "LYN001", 12.5m, "BOX", 100, 10, 11, ExpiryDate, "BATCH-1");

        sut.ItemNo.Should().Be(7);
        sut.BrgId.Should().Be("BRG001");
        sut.LayananId.Should().Be("LYN001");
        sut.QtyOrder.Should().Be(12.5m);
        sut.QtyReceived.Should().Be(0);
        sut.QtyRemaining.Should().Be(12.5m);
        sut.SatuanId.Should().Be("BOX");
        sut.Harga.Should().Be(100);
        sut.Diskon.Should().Be(10);
        sut.Tax.Should().Be(11);
        sut.TglEd.Should().Be(ExpiryDate);
        sut.NoBatch.Should().Be("BATCH-1");
        sut.State.Should().Be(DeliveryOrderItemStateEnum.Open);
    }

    [Fact]
    public void Create_ValidValues_ReturnsOpenItem()
    {
        var sut = DeliveryOrderItemModel.Create(
            "BRG001", "LYN001", 10, "BOX", 100, 10, 11, ExpiryDate, "BATCH-1");

        sut.ItemNo.Should().Be(0);
        sut.BrgId.Should().Be("BRG001");
        sut.LayananId.Should().Be("LYN001");
        sut.QtyOrder.Should().Be(10);
        sut.QtyReceived.Should().Be(0);
        sut.SatuanId.Should().Be("BOX");
        sut.Harga.Should().Be(100);
        sut.Diskon.Should().Be(10);
        sut.Tax.Should().Be(11);
        sut.TglEd.Should().Be(ExpiryDate);
        sut.NoBatch.Should().Be("BATCH-1");
        sut.State.Should().Be(DeliveryOrderItemStateEnum.Open);
    }

    [Fact]
    public void Create_OmittedOptionalValues_UsesSentinels()
    {
        var sut = DeliveryOrderItemModel.Create("BRG001", "LYN001", 10, "BOX", 100, 0, 0);

        sut.TglEd.Should().Be(new DateTime(3000, 1, 1));
        sut.NoBatch.Should().BeEmpty();
    }

    [Fact]
    public void Create_NullSatuanId_UsesEmptyString()
    {
        var sut = DeliveryOrderItemModel.Create("BRG001", "LYN001", 10, null!, 100, 0, 0);

        sut.SatuanId.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidBrgId_Throws(string? brgId)
    {
        var act = () => DeliveryOrderItemModel.Create(brgId!, "LYN001", 10, "BOX", 100, 0, 0);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("brgId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidLayananId_Throws(string? layananId)
    {
        var act = () => DeliveryOrderItemModel.Create("BRG001", layananId!, 10, "BOX", 100, 0, 0);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("layananId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveQtyOrder_Throws(decimal qtyOrder)
    {
        var act = () => DeliveryOrderItemModel.Create("BRG001", "LYN001", qtyOrder, "BOX", 100, 0, 0);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("qtyOrder");
    }

    [Theory]
    [InlineData(-1, 0, 0, "harga")]
    [InlineData(0, -1, 0, "diskon")]
    [InlineData(0, 0, -1, "tax")]
    public void Create_NegativePriceComponent_Throws(
        decimal harga,
        decimal diskon,
        decimal tax,
        string parameterName)
    {
        var act = () => DeliveryOrderItemModel.Create(
            "BRG001", "LYN001", 10, "BOX", harga, diskon, tax);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(parameterName);
    }

    [Fact]
    public void Default_ReturnsDeterministicSentinels()
    {
        var sut = DeliveryOrderItemModel.Default;

        sut.ItemNo.Should().Be(0);
        sut.BrgId.Should().Be("-");
        sut.LayananId.Should().Be("-");
        sut.QtyOrder.Should().Be(0);
        sut.QtyRemaining.Should().Be(0);
        sut.SatuanId.Should().Be("-");
        sut.TglEd.Should().Be(new DateTime(3000, 1, 1));
        sut.NoBatch.Should().BeEmpty();
        sut.State.Should().Be(DeliveryOrderItemStateEnum.Open);
    }

    [Theory]
    [InlineData(HppMethodEnum.Hpp, 100)]
    [InlineData(HppMethodEnum.HppDiskon, 90)]
    [InlineData(HppMethodEnum.HppDiskonTax, 101)]
    public void ComputeHpp_SupportedMethod_ReturnsExpectedValue(HppMethodEnum method, decimal expected)
    {
        var sut = DeliveryOrderItemModel.Create("BRG001", "LYN001", 10, "BOX", 100, 10, 11);

        sut.ComputeHpp(method).Should().Be(expected);
    }

    [Fact]
    public void ComputeHpp_UnknownMethod_Throws()
    {
        var sut = DeliveryOrderItemModel.Create("BRG001", "LYN001", 10, "BOX", 100, 10, 11);

        var act = () => sut.ComputeHpp((HppMethodEnum)99);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("method");
    }
}
