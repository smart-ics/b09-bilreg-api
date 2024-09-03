using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.LayananAgg;

public class LayananDalTest
{
    private readonly LayananDal _sut;

    public LayananDalTest()
    {
        _sut = new LayananDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new LayananModel("A", "B");
        _sut.Insert(expected);
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new LayananModel("A", "B");
        _sut.Update(expected);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new LayananModel("A", "B");
        _sut.Delete(expected);  
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new LayananModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListData()
    {
        var trans = TransHelper.NewScope();
        var expected = new LayananModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
}