using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienDalTest
{
    private readonly PasienDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PasienDto Faker()
    => new PasienDto("A1", "A2", "2000-02-03", "A", "A4", "A5", "A6", "B", 
        "B1", "B2", "B3", "B4", "B5", "B6", 
        "C", "C1", "C2", "C3", "C4", "C5", 
        "D1", "D2", "D3", "D4", "D5", "D6", "D7",
        "E", "F", "G", "H", "I", "F1", true, "-", "-", "-", "-", "-", "-", 
        "-", "-", "-", "-", "-", "-");

    [Fact]
    public void UT1_InserTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UT2_InserTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }
    
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(PasienModel.Key("A1"));
    }
    
    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(PasienModel.Key("A1"));
        actual.Should().BeEquivalentTo(Faker(),
            opt => opt
                .Excluding(x => x.fs_nm_kelurahan)
                .Excluding(x => x.fs_nm_kecamatan)
                .Excluding(x => x.fs_nm_kabupaten)
                .Excluding(x => x.fs_nm_propinsi)
                .Excluding(x => x.fs_nm_agama)
                .Excluding(x => x.fs_nm_suku)
                .Excluding(x => x.fs_nm_status_kawin_dk)
                .Excluding(x => x.fs_nm_pendidikan_dk)
                .Excluding(x => x.fs_nm_pekerjaan_dk)
            );
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(new DateTime(2000,2,3));
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.fs_nm_kelurahan)
                .Excluding(x => x.fs_nm_kecamatan)
                .Excluding(x => x.fs_nm_kabupaten)
                .Excluding(x => x.fs_nm_propinsi)
                .Excluding(x => x.fs_nm_agama)
                .Excluding(x => x.fs_nm_suku)
                .Excluding(x => x.fs_nm_status_kawin_dk)
                .Excluding(x => x.fs_nm_pendidikan_dk)
                .Excluding(x => x.fs_nm_pekerjaan_dk)
        );

    }

    [Fact]
    public void  UT6_ListDataByNameTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        Dictionary<string, string[]> names = new()
        {
            { "A2", ["A2"] }
        };
        var actual = _sut.ListDataByName(names);

    }
}

