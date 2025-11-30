using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class GrupJaminanDalTest
{
    private readonly GrupJaminanDal _sut;

    public GrupJaminanDalTest()
    {
        _sut = new GrupJaminanDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GroupJaminanType("A", "B", true, "C");
        _sut.Insert(expected);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GroupJaminanType("A", "B", true, "C");
        _sut.Update(expected);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GroupJaminanType("A", "B", true, "C");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GroupJaminanType("A", "B", true, "C");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GroupJaminanType("A", "B", true, "C");
        _sut.Insert(expected);
        var actual = _sut.ListData().Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}