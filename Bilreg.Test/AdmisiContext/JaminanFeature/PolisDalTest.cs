using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class PolisDalTest
{
    private readonly PolisDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PolisDto Faker()
        => new PolisDto(
            fs_kd_polis: "A",
            fs_no_polis: "B",
            fs_atas_nama: "C",
            fd_expired: "D",
            fs_kd_tipe_jaminan: "E",
            fb_cover_rj: true,
            fs_kd_kelas_ri: "F",
            fs_nm_tipe_jaminan: "G",
            fs_nm_kelas: "H"
        );

    private static IPolisKey FakerKey()
        => PolisModel.Key("A");

    private static IPasienKey FakerPasienKey()
        => PasienModel.Key("I");

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
            opt => opt.Excluding(x => x.fs_nm_tipe_jaminan)
                .Excluding(x => x.fs_nm_kelas));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        // Note: ListData requires data in ta_polis_cover table
        // You may need to insert related data for this test to work
        var actual = () => _sut.ListData(FakerPasienKey());
        actual.Should().NotThrow<Exception>();
    }
}
