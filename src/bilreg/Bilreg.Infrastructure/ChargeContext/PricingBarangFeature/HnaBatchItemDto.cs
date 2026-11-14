using System.Globalization;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

/// <summary>
/// DTO baris HNA per barang pada tabel legacy <c>tb_trs_hna2</c>.
/// Skema riil menyimpan tanggal sebagai varchar <c>yyyy-MM-dd</c> dan <c>fn_no_urut</c> sebagai
/// <c>decimal(18,0)</c>. Sentinel <c>fd_tgl_expired = '3000-01-01'</c> menandakan snapshot aktif.
/// </summary>
public record HnaBatchItemDto(
    string fs_kd_trs,
    decimal fn_no_urut,
    string fs_kd_barang,
    decimal fn_hna_akhir,
    decimal fn_hna_rata,
    decimal fn_hna_max,
    decimal fn_hna_gen,
    decimal fn_diskon,
    decimal fn_hna_gen_ri,
    string fd_tgl_berlaku,
    string fd_tgl_expired,
    bool fb_void,
    string CRTTGL,
    string CRTJAM,
    string CRTUSR,
    string UPDTGL,
    string UPDJAM,
    string UPDUSR)
{
    internal static HnaBatchItemDto FromModel(HnaBatchItemModel model, DateTime now) => new(
        model.HnaBatchId,
        model.NoUrut,
        model.BarangId,
        model.HnaAkhir,
        model.HnaRata,
        model.HnaMax,
        model.HnaGen,
        model.DiskonGen,
        model.HnaGenRi,
        HnaBatchDto.FormatDate(model.TglBerlaku),
        HnaBatchDto.FormatDate(model.TglExpired),
        model.IsVoid,
        HnaBatchDto.FormatDate(now),
        HnaBatchDto.FormatTime(now),
        string.Empty,
        HnaBatchDto.FormatDate(now),
        HnaBatchDto.FormatTime(now),
        string.Empty);

    public HnaBatchItemModel ToModel() => new(
        fs_kd_trs,
        (int)fn_no_urut,
        fs_kd_barang,
        fn_hna_akhir,
        fn_hna_rata,
        fn_hna_max,
        fn_hna_gen,
        fn_hna_gen_ri,
        fn_diskon,
        ParseDate(fd_tgl_berlaku),
        ParseDate(fd_tgl_expired),
        fb_void);

    internal static DateTime ParseDate(string value) =>
        DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : AppConst.DEF_DATE;
}
