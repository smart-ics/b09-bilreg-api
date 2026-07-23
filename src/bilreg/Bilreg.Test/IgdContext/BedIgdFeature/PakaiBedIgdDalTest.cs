using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.IgdContext.BedIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.BedIgdFeature;

public class PakaiBedIgdDalTest
{
    private readonly PakaiBedIgdDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PakaiBedIgdDto Faker(string id = "TPBI000001", DateTime? checkOut = null)
        => new PakaiBedIgdDto(
            PakaiBedIgdId: id,
            IgdVisitId: "TIGV0000000001",
            BedIgdId: "TBED01",
            BedIgdName: "Bed Tes",
            CheckInDateTime: new DateTime(2026, 1, 1, 9, 0, 0),
            CheckInUserId: "U1",
            CheckOutDateTime: checkOut ?? new DateTime(3000, 1, 1),
            CheckOutUserId: checkOut.HasValue ? "U1" : "");

    private static IPakaiBedIgdKey FakerKey() => PakaiBedIgdModel.Key("TPBI000001");
    private static IBedIgdKey BedKey() => BedIgdModel.Key("TBED01");
    private static IIgdVisitKey VisitKey() => IgdVisitModel.Key("TIGV0000000001");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UpdateClosesRow()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var closed = Faker(checkOut: new DateTime(2026, 1, 1, 11, 0, 0));
        _sut.Update(closed);
        var actual = _sut.GetData(FakerKey());
        actual.CheckOutDateTime.Should().Be(new DateTime(2026, 1, 1, 11, 0, 0));
    }

    [Fact]
    public void GetOpenForBedTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var open = _sut.GetOpenForBed(BedKey());
        open.Should().NotBeNull();
        open!.PakaiBedIgdId.Should().Be("TPBI000001");
    }

    [Fact]
    public void GetOpenForBed_AfterClose_ReturnsNull()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker(checkOut: new DateTime(2026, 1, 1, 11, 0, 0)));
        var open = _sut.GetOpenForBed(BedKey());
        open.Should().BeNull();
    }

    [Fact]
    public void GetOpenForVisitTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var open = _sut.GetOpenForVisit(VisitKey());
        open.Should().NotBeNull();
        open!.PakaiBedIgdId.Should().Be("TPBI000001");
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker("TPBI000001"));
        _sut.Insert(Faker("TPBI000002"));

        var actual = _sut.ListData(VisitKey()).ToList();
        actual.Should().HaveCount(2);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        _sut.Delete(FakerKey());
    }
}
