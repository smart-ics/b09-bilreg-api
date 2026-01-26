using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillingDalTest
{
    private readonly TrsBillingDal _sut = new(ConnStringHelper.GetTestEnv());
    
    private static TrsBillingDto Faker()
        => new TrsBillingDto(
            fs_kd_trs: "A",
            fn_modul: 1,
            fd_tgl_trs: "2025-01-01",
            fs_jam_trs: "10:00:00",
            fd_tgl_jam_trs: "2025-01-01 10:00:00",
            fs_kd_reg: "REG001",
            fs_kd_layanan: "LAY01",
            fs_kd_kelas: "KLS",
            fs_kd_rekap_cetak: "RC001",
            fs_kd_petugas: "PET001",
            fn_sub_total: 100000,
            fn_diskon: 10000,
            fn_biaya: 5000,
            fn_tax: 9000,
            fn_total: 104000,
            fs_keterangan: "Ket 1",
            fs_keterangan2: "Ket 2",
            fs_kd_ref_biaya: "REF",
            fn_qty: 2,
            fs_kd_trs_main: "MAIN001",
            fs_mr: "MR001",
            fs_nm_pasien: "Pasien A",
            fs_nm_layanan: "Layanan A",
            fs_nm_kelas: "Kelas A",
            fs_nm_rekap_cetak: "Rekap Cetak A"
        );
    
    private static ITrsBillingKey FakerKey()
        => TrsBillingType.Default with { TrsBillingId = "A" };
    
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
                .Excluding(x => x.fs_mr)
                .Excluding(x => x.fs_nm_pasien)
                .Excluding(x => x.fs_nm_layanan)
                .Excluding(x => x.fs_nm_kelas)
                .Excluding(x => x.fs_nm_rekap_cetak));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(RegModel.Key("REG001"));
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.fs_mr)
                .Excluding(x => x.fs_nm_pasien)
                .Excluding(x => x.fs_nm_layanan)
                .Excluding(x => x.fs_nm_kelas)
                .Excluding(x => x.fs_nm_rekap_cetak));
    }
}