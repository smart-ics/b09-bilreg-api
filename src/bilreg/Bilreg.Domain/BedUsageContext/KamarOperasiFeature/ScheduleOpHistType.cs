using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public record ScheduleOpHistType(
    int NoUrut,
    DateTime OldTglOp,
    DateTime NewTglOp,
    int OldDurasi,
    int NewDurasi,
    KamarReff OldKamar,
    KamarReff NewKamar,
    PpaReff OldTeamLead,
    PpaReff NewTeamLead,
    string Alasan,
    bool IsOverride,
    string OverrideReason,
    string UserId,
    DateTime CrtDate,
    string ChangeType = "RESCHEDULE")
{
    public static ScheduleOpHistType Default => new(
        0,
        new DateTime(3000, 1, 1),
        new DateTime(3000, 1, 1),
        0,
        0,
        KamarReff.Default,
        KamarReff.Default,
        PpaReff.Default,
        PpaReff.Default,
        string.Empty,
        false,
        string.Empty,
        string.Empty,
        new DateTime(3000, 1, 1),
        "RESCHEDULE");
}
