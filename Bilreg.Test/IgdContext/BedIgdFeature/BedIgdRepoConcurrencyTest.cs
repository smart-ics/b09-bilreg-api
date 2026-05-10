using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.IgdContext.BedIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.BedIgdFeature;

public class BedIgdRepoConcurrencyTest
{
    private readonly BedIgdDal _dal = new(ConnStringHelper.GetTestEnv());
    private readonly BedIgdRepo _sut;

    public BedIgdRepoConcurrencyTest()
    {
        _sut = new BedIgdRepo(_dal);
    }

    private static AuditInfoType TestAudit() => new("U1", new DateTime(2026, 1, 1, 8, 0, 0));

    private void SeedActiveBed()
    {
        var dto = new BedIgdDto(
            BedIgdId: "TBED02",
            BedIgdName: "Bed Tes 2",
            KamarName: "K1",
            BedState: "ACTIVE",
            CurrentIgdVisitId: "",
            OccupyUser: "",
            OccupyDateTime: new DateTime(3000, 1, 1),
            CrtUser: "U1", CrtDate: new DateTime(2026, 1, 1),
            UpdUser: "U1", UpdDate: new DateTime(2026, 1, 1),
            VodUser: "", VodDate: new DateTime(3000, 1, 1));
        _dal.Insert(dto);
    }

    [Fact]
    public void AssignBed_TwoTransactions_OnlyOneSucceeds()
    {
        using var trans = TransHelper.NewScope();
        SeedActiveBed();

        // Two parallel handlers each load their own snapshot then mutate.
        var bedA = _sut.LoadEntity(BedIgdModel.Key("TBED02")).Value;
        var bedB = _sut.LoadEntity(BedIgdModel.Key("TBED02")).Value;

        bedA.Occupy("IGV0001", TestAudit());
        bedB.Occupy("IGV0002", TestAudit());

        // First save wins
        _sut.SaveChanges(bedA);

        // Second save's prior snapshot is now stale -> CAS fails
        var act = () => _sut.SaveChanges(bedB);
        act.Should().Throw<InvalidOperationException>().WithMessage("*occupancy stale*");

        var stored = _sut.LoadEntity(BedIgdModel.Key("TBED02")).Value;
        stored.CurrentIgdVisitId.Should().Be("IGV0001");
        stored.BedState.Should().Be(BedStateEnum.Occupied);
    }

    [Fact]
    public void Release_AfterAssign_TransitionsToDirty()
    {
        using var trans = TransHelper.NewScope();
        SeedActiveBed();

        var bed = _sut.LoadEntity(BedIgdModel.Key("TBED02")).Value;
        bed.Occupy("IGV0001", TestAudit());
        _sut.SaveChanges(bed);

        var occupied = _sut.LoadEntity(BedIgdModel.Key("TBED02")).Value;
        occupied.Release(TestAudit());
        _sut.SaveChanges(occupied);

        var stored = _sut.LoadEntity(BedIgdModel.Key("TBED02")).Value;
        stored.BedState.Should().Be(BedStateEnum.Dirty);
        stored.CurrentIgdVisitId.Should().Be("-");
    }
}
