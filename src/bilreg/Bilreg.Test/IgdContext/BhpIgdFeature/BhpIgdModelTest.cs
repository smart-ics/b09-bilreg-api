using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.IgdContext.BhpIgdFeature;

public class BhpIgdModelTest
{
    private static AuditInfoType TestAudit() => new("U1", new DateTime(2026, 1, 1, 8, 0, 0));

    private static VisitorType TestVisitor() => new(
        VisitorName: "Pasien Tes",
        Gender: "L",
        TglLahir: new DateOnly(1990, 1, 1),
        Kontak: "0812000");

    private static RegModel CreateSampleReg(string regId = "REG001")
        => (RegModel)RegModel.Key(regId)!;

    [Fact]
    public void Create_WhenVisitHasNoReg_SucceedsWithUnregisteredRegIdAndUnpostedStatus()
    {
        // Clinical Flow First: Pra-registrasi diperbolehkan
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        visit.HasReg.Should().BeFalse();

        var bhp = BhpIgdModel.Create(
            visit,
            bhpItemId: "BHP001",
            bhpItemName: "Kassa Steril",
            qty: 2,
            price: 5000m,
            audit: TestAudit());

        bhp.BhpIgdId.Should().StartWith("BHI");
        bhp.IgdVisitId.Should().Be(visit.IgdVisitId);
        bhp.RegId.Should().Be("-");
        bhp.IsPosted.Should().BeFalse();
        bhp.TrsBillId.Should().BeEmpty();
        bhp.PostedDate.Should().BeNull();
        bhp.Subtotal.Should().Be(10000m);
    }

    [Fact]
    public void Create_WhenVisitHasReg_SetsRegIdFromVisit()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        visit.AssignRegister(CreateSampleReg("REG-IGD-001"), TestAudit());
        visit.HasReg.Should().BeTrue();

        var bhp = BhpIgdModel.Create(
            visit,
            bhpItemId: "BHP002",
            bhpItemName: "Spuit 5cc",
            qty: 3,
            price: 4000m,
            audit: TestAudit());

        bhp.RegId.Should().Be("REG-IGD-001");
        bhp.IsPosted.Should().BeFalse();
        bhp.Subtotal.Should().Be(12000m);
    }

    [Fact]
    public void AssignRegister_GivenValidRegId_UpdatesRegId()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var bhp = BhpIgdModel.Create(
            visit,
            bhpItemId: "BHP001",
            bhpItemName: "Kassa",
            qty: 1,
            price: 5000m,
            audit: TestAudit());

        bhp.AssignRegister("REG-POSTED-99");
        bhp.RegId.Should().Be("REG-POSTED-99");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AssignRegister_GivenEmptyOrNull_ThrowsArgumentException(string? invalidRegId)
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var bhp = BhpIgdModel.Create(
            visit,
            bhpItemId: "BHP001",
            bhpItemName: "Kassa",
            qty: 1,
            price: 5000m,
            audit: TestAudit());

        var act = () => bhp.AssignRegister(invalidRegId!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkAsPosted_GivenTrsBillIdAndDate_UpdatesPostingStatus()
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var bhp = BhpIgdModel.Create(
            visit,
            bhpItemId: "BHP001",
            bhpItemName: "Kassa",
            qty: 1,
            price: 5000m,
            audit: TestAudit());

        var postedAt = new DateTime(2026, 1, 1, 9, 30, 0);
        bhp.MarkAsPosted("TBILL999", postedAt);

        bhp.IsPosted.Should().BeTrue();
        bhp.TrsBillId.Should().Be("TBILL999");
        bhp.PostedDate.Should().Be(postedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void MarkAsPosted_GivenInvalidBillId_ThrowsArgumentException(string? trsBillId)
    {
        var visit = IgdVisitModel.Create(TestVisitor(), TestAudit());
        var bhp = BhpIgdModel.Create(
            visit,
            bhpItemId: "BHP001",
            bhpItemName: "Kassa",
            qty: 1,
            price: 5000m,
            audit: TestAudit());

        var act = () => bhp.MarkAsPosted(trsBillId!, DateTime.Now);
        act.Should().Throw<ArgumentException>();
    }
}
