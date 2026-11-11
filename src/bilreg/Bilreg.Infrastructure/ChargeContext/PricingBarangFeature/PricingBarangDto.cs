using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

/// <summary>
/// DTO parameter harga barang pada tabel legacy <c>tb_barang</c>
/// (kolom <c>fs_kd_barang</c>, <c>fn_het</c>, <c>fs_metode_gen_hna</c>).
/// </summary>
public record PricingBarangDto(
    string fs_kd_barang,
    decimal fn_het,
    string fs_metode_gen_hna)
{
    public static PricingBarangDto FromModel(PricingBarangModel model) => new(
        model.BarangId,
        model.HetNominal,
        ((int)model.MetodeGenHna).ToString());

    public PricingBarangModel ToModel() => new(
        fs_kd_barang,
        fn_het,
        ParseMetodeGenHna(fs_metode_gen_hna),
        AuditTrailType.Default,
        []);

    private static MetodeGenHnaEnum ParseMetodeGenHna(string value) =>
        int.TryParse(value, out var code) && Enum.IsDefined(typeof(MetodeGenHnaEnum), code)
            ? (MetodeGenHnaEnum)code
            : MetodeGenHnaEnum.Terakhir;
}
