using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.TipeTarifAgg;

public class TipeTarifDalTest
{
    private readonly TipeTarifDal _sut;

    public TipeTarifDalTest()
    {
        _sut = new TipeTarifDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void Insert_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeTarifDto();
        expected.SetDataTipeTarif();
        _sut.Insert(expected);
    }
    
    [Fact]
    public void Update_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeTarifDto();
        expected.SetDataTipeTarif();
        _sut.Insert(expected);
    }
    
    [Fact]
    public void Delete_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeTarifDto();
        expected.SetDataTipeTarif();
        _sut.Delete(expected);
    }
    
    [Fact]
    public void GetQuery_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeTarifDto();
        expected.SetDataTipeTarif();
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void ListQuery_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeTarifDto();
        expected.SetDataTipeTarif();
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
    
}