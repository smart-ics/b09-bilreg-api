using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.TindakanIgdFeature;

public class TindakanIgdDalTest
{
    private readonly TindakanIgdDal _sut = new(ConnStringHelper.GetTestEnv());

    private static TindakanIgdDto Faker(string id = "TTKI0000001")
        => new TindakanIgdDto(
            TindakanIgdId: id,
            IgdVisitId: "TIGV0000000001",
            RegId: "R001",
            TarifId: "TR001",
            TarifName: "Tindakan A",
            Qty: 2,
            Price: 50000m,
            CrtUser: "U1",
            CrtDate: new DateTime(2026, 1, 1, 12, 0, 0));

    private static ITindakanIgdKey FakerKey() => TindakanIgdModel.Key("TTKI0000001");
    private static IIgdVisitKey VisitKey() => IgdVisitModel.Key("TIGV0000000001");

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
    public void ListDataForVisitTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker("TTKI0000001"));
        _sut.Insert(Faker("TTKI0000002"));
        var actual = _sut.ListData(VisitKey()).ToList();
        actual.Should().HaveCount(2);
    }

    [Fact]
    public void CountForVisit_AfterInsert_ReturnsPositive()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var count = _sut.CountForVisit(VisitKey());
        count.Should().Be(1);
    }

    [Fact]
    public void CountForVisit_NoRows_ReturnsZero()
    {
        using var trans = TransHelper.NewScope();
        var count = _sut.CountForVisit(VisitKey());
        count.Should().Be(0);
    }
}
