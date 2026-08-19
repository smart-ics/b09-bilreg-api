using Bilreg.Domain.BedUsageContext.RoomChargeFeature;

namespace Bilreg.Infrastructure.BedUsageContext.RoomChargeFeature;

public record RoomChargeLogDto(string fs_kd_trs,
string fd_tgl_trs,
string fd_tgl_upd,
string fs_jam_upd,
string fs_kd_petugas,
string fs_kd_upd,
string fs_kd_reg,
string fs_kd_layanan,
string fs_kd_bed,
decimal fn_tarif,
decimal fn_qty,
bool fb_status_void)
{
    public static RoomChargeLogDto FromModel(RoomChargeModel model, bool isVoid)
    {
        return new RoomChargeLogDto(
            model.RoomChargeId,
            model.TimeCharge.ToString("yyyy-MM-dd"),
            DateTime.Now.ToString("yyyy-MM-dd"),
            DateTime.Now.ToString("HH:mm:ss"),
            model.UserId,
            model.UserId,
            model.Reg.RegId,
            model.Layanan.LayananId,
            model.Bed.BedId,
            model.Tarif,
            model.Qty,
            isVoid);
    }
}
