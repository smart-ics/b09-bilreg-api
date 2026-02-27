using Bilreg.Infrastructure.PaymentContext.RegOutFeature;
using Bilreg.Infrastructure.Shared.Helpers;

namespace Bilreg.Test.PaymentContext.RegOutFeature;

public class RegBiayaDalTest
{
    private readonly RegBiayaDal _sut = new(ConnStringHelper.GetTestEnv());

    private static RegBiayaDto Faker()
        => new RegBiayaDto(
            fs_kd_reg: "RG001",
            fs_kd_trs_bl_admin: "BL001",
            fn_bl_admin: 5000,
            fs_kd_trs_bl_materai: "BL003",
            fn_bl_materai: 1000,
            fs_kd_trs_bl_bulat_jasa: "",
            fn_bl_bulat_jasa: 0,
            fs_kd_trs_bl_bulat_obat: "",
            fn_bl_bulat_obat: 0
        );

    //[Fact]
    //public void ListDataTest()
    //{
    //    using var trans = TransHelper.NewScope();
    //    var actual = _sut.ListData(RegModel.Key("RG001"));
    //    actual.Should().ContainEquivalentOf(Faker(),
    //        opt => opt
    //            .Excluding(x => x.fs_kd_reg)
    //            .Excluding(x => x.fs_kd_trs_bl_admin)
    //            .Excluding(x => x.fn_bl_admin)
    //            .Excluding(x => x.fs_kd_trs_bl_materai)
    //            .Excluding(x => x.fn_bl_materai)
    //            .Excluding(x => x.fs_kd_trs_bl_bulat_jasa)
    //            .Excluding(x => x.fn_bl_bulat_jasa)
    //            .Excluding(x => x.fs_kd_trs_bl_bulat_obat)
    //            .Excluding(x => x.fn_bl_bulat_obat));
    //}
}