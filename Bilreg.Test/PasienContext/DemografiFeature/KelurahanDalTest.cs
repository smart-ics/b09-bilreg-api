using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.DemografiFeature;

public class KelurahanDalTest
{
    private readonly KelurahanDal _sut;

    public KelurahanDalTest()
    {
        _sut = new KelurahanDal(ConnStringHelper.GetTestEnv());
    }

    private static KelurahanDto Faker() =>
        new KelurahanDto("A", "B", "C", "D", "E", "F", "G", "H");
    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(KelurahanType.Key("A"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = Faker();
        _sut.Insert(expected);
        var actual = _sut.GetData(KelurahanType.Key("A"));
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = Faker();
        _sut.Insert(expected);
        var actual = _sut.ListData(KecamatanType.Key("C"));
        actual.Should().ContainEquivalentOf(expected);
    }
}