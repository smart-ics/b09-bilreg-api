namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record ScheduleOpHistDto(
    string ScheduleOpHistId,
    string ScheduleOpId,
    int NoUrut,
    DateTime OldTglOp,
    DateTime NewTglOp,
    int OldDurasi,
    int NewDurasi,
    string OldKamarId,
    string NewKamarId,
    string OldPpaId,
    string NewPpaId,
    string Alasan,
    bool IsOverride,
    string OverrideReason,
    string UserId,
    DateTime CrtDate);
