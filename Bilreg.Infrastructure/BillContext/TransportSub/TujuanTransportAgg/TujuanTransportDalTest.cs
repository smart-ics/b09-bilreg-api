using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TransportSub.TujuanTransportAgg;

public class TujuanTransportDalTest
{
    private readonly TujuanTransportDal _sut;

    public TujuanTransportDalTest()
    {
        _sut = new TujuanTransportDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TujuanTransportModel("A", "B", 10, false);
        _sut.Insert(expected);
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TujuanTransportModel("A", "B", 10, false);
        _sut.Update(expected);
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TujuanTransportModel("A", "B", 10, false);
        _sut.Delete(expected);
    }
    
    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TujuanTransportModel("A", "B", 10, false);
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TujuanTransportModel("A", "B", 10, false);
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
}