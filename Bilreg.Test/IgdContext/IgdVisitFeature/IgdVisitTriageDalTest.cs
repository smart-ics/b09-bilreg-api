using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature;

public class IgdVisitTriageDalTest
{
    private readonly IgdVisitTriageDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IgdVisitTriageDto Faker(int noTriage = 1, string level = "P3")
        => new IgdVisitTriageDto(
            IgdVisitId: "TIGV0000000001",
            NoTriage: noTriage,
            TriageLevel: level,
            AssessmentDateTime: new DateTime(2026, 1, 1, 8, 30, 0),
            AssessorUserId: "U1",
            Notes: "TD turun");

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
        _sut.Insert(Faker(1));
        _sut.Insert(Faker(2, "P2"));
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker(1, "P3"));
        _sut.Insert(Faker(2, "P2"));

        var actual = _sut.ListData(FakerKey()).ToList();
        actual.Should().HaveCount(2);
        actual.Should().ContainEquivalentOf(Faker(1, "P3"));
        actual.Should().ContainEquivalentOf(Faker(2, "P2"));
    }
}
