using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RegSub.KarcisAgg;

public class KarcisLayananDalTest
{
    private readonly KarcisLayananDal _sut;

    public KarcisLayananDalTest()
    {
        _sut = new KarcisLayananDal(ConnStringHelper.GetTestEnv());
    }
    
    [Fact]
    public void Insert_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KarcisLayananModel("A", "B", "C");
        _sut.Insert(new List<KarcisLayananModel>() {expected});
    }
    
    [Fact]
    public void Delete_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KarcisLayananModel("A", "B", "C");
        _sut.Delete(expected);
    }
    
    [Fact]
    public void ListData_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KarcisLayananModel("A", "B", "");
        _sut.Insert(new List<KarcisLayananModel>() {expected});
        var actual = _sut.ListData(expected);
        actual.Should().BeEquivalentTo(new List<KarcisLayananModel>() { expected });
    }
}