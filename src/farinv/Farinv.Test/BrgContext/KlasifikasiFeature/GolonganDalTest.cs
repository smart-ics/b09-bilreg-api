using Farinv.Domain.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.BrgContext.KlasifikasiFeature;
using Farinv.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.BrgContext.KlasifikasiFeature;

public class GolonganDalTest
{
    private readonly GolonganDal _sut = new(ConnStringHelper.GetTestEnv());

    private static GolonganDto Faker()
        => new GolonganDto("GLN", "Golongan A");

    private static IGolonganKey FakerKey()
        => GolonganType.Default with { GolonganId = "GLN" };

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