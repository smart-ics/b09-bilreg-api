using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.PolisAgg;

public class PolisDalTest
{
    private readonly PolisDal _sut;

    public PolisDalTest()
    {
        _sut = new PolisDal(ConnStringHelper.GetTestEnv());
    }
    
    private static PolisModel Faker()
        => new PolisDto
        {
            fs_kd_polis = "A",
            fs_atas_nama = "B",
            fs_kd_tipe_jaminan = "C",
            fs_nm_tipe_jaminan = "",
            fs_kd_kelas_ri = "D",
            fs_nm_kelas_ri = "",
            fb_cover_rj = true,
            fd_expired = "2022-01-01",
            fs_no_polis = "12345",
        };
    
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
        _sut.Insert(Faker());
        _sut.Delete(Faker());
    }
    
    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var faker = Faker();
        _sut.Insert(faker);
        var actual = _sut.GetData(faker);
        actual.Should().BeEquivalentTo(faker);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var faker = Faker();
        _sut.Insert(Faker());
        var actual = _sut.ListData(new PasienModel("A", "B"));
    }
    
    
}