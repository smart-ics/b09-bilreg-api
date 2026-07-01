using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.BrgContext.KlasifikasiFeature;

public class GroupObatDkDalTest
{
    private readonly GroupObatDkDal _sut = new(ConnStringHelper.GetTestEnv());

    private static GroupObatDkDto Faker()
        => new GroupObatDkDto("GOD", "Group Obat DK A");

    private static IGroupObatDkKey FakerKey()
        => GroupObatDkType.Default with { GroupObatDkId = "GOD" };

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