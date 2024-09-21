using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RegSub.KarcisAgg;

public class KarcisDalTest
{
    private readonly KarcisDal _sut;
    
    public KarcisDalTest()
    {
        _sut = new KarcisDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void Insert_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KarcisModel("A", "B");
        _sut.Insert(expected);
    }

    [Fact]
    public void Update_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KarcisModel("A", "B");
        _sut.Update(expected);
    }

    [Fact]
    public void GetData_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KarcisModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListData_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new KarcisModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
}