using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.PasienContext.DemografiSub;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.DemografiFeature;

public class KabupatenDalTest
{
    private readonly KabupatenDal _sut;

    public KabupatenDalTest()
    {
        _sut = new KabupatenDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new KabupatenType("A", "B", PropinsiType.Default));
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new KabupatenType("A", "B", PropinsiType.Default));
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(KabupatenType.Key("A"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KabupatenType("A", "B", PropinsiType.Default);
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KabupatenType("A", "B", PropinsiType.Default);
        _sut.Insert(expected);
        var actual = _sut.ListData(expected.Propinsi).Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}