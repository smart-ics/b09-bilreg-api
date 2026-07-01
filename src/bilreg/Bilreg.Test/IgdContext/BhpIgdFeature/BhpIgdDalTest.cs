using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.IgdContext.BhpIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.BhpIgdFeature;

public class BhpIgdDalTest
{
    private readonly BhpIgdDal _sut = new(ConnStringHelper.GetTestEnv());

    private static BhpIgdDto Faker(string id = "TBHI0000001")
        => new BhpIgdDto(
            BhpIgdId: id,
            IgdVisitId: "TIGV0000000001",
            RegId: "R001",
            BhpItemId: "BH001",
            BhpItemName: "Spuit 5cc",
            Qty: 3,
            Price: 5000m,
            CrtUser: "U1",
            CrtDate: new DateTime(2026, 1, 1, 12, 0, 0));

    private static IBhpIgdKey FakerKey() => BhpIgdModel.Key("TBHI0000001");
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
        _sut.Insert(Faker("TBHI0000001"));
        _sut.Insert(Faker("TBHI0000002"));
        var actual = _sut.ListData(VisitKey()).ToList();
        actual.Should().HaveCount(2);
    }

    [Fact]
    public void CountForVisitTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var count = _sut.CountForVisit(VisitKey());
        count.Should().Be(1);
    }
}
