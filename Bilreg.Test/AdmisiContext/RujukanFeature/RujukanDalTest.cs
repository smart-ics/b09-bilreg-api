using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Infrastructure.AdmisiContext.RujukanSub;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RujukanFeature;

public class RujukanDalTest
{
    private readonly RujukanDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RujukanDto Faker()
        => new RujukanDto(
            fs_kd_rujukan: "A",
            fs_nm_rujukan: "B",
            fb_aktif: true,
            fs_alm_rujukan: "C",
            fs_alm2_rujukan: "D",
            fs_kota_rujukan: "E",
            fs_tlp_rujukan: "F",
            fs_kd_rujukan_tipe: "G",
            fs_kd_kelas_rs: "H",
            fs_kd_cara_masuk_dk: "I",
            fs_nm_rujukan_tipe: "J",
            fs_nm_kelas_rs: "K",
            fs_nm_cara_masuk_dk: "L"
        );

    private static IRujukanKey FakerKey()
        => RujukanType.Key("A");

    private static ITipeRujukanKey FakerTipeRujukanKey()
        => TipeRujukanType.Key("G");

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
            opt => opt.Excluding(x => x.fs_nm_rujukan_tipe)
                .Excluding(x => x.fs_nm_kelas_rs)
                .Excluding(x => x.fs_nm_cara_masuk_dk));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var rujukan = Faker();
        _sut.Insert(rujukan);
        
        var actual = _sut.ListData(FakerTipeRujukanKey());
        actual.Should().ContainEquivalentOf(rujukan,
            opt => opt.Excluding(x => x.fs_nm_rujukan_tipe)
                .Excluding(x => x.fs_nm_kelas_rs)
                .Excluding(x => x.fs_nm_cara_masuk_dk));
    }
}