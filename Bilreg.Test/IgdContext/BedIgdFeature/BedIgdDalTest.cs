using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Infrastructure.IgdContext.BedIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.BedIgdFeature;

public class BedIgdDalTest
{
    private readonly BedIgdDal _sut = new(ConnStringHelper.GetTestEnv());

    private static BedIgdDto Faker(string state = "ACTIVE", string visitId = "")
        => new BedIgdDto(
            BedIgdId: "TBED01",
            BedIgdName: "Bed Tes",
            KamarName: "K1",
            BedState: state,
            CurrentIgdVisitId: visitId,
            OccupyUser: "",
            OccupyDateTime: new DateTime(3000, 1, 1),
            CrtUser: "U1", CrtDate: new DateTime(2026, 1, 1, 8, 0, 0),
            UpdUser: "U1", UpdDate: new DateTime(2026, 1, 1, 8, 0, 0),
            VodUser: "", VodDate: new DateTime(3000, 1, 1));

    private static IBedIgdKey FakerKey() => BedIgdModel.Key("TBED01");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker());
    }

    [Fact]
    public void UpdateOccupancyConditional_GivenMatchingPrior_UpdatesAndReturnsOne()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());

        var occupied = Faker("OCCUPIED", "IGV0001") with { OccupyUser = "U1", OccupyDateTime = new DateTime(2026, 1, 1, 9, 0, 0) };
        var rows = _sut.UpdateOccupancyConditional(occupied, priorState: "ACTIVE", priorVisitId: "");

        rows.Should().Be(1);
        var current = _sut.GetData(FakerKey());
        current.BedState.Should().Be("OCCUPIED");
        current.CurrentIgdVisitId.Should().Be("IGV0001");
    }

    [Fact]
    public void UpdateOccupancyConditional_GivenStalePrior_ReturnsZero()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker("OCCUPIED", "IGV0001"));

        // Pretend we believed the prior was ACTIVE+empty (stale snapshot)
        var would = Faker("OCCUPIED", "IGV0002");
        var rows = _sut.UpdateOccupancyConditional(would, priorState: "ACTIVE", priorVisitId: "");

        rows.Should().Be(0);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker());
    }

    [Fact]
    public void ListAvailableTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListAvailable();
        actual.Should().ContainEquivalentOf(Faker());
    }

    [Fact]
    public void ListAvailable_ExcludesOccupied()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker("OCCUPIED", "IGV0001"));
        var actual = _sut.ListAvailable();
        actual.Should().NotContain(x => x.BedIgdId == "TBED01");
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        _sut.Delete(FakerKey());
    }
}
