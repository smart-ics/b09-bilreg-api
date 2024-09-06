using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TransportSub.AmbulanceAgg;

public class AmbulanceKomponenDalTest
{
    private readonly AmbulanceKomponenDal _sut;

    public AmbulanceKomponenDalTest()
    {
        _sut = new AmbulanceKomponenDal(ConnStringHelper.GetTestEnv());
    }
    
    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new AmbulanceKomponenModel("A", "B", 1, false);
        _sut.Insert(new List<AmbulanceKomponenModel>{expected});
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new AmbulanceKomponenModel("A", "B", 1, false);
        _sut.Delete(expected);
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new AmbulanceKomponenModel("A", "B", 1, false);
        _sut.Insert(new List<AmbulanceKomponenModel>{expected});
        var actual = _sut.ListData(expected);
        actual.Should().BeEquivalentTo(new List<AmbulanceKomponenModel>{expected});
    }
}