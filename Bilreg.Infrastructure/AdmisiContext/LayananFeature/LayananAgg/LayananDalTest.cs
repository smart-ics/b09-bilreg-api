using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.LayananFeature.LayananAgg;

public class LayananDalTest
{
    private readonly LayananDal _sut = new(ConnStringHelper.GetTestEnv());

    private static LayananDto Faker()
        => new LayananDto("A", "B", true, "C", "D", "E", "F", "G", "H", "I", "J");

    private static ILayananKey FakerKey()
        => LayananType.Key("A");

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
            opt => opt.Excluding(x => x.fs_nm_instalasi)
                .Excluding(x => x.fs_kd_instalasi_dk)
                .Excluding(x => x.fs_nm_layanan_dk)
                .Excluding(x => x.fs_nm_layanan_tipe_dk)
                .Excluding(x => x.fs_nm_instalasi_dk));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt.Excluding(x => x.fs_nm_instalasi)
                .Excluding(x => x.fs_kd_instalasi_dk)
                .Excluding(x => x.fs_nm_layanan_dk)
                .Excluding(x => x.fs_nm_layanan_tipe_dk)
                .Excluding(x => x.fs_nm_instalasi_dk));
    }
}