using Bilreg.Domain.BillContext.BedUsageFeature.TipeKamarAgg;
using Bilreg.Infrastructure.BillContext.BedUsageFeature.TipeKamarAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.TipeKamarFeature;

public class TipeKamarDalTest
{
    private readonly TipeKamarDal _sut;

    public TipeKamarDalTest()
    {
        _sut = new TipeKamarDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeKamarModel("A", "B");
        _sut.Insert(expected);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeKamarModel("A", "B");
        _sut.Update(expected);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeKamarModel("A", "B");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new TipeKamarModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }
}
