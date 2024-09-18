using Bilreg.Domain.BillContext.TindakanSub.GrupTarifAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.GrupTarifAgg;

public class GrupTarifDalTest
{
    private readonly GrupTarifDal _sut;

    public GrupTarifDalTest()
    {
        _sut = new GrupTarifDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void Insert_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupTarifModel("A", "B");
        _sut.Insert(expected);
    }
    
    [Fact]
    public void Update_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupTarifModel("A", "B");
        _sut.Update(expected);
    }
    
    [Fact]
    public void Delete_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupTarifModel("A", "B");
        _sut.Delete(expected);
    }
    
    [Fact]
    public void GetData_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupTarifModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void ListData_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupTarifModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
}