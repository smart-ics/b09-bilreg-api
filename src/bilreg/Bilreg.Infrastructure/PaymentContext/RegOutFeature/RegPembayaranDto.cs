using Bilreg.Domain.PaymentContext.RegOutFeature;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public record RegPembayaranDto(
    string fs_kd_reg,
    string fs_kd_bayar,
    string fs_nm_bayar,
    decimal fn_jasa,
    decimal fn_obat)
{
    public RegPembayaranType ToModel()
    {
        var subTotal = fn_jasa + fn_obat;
        var result = new RegPembayaranType(fs_kd_reg, fs_kd_bayar, fs_nm_bayar, fn_jasa, fn_obat, subTotal);
        return result;
    }

    public static RegPembayaranDto FromModel(RegPembayaranType model)
    {
        var dto = new RegPembayaranDto(model.RegId, model.CaraBayarId, model.CaraBayarName, model.NilaiJasa, model.NilaiObat);
        return dto;
    }
}
