using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegDalTest
{
    private readonly RegDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RegDto Faker()
        => new RegDto("A", "2024-01-01", "08:00:00",
            "D", "E", "F", "G", "H", "I", "J", "K",
            "L", "M", "N", "O", "P", "Q", "R",
            "S", "T", "U", "V", "W", "X", "Y",
            "Z", "AA", "AB", "AC");

    private static IRegKey FakerKey()
        => RegModel.Key("A");

    private static Periode FakerPeriode()
        => new Periode(new DateTime(2024, 1, 1),new DateTime(2024, 1, 31));

    private static ILayananKey FakerLayananKey()
        => LayananType.Key("R");

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
            opt => opt.Excluding(x => x.fs_nm_pasien)
                .Excluding(x => x.fd_tgl_lahir)
                .Excluding(x => x.fs_jns_kelamin)
                .Excluding(x => x.fs_nm_tipe_jaminan)
                .Excluding(x => x.fs_nm_kelas)
                .Excluding(x => x.fs_nm_cara_masuk_dk)
                .Excluding(x => x.fs_nm_rujukan)
                .Excluding(x => x.fs_nm_medis)
                .Excluding(x => x.fs_nm_layanan)
                .Excluding(x => x.fs_nm_karcis));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var reg = Faker();
        _sut.Insert(reg);
        
        var actual = _sut.ListData(FakerPeriode(), FakerLayananKey());
        actual.Should().ContainEquivalentOf(reg,
            opt => opt.Excluding(x => x.fs_nm_pasien)
                .Excluding(x => x.fd_tgl_lahir)
                .Excluding(x => x.fs_jns_kelamin)
                .Excluding(x => x.fs_nm_tipe_jaminan)
                .Excluding(x => x.fs_nm_kelas)
                .Excluding(x => x.fs_nm_cara_masuk_dk)
                .Excluding(x => x.fs_nm_rujukan)
                .Excluding(x => x.fs_nm_medis)
                .Excluding(x => x.fs_nm_layanan)
                .Excluding(x => x.fs_nm_karcis));
    }
}