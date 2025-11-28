using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.Helpers;
using Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.DemografiFeature;

public class KecamatanDalTest
{
    private readonly KecamatanDal _sut;

    public KecamatanDalTest()
    {
        _sut = new KecamatanDal(ConnStringHelper.GetTestEnv());
    }

    private static KecamatanType Faker() =>
        new KecamatanType("A", "B",
            KabupatenType.Default.ToReff(),
            PropinsiType.Default);
    
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
        _sut.Delete(KecamatanType.Key("A"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = Faker();
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = Faker();
        _sut.Insert(expected);
        var actual = _sut.ListData(KabupatenType.Key("-")).Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}