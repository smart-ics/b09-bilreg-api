using Bilreg.Domain.BillContext.TindakanSub.GrupTarifDkAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.GrupTarifDkAgg;

public class GrupTarifDkDalTest
{
    private readonly GrupTarifDkDal _sut;

    public GrupTarifDkDalTest()
    {
        _sut = new GrupTarifDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupTarifDkModel("01","SEDERHANA");
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupTarifDkModel("01","SEDERHANA");
        
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }

}