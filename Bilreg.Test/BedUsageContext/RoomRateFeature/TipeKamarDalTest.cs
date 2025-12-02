using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Infrastructure.BedUsageContext.RoomRateFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.RoomRateFeature;

public class TipeKamarDalTest
{
    private readonly TipeKamarDal _sut = new(ConnStringHelper.GetTestEnv());

    private static TipeKamarDto Faker()
        => new TipeKamarDto("A", "B", true, true, false);

    private static ITipeKamarKey FakerKey()
        => TipeKamarType.Default with { TipeKamarId = "A" };

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker());
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker());
    }
}