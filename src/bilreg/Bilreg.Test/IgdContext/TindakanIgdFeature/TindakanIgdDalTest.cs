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
            IgdVisitId: "TIGV00000001",
            RegId: "R001",
            ReffId: "TR001",
            Descriptions: "Tindakan A",
            Qty: 2,
            Aktifitas: 0,
            PpaId: "DR01",
            PpaName: "DOKTER1",
            TrsTdkId: "TDK001",
            TrsBillId: "BIL001",
            IsPosted: true,
            PostedDate: new DateTime(2026, 1, 1, 12, 30, 0),
            CrtUser: "U1",
            CrtDate: new DateTime(2026, 1, 1, 12, 0, 0));

    private static ITindakanIgdKey FakerKey() => TindakanIgdModel.Key("TTKI0000001");
    private static IIgdVisitKey VisitKey() => IgdVisitModel.Key("TIGV00000001");

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
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var initial = Faker();
        _sut.Insert(initial);
        var updated = initial with
        {
            RegId = "R999",
            TrsTdkId = "TDK_NEW",
            TrsBillId = "BIL_NEW",
            IsPosted = true,
            PostedDate = new DateTime(2026, 1, 1, 14, 0, 0)
        };
        _sut.Update(updated);
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(updated);
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
