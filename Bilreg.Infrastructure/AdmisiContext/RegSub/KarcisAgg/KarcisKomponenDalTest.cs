using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RegSub.KarcisAgg;

public class KarcisKomponenDalTest
{
    private readonly KarcisKomponenDal _sut;

    public KarcisKomponenDalTest()
    {
        _sut = new KarcisKomponenDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void Insert_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KarcisKomponenModel("A", "B", "C", 1);
        _sut.Insert(new List<KarcisKomponenModel>() {expected});
    }
    
    [Fact]
    public void Delete_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KarcisKomponenModel("A", "B", "C", 1);
        _sut.Delete(expected);
    }
    
    [Fact]
    public void ListData_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KarcisKomponenModel("A", "B", "", 1);
        _sut.Insert(new List<KarcisKomponenModel>() {expected});
        var actual = _sut.ListData(expected);
        actual.Should().BeEquivalentTo(new List<KarcisKomponenModel>() { expected });
    }
}