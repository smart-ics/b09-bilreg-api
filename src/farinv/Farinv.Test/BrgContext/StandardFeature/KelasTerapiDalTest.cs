using Farinv.Domain.BrgContext.StandardFeature;
using Farinv.Infrastructure.BrgContext.StandardFeature;
using Farinv.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.BrgContext.StandardFeature;

public class KelasTerapiDalTest
{
    private readonly KelasTerapiDal _sut = new(ConnStringHelper.GetTestEnv());

    private static KelasTerapiDto Faker()
        => new KelasTerapiDto("KT01", "Kelas Terapi A");

    private static IKelasTerapiKey FakerKey()
        => KelasTerapiType.Default with { KelasTerapiId = "KT01" };

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
