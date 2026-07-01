using Farinv.Domain.BrgContext.PricingPolicyFeature;

namespace Farinv.Infrastructure.BrgContext.PricingPolicyFeature;

public record TipeBrgDto(
    string fs_kd_tipe_barang,
    string fs_nm_tipe_barang,
    bool fb_aktif,
    decimal fn_biaya_per_barang,
    decimal fn_biaya_per_racik,
    decimal fn_profit,
    decimal fn_tax,
    decimal fn_diskon)
{
    public static TipeBrgDto FromModel(TipeBrgType model)
    {
        return new TipeBrgDto(
            model.TipeBrgId,
            model.TipeBrgName,
            model.IsActive,
            model.BiayaPerBarang,
            model.BiayaPerRacik,
            model.Profit,
            model.Tax,
            model.Diskon);
    }

    public TipeBrgType ToModel()
    {
        return TipeBrgType.Create(
            fs_kd_tipe_barang,
            fs_nm_tipe_barang,
            fb_aktif,
            fn_biaya_per_barang,
            fn_biaya_per_racik,
            fn_profit,
            fn_tax,
            fn_diskon);
    }
}
