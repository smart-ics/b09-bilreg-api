using Bilreg.Infrastructure.PaymentContext.RegOutFeature;
using Bilreg.Infrastructure.Shared.Helpers;

namespace Bilreg.Test.PaymentContext.RegOutFeature;

public class RegHutangDalTest
{
    private readonly RegHutangDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RegHutangDto Faker()
        => new RegHutangDto(
            fs_kd_reg: "RG001",
            fd_tgl_piutang: "2026-02-27",
            fn_piutang: 50000,
            fn_sisa: 50000,
            fn_nilai_jasa: 30000,
            fn_nilai_obat: 20000
        );


    //[Fact]
    //public void ListDataTest()
    //{
    //    using var trans = TransHelper.NewScope();
    //    var actual = _sut.ListData(PasienModel.Key("MR001"));
    //    actual.Should().ContainEquivalentOf(Faker(),
    //        opt => opt
    //            .Excluding(x => x.fs_kd_reg)
    //            .Excluding(x => x.fd_tgl_piutang)
    //            .Excluding(x => x.fn_piutang)
    //            .Excluding(x => x.fn_sisa)
    //            .Excluding(x => x.fn_nilai_jasa)
    //            .Excluding(x => x.fn_nilai_obat));
    //}

}