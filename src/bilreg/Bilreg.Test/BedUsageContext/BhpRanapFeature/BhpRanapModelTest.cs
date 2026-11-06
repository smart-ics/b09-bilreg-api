using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.BhpRanapFeature;

public class BhpRanapModelTest
{
    [Fact]
    public void Create_ValidInput_ShouldCreateBhpRanapModel()
    {
        var audit = new AuditInfoType("USER1", new DateTime(2026, 2, 11, 10, 0, 0));
        var model = BhpRanapModel.Create(
            pakaiBedId: "BED001",
            regId: "REG001",
            layananId: "3RI02",
            bhpItemId: "BHP01",
            bhpItemName: "Infus Set Makro",
            qty: 2,
            price: 15000m,
            audit: audit);

        model.Should().NotBeNull();
        model.BhpRanapId.Should().StartWith("BHR");
        model.PakaiBedId.Should().Be("BED001");
        model.RegId.Should().Be("REG001");
        model.LayananId.Should().Be("3RI02");
        model.BhpItemId.Should().Be("BHP01");
        model.BhpItemName.Should().Be("Infus Set Makro");
        model.Qty.Should().Be(2);
        model.Price.Should().Be(15000m);
        model.Subtotal.Should().Be(30000m);
        model.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void Void_ValidInput_ShouldSetVoidAuditAndReason()
    {
        var audit = new AuditInfoType("USER1", new DateTime(2026, 2, 11, 10, 0, 0));
        var model = BhpRanapModel.Create(
            pakaiBedId: "BED001",
            regId: "REG001",
            layananId: "3RI02",
            bhpItemId: "BHP01",
            bhpItemName: "Infus Set Makro",
            qty: 1,
            price: 15000m,
            audit: audit);

        var voidTime = new DateTime(2026, 2, 11, 11, 0, 0);
        model.Void("USER2", voidTime, "Salah input kamar");

        model.IsVoid.Should().BeTrue();
        model.VoidAudit.UserId.Should().Be("USER2");
        model.VoidAudit.Timestamp.Should().Be(voidTime);
        model.VoidKet.Should().Be("Salah input kamar");
    }
}
