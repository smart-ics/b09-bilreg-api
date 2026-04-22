using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapDalTest
{
    private readonly AntrianMapDal _sut = new AntrianMapDal(ConnStringHelper.GetTestEnv());

    private AntrianMapDto Faker()
    {
        var tglJadwal = new DateTime(2025, 2, 3);
        var result = new AntrianMapDto(
            "AAAA",
            "BBB",
            "PPA001",
            "LYN001",
            tglJadwal,
            "08:00",
            "08:30",
            "CCC",
            30,
            "Dokter-A", "Layanan-B"
        );
        return result;
    }
    
    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var antrianFaker = Faker();
        _sut.Insert(antrianFaker);
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var antrianFaker = Faker();
        _sut.Update(antrianFaker);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var antrianFaker = Faker();
        _sut.Insert(antrianFaker);

        var actual = _sut.GetData(AntrianMapModel.Key(antrianFaker.fs_kd_antrian_map));

        actual.Should().BeEquivalentTo(antrianFaker, opt => opt
            .Excluding(x => x.fs_nm_dokter)
            .Excluding(x => x.fs_nm_layanan)
        );
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var antrianFaker = Faker();
        _sut.Insert(antrianFaker);
        var layananKey = LayananType.Key(antrianFaker.fs_kd_layanan);
        var ppaKey = PpaType.Key(antrianFaker.fs_kd_dokter);
        
        var actual = _sut.ListData(layananKey, ppaKey, 
            DateOnly.FromDateTime(antrianFaker.fd_tgl_jadwal));

        actual.Should().BeEquivalentTo(new List<AntrianMapDto>{Faker()}, opt => opt
            .Excluding(x => x.fs_nm_dokter)
            .Excluding(x => x.fs_nm_layanan)
        );
    }
    
}