using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.RoomChargeSub.BangsalAgg;

public class BangsalDalTest
{
    private readonly BangsalDal _sut;
    public BangsalDalTest()
    {
        _sut = new BangsalDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new BangsalModel("A", "B");
        _sut.Insert(expected);
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new BangsalModel("A", "B");
        _sut.Update(expected);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new BangsalModel("A", "B");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetQueryTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new BangsalModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListQueryTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new BangsalModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
    
}
