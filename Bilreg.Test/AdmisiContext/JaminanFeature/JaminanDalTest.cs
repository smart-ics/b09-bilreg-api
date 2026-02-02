using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class JaminanDalTest
{
    private readonly JaminanDal _sut = new(ConnStringHelper.GetTestEnv());

    private static JaminanDto Faker()
        => new JaminanDto("A", "B", true, "C", "D", "E", "F", "G", "H", "I", "J", 
            "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "A1", "B1", "C1", "D1");

    private static IJaminanKey FakerKey()
        => JaminanType.Default with { JaminanId = "A" };

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
                .Excluding(x => x.fs_kd_pos)
                .Excluding(x => x.fs_nm_cara_bayar_dk)
                .Excluding(x => x.fs_nm_grup_jaminan)
                .Excluding(x => x.fs_nm_tarif_tipe_rawat_jalan)
                .Excluding(x => x.fs_nm_tarif_tipe_rawat_inap)
                .Excluding(x => x.fs_nm_piut_rawat)
                .Excluding(x => x.fs_nm_piut_obat_rawat)
                .Excluding(x => x.fs_nm_rek_ppdp_jasa_ri)
                .Excluding(x => x.fs_nm_rek_ppdp_obat_ri));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.fs_kd_pos)
                .Excluding(x => x.fs_nm_cara_bayar_dk)
                .Excluding(x => x.fs_nm_grup_jaminan)
                .Excluding(x => x.fs_nm_tarif_tipe_rawat_jalan)
                .Excluding(x => x.fs_nm_tarif_tipe_rawat_inap)
                .Excluding(x => x.fs_nm_piut_rawat)
                .Excluding(x => x.fs_nm_piut_obat_rawat)
                .Excluding(x => x.fs_nm_rek_ppdp_jasa_ri)
                .Excluding(x => x.fs_nm_rek_ppdp_obat_ri));
    }
}