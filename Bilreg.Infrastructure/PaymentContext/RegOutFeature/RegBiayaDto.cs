using Bilreg.Domain.PaymentContext.RegOutFeature;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public record RegBiayaDto(
    string fs_kd_reg,
    string fs_kd_trs_bl_admin,
    decimal fn_bl_admin,
    string fs_kd_trs_bl_materai,
    decimal fn_bl_materai,
    string fs_kd_trs_bl_bulat_jasa,
    decimal fn_bl_bulat_jasa,
    string fs_kd_trs_bl_bulat_obat,
    decimal fn_bl_bulat_obat)
{
    public IEnumerable<RegBiayaType> ToModels()
    {
        // ADM Component untuk Administrasi
        yield return new RegBiayaType(
            regId: fs_kd_reg,
            komponenId: "ADM",
            reffBlId: fs_kd_trs_bl_admin,
            nilai: fn_bl_admin
        );

        // MTR Component untuk Materai
        yield return new RegBiayaType(
            regId: fs_kd_reg,
            komponenId: "MTR",
            reffBlId: fs_kd_trs_bl_materai,
            nilai: fn_bl_materai
        );

        // BBJ Component untuk Bulat Jasa
        yield return new RegBiayaType(
            regId: fs_kd_reg,
            komponenId: "BBJ",
            reffBlId: fs_kd_trs_bl_bulat_jasa,
            nilai: fn_bl_bulat_jasa
        );

        // BBO Component untuk Bulat Obat
        yield return new RegBiayaType(
            regId: fs_kd_reg,
            komponenId: "BBO",
            reffBlId: fs_kd_trs_bl_bulat_obat,
            nilai: fn_bl_bulat_obat
        );
    }
}
