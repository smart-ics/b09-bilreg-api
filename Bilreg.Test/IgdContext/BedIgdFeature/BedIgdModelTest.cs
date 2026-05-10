using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.IgdContext.BedIgdFeature;

public class BedIgdModelTest
{
    private static AuditInfoType TestAudit() => new("U1", new DateTime(2026, 1, 1, 8, 0, 0));

    private static BedIgdModel ActiveBed() =>
        BedIgdModel.CreateMaster("B01", "Bed 1", "Kamar IGD A", TestAudit());

    [Fact]
    public void CreateMaster_ProducesActiveAndUnoccupied()
    {
        var bed = ActiveBed();

        bed.BedState.Should().Be(BedStateEnum.Active);
        bed.IsOccupied.Should().BeFalse();
        bed.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void Occupy_FromActive_TransitionsToOccupied()
    {
        var bed = ActiveBed();

        bed.Occupy("IGV1", TestAudit());

        bed.BedState.Should().Be(BedStateEnum.Occupied);
        bed.CurrentIgdVisitId.Should().Be("IGV1");
    }

    [Fact]
    public void Occupy_AlreadyOccupied_Throws()
    {
        var bed = ActiveBed();
        bed.Occupy("IGV1", TestAudit());

        var act = () => bed.Occupy("IGV2", TestAudit());

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah ditempati*");
    }

    [Fact]
    public void Occupy_WhenMaintenance_Throws()
    {
        var bed = ActiveBed();
        bed.MarkMaintenance(TestAudit());

        var act = () => bed.Occupy("IGV1", TestAudit());

        act.Should().Throw<InvalidOperationException>().WithMessage("*Active*");
    }

    [Fact]
    public void Release_FromOccupied_TransitionsToDirty()
    {
        var bed = ActiveBed();
        bed.Occupy("IGV1", TestAudit());

        bed.Release(TestAudit());

        bed.BedState.Should().Be(BedStateEnum.Dirty);
        bed.IsOccupied.Should().BeFalse();
    }

    [Fact]
    public void Release_WhenNotOccupied_Throws()
    {
        var bed = ActiveBed();
        var act = () => bed.Release(TestAudit());
        act.Should().Throw<InvalidOperationException>().WithMessage("*tidak sedang ditempati*");
    }

    [Fact]
    public void MarkClean_FromDirty_TransitionsToActive()
    {
        var bed = ActiveBed();
        bed.Occupy("IGV1", TestAudit());
        bed.Release(TestAudit());

        bed.MarkClean(TestAudit());

        bed.BedState.Should().Be(BedStateEnum.Active);
    }

    [Fact]
    public void MarkMaintenance_WhileOccupied_Throws()
    {
        var bed = ActiveBed();
        bed.Occupy("IGV1", TestAudit());

        var act = () => bed.MarkMaintenance(TestAudit());

        act.Should().Throw<InvalidOperationException>().WithMessage("*ditempati*");
    }

    [Fact]
    public void Snapshot_OnConstruction_MirrorsCurrentState()
    {
        var bed = ActiveBed();
        bed.BedStateSnapshot.Should().Be(BedStateEnum.Active);
        bed.CurrentIgdVisitIdSnapshot.Should().Be("-");
    }

    [Fact]
    public void Snapshot_AfterMutation_RemainsAtConstructionTimeValue()
    {
        var bed = ActiveBed();
        bed.Occupy("IGV1", TestAudit());

        // snapshot tracks what was loaded; mutation does NOT update it
        bed.BedStateSnapshot.Should().Be(BedStateEnum.Active);
        bed.CurrentIgdVisitIdSnapshot.Should().Be("-");
        bed.BedState.Should().Be(BedStateEnum.Occupied);
        bed.CurrentIgdVisitId.Should().Be("IGV1");
    }
}
