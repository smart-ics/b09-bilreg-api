using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature;

public class IgdVisitEventDalTest
{
    private readonly IgdVisitEventDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IgdVisitEventDto Faker(int noEvent = 1)
        => new IgdVisitEventDto(
            IgdVisitId: "TIGV0000000001",
            NoEvent: noEvent,
            EventKind: "DAFTAR",
            EventDateTime: new DateTime(2026, 1, 1, 8, 0, 0),
            UserId: "U1",
            Notes: "Tes");

    private static IIgdVisitKey FakerKey() => IgdVisitModel.Key("TIGV0000000001");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        _sut.Insert(Faker(2));
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker(1));
        _sut.Insert(Faker(2));

        var actual = _sut.ListData(FakerKey()).ToList();
        actual.Should().HaveCount(2);
        actual.Should().ContainEquivalentOf(Faker(1));
        actual.Should().ContainEquivalentOf(Faker(2));
    }
}
