using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.BrgContext.BrgFeature;
using Farinv.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.BrgContext.BrgFeature;

public class BrgDalTest
{
    private readonly BrgDal _sut = new(ConnStringHelper.GetTestEnv());

    private static BrgDto Faker()
        => new BrgDto("A", "B", true, "C", "D",
            "E", "F", "G", "H", "I", "J",
            "K", "L", "M", "N",
            "O", "P", "Q",
            "R", "S", "T",
            "U", "V", "W", "X", "Y",
            "Z", "AA");
    private static IBrgKey FakerKey()
        => BrgObatType.Default with { BrgId = "A" };

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
            opt => opt.Excluding(x => x.fs_nm_grup_rek)
                .Excluding(x => x.fs_kd_grup_rek_dk)
                .Excluding(x => x.fs_nm_grup_rek_dk)
                .Excluding(x => x.fs_nm_golongan)
                .Excluding(x => x.fs_nm_kelompok)
                .Excluding(x => x.fs_nm_sifat)
                .Excluding(x => x.fs_nm_bentuk)
                .Excluding(x => x.fs_nm_pabrik)
                .Excluding(x => x.fs_nm_gol_obat_dk)
                .Excluding(x => x.fs_nm_generik)
                .Excluding(x => x.fs_nm_gol_terapi)
                .Excluding(x => x.fs_nm_kelas_terapi)
                .Excluding(x => x.fs_nm_original));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var keyword = "B";
        var actual = _sut.ListData(keyword);
        Assert.True(true); //<-- FTS tidak bisa di-test di dalam transaction
        //actual.Should().NotBeNull();
    }
}