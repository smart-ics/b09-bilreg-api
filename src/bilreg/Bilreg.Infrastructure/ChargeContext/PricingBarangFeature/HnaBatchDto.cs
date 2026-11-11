using System.Globalization;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

/// <summary>
/// DTO header batch HNA pada tabel legacy <c>tb_trs_hna</c>.
/// Skema riil tidak memiliki kolom <c>fb_void</c>; status void ditandai oleh sentinel
/// <c>fd_tgl_void = '3000-01-01'</c>. Seluruh tanggal disimpan sebagai varchar <c>yyyy-MM-dd</c>.
/// </summary>
public record HnaBatchDto(
    string fs_kd_trs,
    string fd_tgl_trs,
    string fs_jam_trs,
    string fs_kd_petugas,
    string fd_tgl_void,
    string fs_jam_void,
    string fs_kd_petugas_void,
    string CRTTGL,
    string CRTJAM,
    string CRTUSR,
    string UPDTGL,
    string UPDJAM,
    string UPDUSR)
{
    internal static HnaBatchDto FromModel(HnaBatchModel model, DateTime now) => new(
        model.HnaBatchId,
        FormatDate(model.TglTrs),
        FormatTime(now),
        model.PetugasId,
        FormatDate(AppConst.DEF_DATE),
        "00:00:00",
        string.Empty,
        FormatDate(now),
        FormatTime(now),
        model.PetugasId,
        FormatDate(now),
        FormatTime(now),
        model.PetugasId);

    internal static string FormatDate(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    internal static string FormatTime(DateTime value) => value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
}
