using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.RoomChargeFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using System.Globalization;

namespace Bilreg.Infrastructure.BedUsageContext.RoomChargeFeature;

public record RoomChargeDto(
string fs_kd_trs, string fs_kd_pakai_bed,
string fd_tgl_trs, string fs_jam_trs,
string fs_kd_petugas, string fs_kd_reg, 
string fs_kd_layanan, string fs_kd_bed,
decimal fn_tarif, decimal fn_diskon,
decimal fn_total, string fs_ket,
decimal fn_qty, decimal fn_nilai_klaim,
string fs_kd_trs_dx,
string fs_mr, string fs_nm_pasien,
string fs_nm_layanan, string fs_nm_bed, bool fb_aktif_bed)
{
    public static RoomChargeDto FromModel(RoomChargeModel model)
    {
        var result = new RoomChargeDto(
            model.RoomChargeId, model.PakaiBedId, model.TimeCharge.ToString("yyyy-MM-dd"),
            model.TimeCharge.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            model.UserId, model.Reg.RegId, model.Layanan.LayananId,
            model.Bed.BedId, model.Tarif, model.Diskon, model.Total,
            "-", model.Qty, 0, "-",
            model.Reg.PasienId, model.Reg.PasienName, 
            model.Layanan.LayananName, model.Bed.BedName, model.Bed.IsAktif);
        return result;
    }

    public RoomChargeModel ToModel(IEnumerable<RoomChargeKomponenModel> listKomponen)
    {
        var reg = new RegReff(fs_kd_reg, fs_mr, fs_nm_pasien);
        var lyn = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var bed = new BedReff(fs_kd_bed, fs_nm_bed, fb_aktif_bed);
        var fd_tgl_jam_trs = $"{fd_tgl_trs.Trim()} {fs_jam_trs.Trim()}";
        var timeCharge = DateTime.ParseExact(
            fd_tgl_jam_trs,
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture
        );

        var result = new RoomChargeModel(fs_kd_trs, fs_kd_pakai_bed,
            timeCharge, fs_kd_petugas,
            reg, lyn, bed, fn_tarif, fn_diskon, fn_total, fn_qty, listKomponen);
        return result;

    }

    public RoomChargeView ToView()
    {
        var reg = new RegReff(fs_kd_reg, fs_mr, fs_nm_pasien);
        var lyn = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var bed = new BedReff(fs_kd_bed, fs_nm_bed, fb_aktif_bed);
        var fd_tgl_jam_trs = $"{fd_tgl_trs.Trim()} {fs_jam_trs.Trim()}";
        var timeCharge = DateTime.ParseExact(
            fd_tgl_jam_trs,
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture
        );
        var result = new RoomChargeView(fs_kd_trs, fs_kd_pakai_bed, 
            timeCharge, fs_kd_petugas, 
            reg, lyn, bed, fn_tarif, fn_diskon, fn_total, fn_qty);

        return result;
    }
}