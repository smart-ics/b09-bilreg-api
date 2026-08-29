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
    public static RoomChargeLogDto FromModel(RoomChargeModel model, bool isVoid, DateTime occurredAt, string userId)
    {
        return new RoomChargeLogDto(
            model.RoomChargeId,
            model.TimeCharge.ToString("yyyy-MM-dd"),
            occurredAt.ToString("yyyy-MM-dd"),
            occurredAt.ToString("HH:mm:ss"),
            model.UserId,
            userId,
            model.Reg.RegId,
            model.Layanan.LayananId,
            model.Bed.BedId,
            model.Tarif,
            model.Qty,
            isVoid);
    }
}
