using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using FluentAssertions;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class SupplierReffTest
{
    [Fact]
    public void Constructor_MapsValues()
    {
        var sut = new SupplierReff("SUP001", "Supplier Satu");

        sut.SupplierId.Should().Be("SUP001");
        sut.SupplierName.Should().Be("Supplier Satu");
    }

    [Fact]
    public void Default_ReturnsDeterministicSentinel()
    {
        var first = SupplierReff.Default;
        var second = SupplierReff.Default;

        first.SupplierId.Should().Be("-");
        first.SupplierName.Should().Be("-");
        second.Should().Be(first);
    }
}
