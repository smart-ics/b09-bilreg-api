using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.IgdContext.BedIgdFeature;

public class PakaiBedIgdModelTest
{
    private static AuditInfoType TestAudit(int hour = 8) =>
        new("U1", new DateTime(2026, 1, 1, hour, 0, 0));

    private static (IgdVisitModel visit, BedIgdModel bed) Pair()
    {
        var visit = IgdVisitModel.Create(
            new VisitorType("Tes", "L", new DateOnly(1990, 1, 1), "081"), TestAudit());
        var bed = BedIgdModel.CreateMaster("B01", "Bed 1", "K1", TestAudit());
        return (visit, bed);
    }

    [Fact]
    public void Open_ProducesOpenRowWithSentinelCheckOut()
    {
        var (visit, bed) = Pair();
        var pakai = PakaiBedIgdModel.Open(visit, bed, TestAudit());

        pakai.PakaiBedIgdId.Should().StartWith("PBI");
        pakai.IgdVisitId.Should().Be(visit.IgdVisitId);
        pakai.BedIgdId.Should().Be(bed.BedIgdId);
        pakai.IsOpen.Should().BeTrue();
        pakai.CheckOutDateTime.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void Close_FromOpen_SetsCheckOut()
    {
        var (visit, bed) = Pair();
        var pakai = PakaiBedIgdModel.Open(visit, bed, TestAudit(8));

        pakai.Close(TestAudit(10));

        pakai.IsOpen.Should().BeFalse();
        pakai.CheckOutDateTime.Should().Be(new DateTime(2026, 1, 1, 10, 0, 0));
        pakai.CheckOutUserId.Should().Be("U1");
    }

    [Fact]
    public void Close_AlreadyClosed_Throws()
    {
        var (visit, bed) = Pair();
        var pakai = PakaiBedIgdModel.Open(visit, bed, TestAudit(8));
        pakai.Close(TestAudit(10));

        var act = () => pakai.Close(TestAudit(11));

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah ditutup*");
    }

    [Fact]
    public void Close_BeforeCheckIn_Throws()
    {
        var (visit, bed) = Pair();
        var pakai = PakaiBedIgdModel.Open(visit, bed, TestAudit(8));

        var act = () => pakai.Close(TestAudit(7));

        act.Should().Throw<ArgumentException>().WithMessage("*lebih awal*");
    }
}
