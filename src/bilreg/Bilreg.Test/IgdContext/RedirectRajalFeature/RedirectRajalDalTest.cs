using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.RedirectRajalFeature;
using Bilreg.Infrastructure.IgdContext.RedirectRajalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.RedirectRajalFeature;

public class RedirectRajalDalTest
{
    private readonly RedirectRajalDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RedirectRajalDto Faker(string id = "TRDR0000001")
        => new RedirectRajalDto(
            RedirectRajalId: id,
            IgdVisitId: "TIGV0000000001",
            VisitorName: "Tes Visitor",
            RedirectDateTime: new DateTime(2026, 1, 1, 10, 0, 0),
            Reason: "Rajal lanjut",
            RedirectUserId: "U1");

    private static IRedirectRajalKey FakerKey() => RedirectRajalModel.Key("TRDR0000001");
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
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker("TRDR0000001"));
        _sut.Insert(Faker("TRDR0000002"));
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
