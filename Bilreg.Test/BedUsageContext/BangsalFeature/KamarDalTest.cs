using Bilreg.Infrastructure.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.BangsalFeature;

public class KamarDalTest
{
    private readonly KamarDal _sut = new(ConnStringHelper.GetTestEnv());

    private static KamarDto Faker()
        => new KamarDto("A", "B", "C", "D", "E", "F");

    private static IKamarKey FakerKey()
        => KamarType.Default with { KamarId = "A" };

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
        actual.Should().BeEquivalentTo(Faker(),
            opt => opt
                .Excluding(x => x.fs_nm_bangsal)
                .Excluding(x => x.fs_nm_kelas));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.fs_nm_bangsal)
                .Excluding(x => x.fs_nm_kelas));
    }
}